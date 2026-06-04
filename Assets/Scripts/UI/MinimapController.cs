// 保存先: Assets/Scripts/UI/MinimapController.cs
// ミニマップ（地形背景＋Base/Pathオーバーレイ）。Mキーで開閉。
//   ・背景：俯瞰オルソカメラ→RenderTexture→RawImage。開いている間だけカメラ有効＝普段ゼロコスト。
//   ・オーバーレイ：Baseマーカー（Team色・クリック可）、Path線（Waypoint経由の折れ線）。位置は WorldToViewportPoint で地形と一致。
//   ・M-3a：指示元（自国＆プレイヤーの近く）→指示先（隣接の中立/敵）の2段クリックで、指示元BaseAIに派遣先を指示。指示中は矢印表示。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    [SerializeField] private Button quotaConfirmButton;       // 発令

    [Header("操作")]
    [SerializeField] private Key toggleKey = Key.M;
    [SerializeField] private float commandRange = 20f;  // 指示元にできる、プレイヤーからの距離
    [SerializeField] private float orderDuration = 30f; // 派遣指示の持続秒
    [SerializeField] private int defaultCount = 0;      // 兵種ごとの数の初期値（M-3c-2）
    [SerializeField] private int minCount = 0;          // 0＝その兵種は送らない
    [SerializeField] private int maxCount = 10;

    [Header("見た目")]
    [SerializeField] private float markerSize = 16f;
    [SerializeField] private float selectedScale = 1.6f;
    [SerializeField] private Color redColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color blueColor = new Color(0.2f, 0.4f, 0.9f);
    [SerializeField] private Color neutralColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color orderColor = new Color(1f, 0.9f, 0.2f); // 指示中の矢印色

    private bool open;
    private bool built;
    private Base orderSource;        // 選択中の指示元（null＝未選択）
    private Base pendingDestination; // 数入力待ちの指示先（M-3c-2）

    private readonly List<(Base baseRef, Image marker)> markers = new List<(Base, Image)>();
    private readonly List<(Vector3 a, Vector3 b, RectTransform line)> segments = new List<(Vector3, Vector3, RectTransform)>();
    private readonly List<RectTransform> orderArrows = new List<RectTransform>(); // 指示矢印のプール
    private readonly List<MinimapQuotaRow> quotaRows = new List<MinimapQuotaRow>(); // 生成した兵種行（M-3c-2）

    private void Start()
    {
        if (quotaConfirmButton != null) quotaConfirmButton.onClick.AddListener(OnConfirmQuota);
        SetOpen(false);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            SetOpen(!open);

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
        pts.Add(a.transform.position);

        var wps = path.Waypoints;
        if (wps != null && wps.Count > 0)
        {
            bool reverse = false;
            if (wps.Count >= 2 && wps[0] != null && wps[wps.Count - 1] != null)
            {
                float dFirst = (wps[0].Position - a.transform.position).sqrMagnitude;
                float dLast = (wps[wps.Count - 1].Position - a.transform.position).sqrMagnitude;
                reverse = dFirst > dLast;
            }
            for (int i = 0; i < wps.Count; i++)
            {
                var wp = reverse ? wps[wps.Count - 1 - i] : wps[i];
                if (wp != null) pts.Add(wp.Position);
            }
        }

        pts.Add(b.transform.position);
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
            marker.rectTransform.anchoredPosition = WorldToOverlay(baseRef.transform.position);
        }
        foreach (var (wa, wb, line) in segments)
        {
            if (line == null) continue;
            PlaceLine(line, WorldToOverlay(wa), WorldToOverlay(wb));
        }
    }

    // --- クリック（2段選択：指示元→指示先） ---
    private void HandleBaseClicked(Base b)
    {
        // 数入力中にBaseをクリックしたら、その指示を取り消す
        if (pendingDestination != null)
        {
            Debug.Log("[Minimap] 発令キャンセル");
            ResetOrderFlow();
            return;
        }

        if (orderSource == null)
        {
            if (CanBeSource(b)) { orderSource = b; UpdateHighlight(); Debug.Log($"[Minimap] 指示元: {b.name}"); }
            else Debug.Log("[Minimap] 指示元にできません（自国かつプレイヤーの近く以内のBaseのみ）");
            return;
        }

        if (b == orderSource)
        {
            Debug.Log($"[Minimap] 指示元キャンセル: {b.name}");
            orderSource = null; UpdateHighlight();
        }
        else if (CanBeDestination(orderSource, b))
        {
            pendingDestination = b;
            ShowQuotaPanel(orderSource);
            Debug.Log($"[Minimap] 指示先: {b.name} → 兵種ごとに数を入れて発令してください");
        }
        else if (CanBeSource(b))
        {
            orderSource = b; UpdateHighlight();
            Debug.Log($"[Minimap] 指示元変更: {b.name}");
        }
        else Debug.Log("[Minimap] 指示先にできません（指示元に隣接する中立/敵Baseのみ）");
    }

    // 指示元が生産できる兵種ぶん、行（名前＋−／数／＋）を並べる。すべて常時表示。
    private void ShowQuotaPanel(Base source)
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
            ResetOrderFlow();
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

    // 発令ボタン：各行の (兵種, 数) のうち数>0 をまとめて指示元へ渡す。
    private void OnConfirmQuota()
    {
        if (orderSource == null || pendingDestination == null) { ResetOrderFlow(); return; }

        var quotas = new List<(MinionData type, int count)>();
        foreach (var row in quotaRows)
            if (row != null && row.Type != null && row.Count > 0)
                quotas.Add((row.Type, row.Count));

        if (quotas.Count == 0)
        {
            Debug.Log("[Minimap] 数が全て0です（どれか1兵種を1以上に）");
            return; // パネルは閉じない（入れ直し可）
        }

        var ai = orderSource.GetComponent<BaseAI>();
        if (ai != null) ai.SetDirectedTarget(pendingDestination, quotas, orderDuration);
        Debug.Log($"[Minimap] 発令: {orderSource.name} → {pendingDestination.name} {DescribeQuotas(quotas)}（{orderDuration}秒）");
        ResetOrderFlow();
    }

    private string DescribeQuotas(List<(MinionData type, int count)> quotas)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < quotas.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(quotas[i].type.name).Append("×").Append(quotas[i].count);
        }
        return sb.ToString();
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

    // 指示の途中状態（指示元・指示先・兵種行）を全部リセットする。
    private void ResetOrderFlow()
    {
        orderSource = null;
        pendingDestination = null;
        HideQuotaPanel();
        UpdateHighlight();
    }

    // 指示元になれる：自国かつプレイヤーの近く。
    private bool CanBeSource(Base b)
    {
        if (b == null || player == null) return false;
        var ai = b.GetComponent<BaseAI>();
        if (ai == null || ai.Team != player.Team) return false;
        return Vector3.Distance(player.transform.position, b.transform.position) <= commandRange;
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
            float size = (b == orderSource) ? sel : markerSize;
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

    // 指示中のBaseについて、指示元→指示先の矢印（線）を出す。
    private void RefreshOrders()
    {
        int idx = 0;
        foreach (var (b, _) in markers)
        {
            if (b == null) continue;
            var ai = b.GetComponent<BaseAI>();
            Base tgt = ai != null ? ai.DirectedTarget : null;
            if (tgt == null) continue;

            var arrow = GetArrow(idx++);
            arrow.gameObject.SetActive(true);
            var img = arrow.GetComponent<Image>();
            if (img != null) img.color = orderColor;
            PlaceLine(arrow, WorldToOverlay(b.transform.position), WorldToOverlay(tgt.transform.position));
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
