// 保存先: Assets/Scripts/UI/MinimapController.cs
// ミニマップ（地形背景＋Base/Pathオーバーレイ）。Mキーで開閉。
//   ・背景：俯瞰オルソカメラ→RenderTexture→RawImage。開いている間だけカメラ有効＝普段ゼロコスト。
//   ・オーバーレイ：Baseマーカー（Team色・クリック可）、Path線（Waypoint経由の折れ線）。位置は WorldToViewportPoint で地形と一致。
//   ・M-3a：指示元（自国＆プレイヤーの近く）→指示先（隣接の中立/敵）の2段クリックで、指示元BaseAIに派遣先を指示。指示中は矢印表示。
//   ・M-3c-2：複数の派遣先に「指示先ごと×兵種ごとの数」を溜めて一括発令（F1）。指示先選択→数入力→「追加」を繰り返し、最後に「発令」。
//   ・パン：MinimapDragPannerからのドラッグでミニマップカメラを移動（地図をつかんで動かす）。開くたびにPlayerの真上から開始。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MinimapController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private World world;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private Image baseMarkerPrefab;
    [SerializeField] private Image pathLinePrefab;
    [SerializeField] private PlayerCombatCore player; // 指示元の「自国＆近く」判定に使う
    [SerializeField] private GameObject quotaPanel;           // 兵種ごとの数を入れるパネル（M-3c-2）
    [SerializeField] private MinimapQuotaRow quotaRowPrefab;  // 兵種1行（名前＋−／数／＋）
    [SerializeField] private RectTransform quotaRowContainer; // 兵種行を並べる親
    [SerializeField] private Button quotaAddButton;           // この派遣先を追加（F1）
    [SerializeField] private Button quotaConfirmButton;       // 全発令（溜めた全指示先を一括送信）
    [SerializeField] private Text pendingSummaryLabel;        // 追加済み指示の一覧表示（任意・未設定可）
    [SerializeField] private EquipmentUIController equipmentUI; // C画面中のM＝装備をキャンセルしてMを開くため

    [Header("操作")]
    [SerializeField] private Key toggleKey = Key.M;
    [SerializeField] private float commandRange = 20f;  // 指示元にできる、プレイヤーからの距離
    [SerializeField] private float orderDuration = 30f; // 派遣指示の持続秒
    [SerializeField] private int defaultCount = 0;      // 兵種ごとの数の初期値（M-3c-2）
    [SerializeField] private int minCount = 0;          // 0＝その兵種は送らない
    [SerializeField] private int maxCount = 10;
    [SerializeField] private bool invertDrag = false;  // パン方向が逆に感じたらON（地図をつかんで動かす向きを反転）

    [Header("見た目")]
    [SerializeField] private float markerSize = 16f;
    [SerializeField] private float selectedScale = 1.6f;
    [SerializeField] private Color redColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color blueColor = new Color(0.2f, 0.4f, 0.9f);
    [SerializeField] private Color neutralColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color orderColor = new Color(1f, 0.9f, 0.2f); // 指示中の矢印色（発令済み）
    [SerializeField] private Color previewColor = new Color(1f, 0.6f, 0.1f); // 発令前プレビュー矢印色（追加済み）

    public static MinimapController Instance { get; private set; }
    public bool IsOpen => open;

    private bool open;
    private bool built;
    private Base orderSource;        // 選択中の指示元（null＝未選択）
    private Base pendingDestination; // 数入力中の指示先（M-3c-2。追加前の一時状態）
    private readonly List<BaseAI.DirectedOrder> pendingOrders = new List<BaseAI.DirectedOrder>(); // 追加済み指示（発令待ち・F1）

    private readonly List<(Base baseRef, Image marker)> markers = new List<(Base, Image)>();
    private readonly List<(Vector3 a, Vector3 b, RectTransform line)> segments = new List<(Vector3, Vector3, RectTransform)>();
    private readonly List<RectTransform> orderArrows = new List<RectTransform>(); // 指示矢印のプール
    private readonly List<MinimapQuotaRow> quotaRows = new List<MinimapQuotaRow>(); // 生成した兵種行（M-3c-2）

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (quotaAddButton != null) quotaAddButton.onClick.AddListener(OnAddOrder);
        if (quotaConfirmButton != null) quotaConfirmButton.onClick.AddListener(OnConfirmAll);
        SetOpen(false);
    }

    // 外部（I/Cキー処理）からM画面を閉じる（キャンセル）。
    public void Close() => SetOpen(false);

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
        {
            // 商人UI中のMは取引キャンセル→ミニマップを開く（画面の切り替え）。
            if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen)
            {
                MerchantUIController.Instance.Close();
                SetOpen(true);
            }
            // 進化画面（魔族のC画面）中のMは進化画面を閉じる → Mを開く。
            else if (EvolutionUIController.Instance != null && EvolutionUIController.Instance.IsOpen)
            {
                EvolutionUIController.Instance.Close();
                SetOpen(true);
            }
            // 装備画面（C）中のMは装備をキャンセル（瓶も一緒に閉じる）→ Mを開く。
            else if (equipmentUI != null && equipmentUI.IsOpen)
            {
                equipmentUI.Close();
                SetOpen(true);
            }
            // 瓶（I単独）中のMは瓶をキャンセル → Mを開く。
            else if (BottleUIController.Instance != null && BottleUIController.Instance.IsOpen)
            {
                BottleUIController.Instance.CloseBottle();
                SetOpen(true);
            }
            else SetOpen(!open);
        }

        if (open)
        {
            RefreshColors();
            RefreshOrders();
        }
    }

    private void SetOpen(bool value)
    {
        open = value;
        if (mapPanel != null) mapPanel.SetActive(value);
        if (minimapCamera != null) minimapCamera.enabled = value;

        if (value)
        {
            if (!built) Build();
            CenterCameraOnPlayer(); // 開くたびにPlayerの真上から開始
            UpdateOverlayPositions();
        }
        else
        {
            ResetOrderFlow(); // 閉じたら作りかけの指示を破棄
        }
    }

    private void Build()
    {
        if (world == null || overlayRoot == null) { built = true; return; }

        // Path線（Waypoint経由の折れ線。連続2点ごとに線分1本。先に作って背面側に）
        if (pathLinePrefab != null && world.Paths != null)
        {
            foreach (var path in world.Paths)
            {
                if (path == null || path.ConnectedBases == null || path.ConnectedBases.Count < 2) continue;
                Base a = path.ConnectedBases[0];
                Base b = path.ConnectedBases[1];
                if (a == null || b == null) continue;

                List<Vector3> pts = BuildRoutePoints(a, b, path);
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    var line = Instantiate(pathLinePrefab, overlayRoot);
                    line.name = "PathSeg";
                    segments.Add((pts[i], pts[i + 1], line.rectTransform));
                }
            }
        }

        // Baseマーカー（線より前面に）
        if (baseMarkerPrefab != null && world.Bases != null)
        {
            foreach (var b in world.Bases)
            {
                if (b == null) continue;
                var marker = Instantiate(baseMarkerPrefab, overlayRoot);
                marker.name = $"Base_{b.name}";
                marker.rectTransform.sizeDelta = new Vector2(markerSize, markerSize);
                markers.Add((b, marker));

                var mm = marker.GetComponent<MinimapMarker>();
                if (mm != null) mm.Setup(b, HandleBaseClicked);
            }
        }

        built = true;
    }

    // 経路のワールド点列：BaseA →（Waypointを順に・BaseAに近い端から）→ BaseB。
    private List<Vector3> BuildRoutePoints(Base a, Base b, Path path)
    {
        var pts = new List<Vector3>();
        pts.Add(a.GridCenterWorld); // マーカーと同じグリッド中央から線を引く

        var wps = path.Waypoints;
        if (wps != null && wps.Count > 0)
        {
            bool reverse = false;
            if (wps.Count >= 2 && wps[0] != null && wps[wps.Count - 1] != null)
            {
                float dFirst = (wps[0].Position - a.GridCenterWorld).sqrMagnitude;
                float dLast = (wps[wps.Count - 1].Position - a.GridCenterWorld).sqrMagnitude;
                reverse = dFirst > dLast;
            }
            for (int i = 0; i < wps.Count; i++)
            {
                var wp = reverse ? wps[wps.Count - 1 - i] : wps[i];
                if (wp != null) pts.Add(wp.Position);
            }
        }

        pts.Add(b.GridCenterWorld);
        return pts;
    }

    // ワールド座標 → overlayRoot のローカル座標（minimapCameraの投影を使うので地形と一致）。
    private Vector2 WorldToOverlay(Vector3 worldPos)
    {
        if (minimapCamera == null || overlayRoot == null) return Vector2.zero;
        Vector3 vp = minimapCamera.WorldToViewportPoint(worldPos);
        Rect r = overlayRoot.rect;
        return new Vector2((vp.x - 0.5f) * r.width, (vp.y - 0.5f) * r.height);
    }

    private void PlaceLine(RectTransform line, Vector2 pa, Vector2 pb)
    {
        Vector2 dir = pb - pa;
        float dist = dir.magnitude;
        line.anchoredPosition = (pa + pb) * 0.5f;
        line.sizeDelta = new Vector2(dist, line.sizeDelta.y); // 太さ(height)維持、長さ(width)を距離に
        line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void UpdateOverlayPositions()
    {
        foreach (var (baseRef, marker) in markers)
        {
            if (baseRef == null || marker == null) continue;
            bool visible = IsInView(baseRef.transform.position);
            if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
            if (visible)
                marker.rectTransform.anchoredPosition = WorldToOverlay(baseRef.GridCenterWorld); // 左下隅でなくグリッド中央に置く
        }
        foreach (var (wa, wb, line) in segments)
        {
            if (line == null) continue;
            PlaceLine(line, WorldToOverlay(wa), WorldToOverlay(wb));
        }
    }

    // ワールド座標がミニマップカメラの視野（0..1）内か。マーカーの表示/非表示判定に使う。
    //   ・点（マーカー）はこれでON/OFFする。線（Path・矢印）は端で切れるべきなのでMaskでクリップする。
    private bool IsInView(Vector3 worldPos)
    {
        if (minimapCamera == null) return true;
        Vector3 vp = minimapCamera.WorldToViewportPoint(worldPos);
        return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    // 操作中プレイヤーの位置・Team（陣営選択後はActivePlayer＝人間/魔族。未設定時は従来のplayer参照）。
    private Transform PlayerTransform => ActivePlayer.Exists ? ActivePlayer.Transform : (player != null ? player.transform : null);
    private Team PlayerTeam => ActivePlayer.Exists ? ActivePlayer.Team : (player != null ? player.Team : Team.None);

    // ミニマップカメラをPlayerの真上へ（XZをPlayerに合わせ、高さYは現状維持）。開くたびに呼ぶ。
    private void CenterCameraOnPlayer()
    {
        var pt = PlayerTransform;
        if (minimapCamera == null || pt == null) return;
        Vector3 cam = minimapCamera.transform.position;
        Vector3 pp = pt.position;
        cam.x = pp.x;
        cam.z = pp.z;
        minimapCamera.transform.position = cam;
    }

    // MinimapDragPanner（パネルに付与）から呼ばれる。ドラッグ量ぶんミニマップカメラを動かす（パン）。
    //   ・overlayRoot基準のローカル移動量に変換し、カメラのorthographicSizeでワールド移動量へ。
    //   ・カメラ向きに依存しないよう right/up を使う（真上向きなら right=+X, up=+Z）。
    //   ・地図をつかんで動かす＝カメラはカーソルと逆向き（invertDragで反転可）。
    public void OnMinimapDrag(PointerEventData e)
    {
        if (!open || minimapCamera == null || overlayRoot == null) return;

        Camera evCam = e.pressEventCamera; // OverlayキャンバスならnullでOK
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, e.position, evCam, out Vector2 cur)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, e.position - e.delta, evCam, out Vector2 prev)) return;

        Vector2 localDelta = cur - prev;
        Rect r = overlayRoot.rect;
        if (r.width <= 0f || r.height <= 0f) return;

        float worldH = 2f * minimapCamera.orthographicSize;
        float worldW = worldH * minimapCamera.aspect;
        float dx = (localDelta.x / r.width) * worldW;
        float dz = (localDelta.y / r.height) * worldH;

        float sign = invertDrag ? 1f : -1f; // 通常は逆向き（つかんで動かす）
        Vector3 move = sign * (dx * minimapCamera.transform.right + dz * minimapCamera.transform.up);
        minimapCamera.transform.position += move;

        UpdateOverlayPositions();
    }

    // --- クリック（指示元→派遣先＋数を「追加」で確定し、繰り返し溜める） ---
    //   ・指示元（自分のBase）をクリック → 全キャンセル（選択も積んだ指示もリセット）。
    //   ・「追加」で確定。確定済みは編集不可（変えたいならキャンセルしてやり直し）。
    private void HandleBaseClicked(Base b)
    {
        // 自分のBase（指示元）クリック＝全キャンセル。編集中・確定済みありでも同じ。
        if (orderSource != null && b == orderSource)
        {
            Debug.Log($"[Minimap] 指示元クリック → キャンセル（リセット）: {b.name}");
            ResetOrderFlow();
            return;
        }

        // 設定中（ある派遣先の数を編集中・未「追加」）
        if (pendingDestination != null)
        {
            if (b == pendingDestination)
            {
                // 同じ派遣先＝継続して設定中（何もしない。兵種行はそのまま）。
                Debug.Log($"[Minimap] 設定中: {b.name}（数を入れて「追加」。別の派遣先で切替、指示元でキャンセル）");
                return;
            }
            if (IsCommitted(b))
            {
                Debug.Log($"[Minimap] {b.name} は確定済み（編集不可。やり直すなら指示元クリックでキャンセル）");
                return;
            }
            if (CanBeDestination(orderSource, b))
            {
                // 別の派遣先へ切替。設定中だった分は「追加」していないので破棄。
                Debug.Log($"[Minimap] 派遣先切替: {b.name}（設定中だった分は未追加のため破棄）");
                pendingDestination = b; ShowQuotaPanel(orderSource, b); UpdateHighlight();
                return;
            }
            if (CanBeSource(b))
            {
                Debug.Log($"[Minimap] 指示元変更: {b.name}（指示をリセット）");
                ResetOrderFlow(); orderSource = b; UpdateHighlight();
                return;
            }
            Debug.Log("[Minimap] 派遣先にできません（指示元に隣接する中立/敵Baseのみ）");
            return;
        }

        // 指示元 未選択
        if (orderSource == null)
        {
            if (CanBeSource(b)) { orderSource = b; UpdateHighlight(); Debug.Log($"[Minimap] 指示元: {b.name}"); }
            else Debug.Log("[Minimap] 指示元にできません（自国かつプレイヤーの近く以内のBaseのみ）");
            return;
        }

        // 指示元 選択済み・設定中でない（最初の派遣先選択 or「追加」直後＝次の命令を設定する状態）
        if (IsCommitted(b))
        {
            Debug.Log($"[Minimap] {b.name} は確定済み（編集不可。やり直すなら指示元クリックでキャンセル）");
            return;
        }
        if (CanBeDestination(orderSource, b))
        {
            pendingDestination = b; ShowQuotaPanel(orderSource, b); UpdateHighlight();
            Debug.Log($"[Minimap] 派遣先: {b.name} → 数を入れて「追加」");
            return;
        }
        if (CanBeSource(b))
        {
            Debug.Log($"[Minimap] 指示元変更: {b.name}（指示をリセット）");
            ResetOrderFlow(); orderSource = b; UpdateHighlight();
            return;
        }
        Debug.Log("[Minimap] 派遣先にできません（指示元に隣接する中立/敵Baseのみ）");
    }

    // 既に「追加」して確定済みの派遣先か。
    private bool IsCommitted(Base b) => pendingOrders.Exists(o => o.target == b);

    // 指示元が生産できる兵種ぶん、行（名前＋−／数／＋）を既定値で並べる。
    private void ShowQuotaPanel(Base source, Base destination)
    {
        ClearQuotaRows();
        var ai = source != null ? source.GetComponent<BaseAI>() : null;
        if (ai == null || quotaRowPrefab == null || quotaRowContainer == null || quotaPanel == null)
        {
            Debug.Log("[Minimap] 数UI（兵種行）の参照が未設定です");
            ResetOrderFlow();
            return;
        }
        var types = ai.GetProducibleMinions();
        if (types == null || types.Count == 0)
        {
            Debug.Log("[Minimap] この指示元は生産できる兵種がありません");
            pendingDestination = null; HideQuotaPanel(); UpdateHighlight();
            return;
        }

        foreach (var t in types)
        {
            var row = Instantiate(quotaRowPrefab, quotaRowContainer);
            row.Setup(t, defaultCount, minCount, maxCount);
            quotaRows.Add(row);
        }
        quotaPanel.SetActive(true);
    }

    // 「追加」：今の兵種×数を1指示先ぶんとして溜める。数>0が1件も無ければ追加しない。
    private void OnAddOrder()
    {
        if (orderSource == null || pendingDestination == null) return;

        var quotas = CollectQuotas();
        if (quotas.Count == 0)
        {
            Debug.Log("[Minimap] 数が全て0です（追加するにはどれか1兵種を1以上に）");
            return; // パネルは閉じない（入れ直し可）
        }

        // 同じ派遣先が既にあれば置き換え（再編集）
        pendingOrders.RemoveAll(o => o.target == pendingDestination);
        pendingOrders.Add(new BaseAI.DirectedOrder { target = pendingDestination, quotas = quotas });

        Debug.Log($"[Minimap] 追加: {pendingDestination.name} {DescribeQuotas(quotas)}（計{pendingOrders.Count}先）");
        // 確定したのでこの派遣先は編集不可に（pendingDestination=null）。
        // 兵種行は表示したまま、数だけ既定値にリセット（次の命令を設定する状態へ）。
        pendingDestination = null;
        ResetQuotaRowsToDefault();
        UpdatePendingSummary();
        UpdateHighlight();
    }

    // 兵種行を消さずに数だけ既定値へ戻す（「追加」後・次の命令用）。
    private void ResetQuotaRowsToDefault()
    {
        foreach (var row in quotaRows)
            if (row != null) row.Setup(row.Type, defaultCount, minCount, maxCount);
    }

    // 「発令」：これまでに「追加」した指示だけを一括送信する。
    //   編集中で未「追加」の派遣先は送らない（確定は「追加」のみ）。
    private void OnConfirmAll()
    {
        if (orderSource == null) { ResetOrderFlow(); return; }

        if (pendingDestination != null && !IsCommitted(pendingDestination))
            Debug.Log($"[Minimap] 設定中の {pendingDestination.name} は未「追加」のため送りません");

        if (pendingOrders.Count == 0)
        {
            Debug.Log("[Minimap] 指示が空です（派遣先を「追加」してから発令）");
            return; // フローは維持（入れ直し可）
        }

        var ai = orderSource.GetComponent<BaseAI>();
        if (ai != null) ai.SetDirectedOrders(pendingOrders, orderDuration);
        Debug.Log($"[Minimap] 発令: {orderSource.name} → {pendingOrders.Count}先（{orderDuration}秒）");
        ResetOrderFlow();
    }

    // 兵種行から数>0のものを集める。
    private List<(MinionData type, int count)> CollectQuotas()
    {
        var quotas = new List<(MinionData type, int count)>();
        foreach (var row in quotaRows)
            if (row != null && row.Type != null && row.Count > 0)
                quotas.Add((row.Type, row.Count));
        return quotas;
    }

    private string DescribeQuotas(List<(MinionData type, int count)> quotas)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < quotas.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(DisplayName(quotas[i].type)).Append("×").Append(quotas[i].count);
        }
        return sb.ToString();
    }

    // アセット名の接頭辞を剥がした表示名（例: MinionData_Minion → Minion。QuotaRowと同じ流儀）。
    private static string DisplayName(MinionData type)
    {
        if (type == null) return "?";
        string n = type.name;
        int us = n.IndexOf('_');
        return (us >= 0 && us < n.Length - 1) ? n.Substring(us + 1) : n;
    }

    private void UpdatePendingSummary()
    {
        if (pendingSummaryLabel == null) return;
        if (pendingOrders.Count == 0) { pendingSummaryLabel.text = "（指示先なし）"; return; }
        var sb = new System.Text.StringBuilder();
        foreach (var o in pendingOrders)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(o.target != null ? o.target.name : "?").Append(": ").Append(DescribeQuotas(o.quotas));
        }
        pendingSummaryLabel.text = sb.ToString();
    }

    private void ClearQuotaRows()
    {
        foreach (var row in quotaRows) if (row != null) Destroy(row.gameObject);
        quotaRows.Clear();
    }

    private void HideQuotaPanel()
    {
        ClearQuotaRows();
        if (quotaPanel != null) quotaPanel.SetActive(false);
    }

    // 指示の途中状態（指示元・指示先・追加済み・兵種行）を全部リセットする。
    private void ResetOrderFlow()
    {
        orderSource = null;
        pendingDestination = null;
        pendingOrders.Clear();
        HideQuotaPanel();
        UpdatePendingSummary();
        UpdateHighlight();
    }

    // 指示元になれる：自国かつプレイヤーの近く。
    private bool CanBeSource(Base b)
    {
        var pt = PlayerTransform;
        if (b == null || pt == null) return false;
        var ai = b.GetComponent<BaseAI>();
        if (ai == null || ai.Team != PlayerTeam) return false;
        return Vector3.Distance(pt.position, b.transform.position) <= commandRange;
    }

    // 指示先になれる：指示元に隣接し、中立または敵。
    private bool CanBeDestination(Base src, Base dst)
    {
        if (src == null || dst == null || dst == src) return false;
        if (!IsNeighbor(src, dst)) return false;
        var srcAi = src.GetComponent<BaseAI>();
        var dstAi = dst.GetComponent<BaseAI>();
        if (srcAi == null || dstAi == null) return false;
        return dstAi.Team == Team.None || dstAi.Team != srcAi.Team;
    }

    private bool IsNeighbor(Base src, Base dst)
    {
        if (src.Paths == null) return false;
        foreach (var path in src.Paths)
            if (path != null && path.ConnectedBases != null && path.ConnectedBases.Contains(dst)) return true;
        return false;
    }

    private void UpdateHighlight()
    {
        float sel = markerSize * selectedScale;
        foreach (var (b, marker) in markers)
        {
            if (marker == null) continue;
            bool isSource = (b == orderSource);
            bool isPending = (b == pendingDestination) || pendingOrders.Exists(o => o.target == b);
            float size = (isSource || isPending) ? sel : markerSize;
            marker.rectTransform.sizeDelta = new Vector2(size, size);
        }
    }

    private void RefreshColors()
    {
        foreach (var (baseRef, marker) in markers)
        {
            if (baseRef == null || marker == null) continue;
            var ai = baseRef.GetComponent<BaseAI>();
            marker.color = TeamColor(ai != null ? ai.Team : Team.None);
        }
    }

    // 発令済み：各Baseの指示中の派遣先ぶん矢印を出す。
    // 発令前：指示元→追加済み派遣先のプレビュー矢印を別色で出す。
    private void RefreshOrders()
    {
        int idx = 0;

        foreach (var (b, _) in markers)
        {
            if (b == null) continue;
            var ai = b.GetComponent<BaseAI>();
            var targets = ai != null ? ai.GetDirectedTargets() : null;
            if (targets == null) continue;
            foreach (var tgt in targets)
            {
                if (tgt == null) continue;
                var arrow = GetArrow(idx++);
                arrow.gameObject.SetActive(true);
                var img = arrow.GetComponent<Image>();
                if (img != null) img.color = orderColor;
                PlaceLine(arrow, WorldToOverlay(b.GridCenterWorld), WorldToOverlay(tgt.GridCenterWorld));
            }
        }

        if (orderSource != null)
        {
            foreach (var o in pendingOrders)
            {
                if (o.target == null) continue;
                var arrow = GetArrow(idx++);
                arrow.gameObject.SetActive(true);
                var img = arrow.GetComponent<Image>();
                if (img != null) img.color = previewColor;
                PlaceLine(arrow, WorldToOverlay(orderSource.GridCenterWorld), WorldToOverlay(o.target.GridCenterWorld));
            }
        }

        for (int i = idx; i < orderArrows.Count; i++)
            if (orderArrows[i] != null) orderArrows[i].gameObject.SetActive(false);
    }

    private RectTransform GetArrow(int i)
    {
        while (orderArrows.Count <= i)
        {
            var line = Instantiate(pathLinePrefab, overlayRoot);
            line.name = "OrderArrow";
            orderArrows.Add(line.rectTransform);
        }
        return orderArrows[i];
    }

    private Color TeamColor(Team team)
    {
        switch (team)
        {
            case Team.Red: return redColor;
            case Team.Blue: return blueColor;
            default: return neutralColor;
        }
    }
}
