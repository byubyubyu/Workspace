// 保存先: Assets/Scripts/UI/MinimapDispatchPanel.cs
// 派遣指令パネル（M画面の右カラム）。MinimapControllerから分離した指令フロー担当。
//   ・M-3a：指示元（自国＆プレイヤーの近く）→指示先（隣接の中立/敵）の2段クリックで派遣先を指示。
//   ・M-3c-2：複数の派遣先に「指示先ごと×兵種ごとの数」を溜めて一括発令。
//     指示先選択→数入力→「追加」を繰り返し、最後に「発令」でBaseAI.SetDirectedOrdersへ送る。
//   ・描画（マーカー色/サイズ・矢印）はMinimapControllerが担当し、こちらの状態を
//     IsEmphasized / GetPreviewArrows で毎フレーム読む（パネル→ビューの参照は持たない一方向）。
//   ・Baseクリックはビュー（マーカー）からOnBaseClickedに転送されてくる。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapDispatchPanel : MonoBehaviour
{
    [Header("数入力UI（M-3c-2）")]
    [SerializeField] private PlayerCombatCore player;          // 指示元の「自国＆近く」判定に使う（ActivePlayer未設定時のフォールバック）
    [SerializeField] private GameObject quotaPanel;            // 兵種ごとの数を入れるパネル
    [SerializeField] private MinimapQuotaRow quotaRowPrefab;   // 兵種1行（名前＋−／数／＋）
    [SerializeField] private RectTransform quotaRowContainer;  // 兵種行を並べる親
    [SerializeField] private Button quotaAddButton;            // この派遣先を追加
    [SerializeField] private Button quotaConfirmButton;        // 全発令（溜めた全指示先を一括送信）
    [SerializeField] private Text pendingSummaryLabel;         // 追加済み指示の一覧表示（任意・未設定可）

    [Header("指令ルール")]
    [SerializeField] private float commandRange = 20f;  // 指示元にできる、プレイヤーからの距離
    [SerializeField] private float orderDuration = 30f; // 派遣指示の持続秒
    [SerializeField] private int defaultCount = 0;      // 兵種ごとの数の初期値
    [SerializeField] private int minCount = 0;          // 0＝その兵種は送らない
    [SerializeField] private int maxCount = 10;

    private Base orderSource;        // 選択中の指示元（null＝未選択）
    private Base pendingDestination; // 数入力中の指示先（追加前の一時状態）
    private readonly List<BaseAI.DirectedOrder> pendingOrders = new List<BaseAI.DirectedOrder>(); // 追加済み指示（発令待ち）
    private readonly List<MinimapQuotaRow> quotaRows = new List<MinimapQuotaRow>(); // 生成した兵種行

    // 操作中プレイヤーの位置・Team（陣営選択後はActivePlayer。未設定時は従来のplayer参照）。
    private Transform PlayerTransform => ActivePlayer.Exists ? ActivePlayer.Transform : (player != null ? player.transform : null);
    private Team PlayerTeam => ActivePlayer.Exists ? ActivePlayer.Team : (player != null ? player.Team : Team.None);

    private void Start()
    {
        if (quotaAddButton != null) quotaAddButton.onClick.AddListener(OnAddOrder);
        if (quotaConfirmButton != null) quotaConfirmButton.onClick.AddListener(OnConfirmAll);
        HideQuotaPanel();
        UpdatePendingSummary();
    }

    // ============ ビューが読む状態（描画はMinimapController側） ============

    // このBaseを強調表示するか（指示元・設定中・追加済みの派遣先）。
    public bool IsEmphasized(Base b)
    {
        if (b == null) return false;
        return b == orderSource || b == pendingDestination || pendingOrders.Exists(o => o.target == b);
    }

    // 発令前プレビュー矢印（指示元→追加済み派遣先）のワールド座標ペアをbufferへ詰める。
    public void GetPreviewArrows(List<(Vector3 from, Vector3 to)> buffer)
    {
        buffer.Clear();
        if (orderSource == null) return;
        foreach (var o in pendingOrders)
            if (o.target != null) buffer.Add((orderSource.GridCenterWorld, o.target.GridCenterWorld));
    }

    // 指示の途中状態（指示元・指示先・追加済み・兵種行）を全部リセットする。M画面を閉じた時にも呼ばれる。
    public void ResetFlow()
    {
        orderSource = null;
        pendingDestination = null;
        pendingOrders.Clear();
        HideQuotaPanel();
        UpdatePendingSummary();
    }

    // ============ クリック（指示元→派遣先＋数を「追加」で確定し、繰り返し溜める） ============
    //   ・指示元（自分のBase）をクリック → 全キャンセル（選択も積んだ指示もリセット）。
    //   ・「追加」で確定。確定済みは編集不可（変えたいならキャンセルしてやり直し）。
    //   ビュー（MinimapMarker→MinimapController）から転送されてくる。
    public void OnBaseClicked(Base b)
    {
        // 自分のBase（指示元）クリック＝全キャンセル。編集中・確定済みありでも同じ。
        if (orderSource != null && b == orderSource)
        {
            Debug.Log($"[Minimap] 指示元クリック → キャンセル（リセット）: {b.name}");
            ResetFlow();
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
                pendingDestination = b; ShowQuotaPanel(orderSource, b);
                return;
            }
            if (CanBeSource(b))
            {
                Debug.Log($"[Minimap] 指示元変更: {b.name}（指示をリセット）");
                ResetFlow(); orderSource = b;
                return;
            }
            Debug.Log("[Minimap] 派遣先にできません（指示元に隣接する中立/敵Baseのみ）");
            return;
        }

        // 指示元 未選択
        if (orderSource == null)
        {
            if (CanBeSource(b)) { orderSource = b; Debug.Log($"[Minimap] 指示元: {b.name}"); }
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
            pendingDestination = b; ShowQuotaPanel(orderSource, b);
            Debug.Log($"[Minimap] 派遣先: {b.name} → 数を入れて「追加」");
            return;
        }
        if (CanBeSource(b))
        {
            Debug.Log($"[Minimap] 指示元変更: {b.name}（指示をリセット）");
            ResetFlow(); orderSource = b;
            return;
        }
        Debug.Log("[Minimap] 派遣先にできません（指示元に隣接する中立/敵Baseのみ）");
    }

    // 既に「追加」して確定済みの派遣先か。
    private bool IsCommitted(Base b) => pendingOrders.Exists(o => o.target == b);

    // ============ 数入力（兵種行）・追加・発令 ============

    // 指示元が生産できる兵種ぶん、行（名前＋−／数／＋）を既定値で並べる。
    private void ShowQuotaPanel(Base source, Base destination)
    {
        ClearQuotaRows();
        var ai = source != null ? source.GetComponent<BaseAI>() : null;
        if (ai == null || quotaRowPrefab == null || quotaRowContainer == null || quotaPanel == null)
        {
            Debug.Log("[Minimap] 数UI（兵種行）の参照が未設定です");
            ResetFlow();
            return;
        }
        var types = ai.GetProducibleMinions();
        if (types == null || types.Count == 0)
        {
            Debug.Log("[Minimap] この指示元は生産できる兵種がありません");
            pendingDestination = null; HideQuotaPanel();
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
        if (orderSource == null) { ResetFlow(); return; }

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
        ResetFlow();
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

    // ============ 指示元/指示先の判定ルール ============

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
}
