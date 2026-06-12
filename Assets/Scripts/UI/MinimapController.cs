// 保存先: Assets/Scripts/UI/MinimapController.cs
// ミニマップのビュー（地形背景＋Base/Pathオーバーレイの描画・パン・開閉）。統合メニューの「マップ」タブ。
//   ・背景：俯瞰オルソカメラ→RenderTexture→RawImage。開いている間だけカメラ有効＝普段ゼロコスト。
//   ・オーバーレイ：Baseマーカー（Team色・クリック可）、Path線（Waypoint経由の折れ線）。位置は WorldToViewportPoint で地形と一致。
//   ・矢印：発令済み（BaseAIの指示中ターゲット）＝黄、発令前プレビュー＝橙。
//   ・パン：MinimapDragPannerからのドラッグでミニマップカメラを移動。開くたびにPlayerの真上から開始。
//   ・派遣指令のフロー（指示元→派遣先→数→追加→発令）は MinimapDispatchPanel に分離（2026-06-12）。
//     マーカークリックはパネルへ転送し、強調表示・プレビュー矢印はパネルの状態を毎フレーム読んで描く（一方向参照）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MinimapController : MonoBehaviour, IMenuTab
{
    [Header("参照")]
    [SerializeField] private World world;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private Image baseMarkerPrefab;
    [SerializeField] private Image pathLinePrefab;
    [SerializeField] private MinimapDispatchPanel dispatch; // 派遣指令フロー（クリック転送先・強調/プレビューの状態元）

    [Header("操作")]
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

    private readonly List<(Base baseRef, Image marker)> markers = new List<(Base, Image)>();
    private readonly List<(Vector3 a, Vector3 b, RectTransform line)> segments = new List<(Vector3, Vector3, RectTransform)>();
    private readonly List<RectTransform> orderArrows = new List<RectTransform>(); // 指示矢印のプール
    private readonly List<(Vector3 from, Vector3 to)> previewBuffer = new List<(Vector3, Vector3)>(); // パネルから毎フレーム受けるプレビュー矢印

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
        SetOpen(false);
    }

    // 外部（I/Cキー処理）からM画面を閉じる（キャンセル）。
    public void Close() => SetOpen(false);

    // --- IMenuTab（統合メニューの「マップ」タブとして呼ばれる。人間/魔族共通） ---
    public void TabShow() => SetOpen(true);
    public void TabHide() => SetOpen(false);

    private void Update()
    {
        if (open)
        {
            RefreshMarkers();
            RefreshOrderArrows();
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
            if (dispatch != null) dispatch.ResetFlow(); // 閉じたら作りかけの指示を破棄
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

        // Baseマーカー（線より前面に）。クリックは派遣指令パネルへ転送する。
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

    private void HandleBaseClicked(Base b)
    {
        if (dispatch != null) dispatch.OnBaseClicked(b);
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

    // ミニマップカメラをPlayerの真上へ（XZをPlayerに合わせ、高さYは現状維持）。開くたびに呼ぶ。
    private void CenterCameraOnPlayer()
    {
        var pt = ActivePlayer.Transform;
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

    // ============ 毎フレームの描画更新（開いている間だけ） ============

    // マーカーの色（Team）とサイズ（派遣指令パネルの強調状態）を最新化する。
    private void RefreshMarkers()
    {
        float sel = markerSize * selectedScale;
        foreach (var (baseRef, marker) in markers)
        {
            if (baseRef == null || marker == null) continue;
            var ai = baseRef.GetComponent<BaseAI>();
            marker.color = TeamColor(ai != null ? ai.Team : Team.None);
            float size = (dispatch != null && dispatch.IsEmphasized(baseRef)) ? sel : markerSize;
            marker.rectTransform.sizeDelta = new Vector2(size, size);
        }
    }

    // 発令済み：各Baseの指示中の派遣先ぶん矢印を出す（黄）。
    // 発令前：派遣指令パネルのプレビュー矢印を別色で出す（橙）。
    private void RefreshOrderArrows()
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
                DrawArrow(idx++, b.GridCenterWorld, tgt.GridCenterWorld, orderColor);
            }
        }

        if (dispatch != null)
        {
            dispatch.GetPreviewArrows(previewBuffer);
            foreach (var (from, to) in previewBuffer)
                DrawArrow(idx++, from, to, previewColor);
        }

        for (int i = idx; i < orderArrows.Count; i++)
            if (orderArrows[i] != null) orderArrows[i].gameObject.SetActive(false);
    }

    private void DrawArrow(int i, Vector3 fromWorld, Vector3 toWorld, Color color)
    {
        var arrow = GetArrow(i);
        arrow.gameObject.SetActive(true);
        var img = arrow.GetComponent<Image>();
        if (img != null) img.color = color;
        PlaceLine(arrow, WorldToOverlay(fromWorld), WorldToOverlay(toWorld));
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
