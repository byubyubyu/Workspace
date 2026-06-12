// 保存先: Assets/Scripts/Demon/EvolutionUIController.cs
// 進化画面＝統合メニュー（TabMenuController）の魔族「進化」タブ。GDDセクション14。
//   レイアウト：左＝メインカメラの寄り表示（クローズアップはメニューが一元管理）／
//     中央＝進化候補リスト（EvolutionOptionSlotの動的生成行・部位prefabを3D表示）／
//     右＝素体名・捕食ポイントゲージ・現在ステータス（実効値）。
//   ・行クリック→詳細ビュー：候補部位の大3D＋ステ補正差分＋付与ワザ＋「進化」確定／「戻る」。
//     進化成功でリストへ戻る（画面は閉じない＝連続進化できる）。
//   ・3D表示は商人と同じ「裏空間（MerchantDisplay）＋専用カメラ→RenderTexture→RawImage」方式。
//     部位prefabは挙動を同梱するため、裏空間側はstripBehaviours=trueで飾り表示にする。
//   ※ 旧・自前のCキー処理／メインカメラ管理（左カラム絞り・CloseUpIsolator）／商人・M・瓶との
//     相互閉じは、魔族の統合メニュー化（2026-06-12）でTabMenuControllerへ一元化し削除した。
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionUIController : MonoBehaviour, IMenuTab
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;            // 進化画面のパネル

    [Header("候補リスト（動的生成）")]
    [SerializeField] private EvolutionOptionSlot slotPrefab; // 候補1件ぶんの行prefab
    [SerializeField] private RectTransform slotContainer;    // 行を並べる親（VerticalLayoutGroup想定）
    [SerializeField] private GameObject listGroup;            // ListView親

    [Header("詳細ビュー（行クリックで切替）")]
    [SerializeField] private GameObject detailGroup;          // DetailView親
    [SerializeField] private RectTransform detailPartFrame;   // 候補部位の大表示枠
    [SerializeField] private Text detailNameLabel;            // 「頭 → 硬い頭」
    [SerializeField] private Text detailStatsLabel;           // ステ補正差分＋付与ワザ
    [SerializeField] private Button confirmButton;            // 「進化」（不足時disabled）
    [SerializeField] private Button backButton;               // 「戻る」

    [Header("3Dモデル表示（商人画面と同じ方式）")]
    [SerializeField] private MerchantDisplay partDisplay;     // 裏空間（部位prefabを並べる。stripBehaviours=true）
    [SerializeField] private Camera partCamera;               // 裏空間を撮る専用カメラ（開いている間だけ有効）
    [SerializeField] private RectTransform rawImageRect;      // モデルを映すRawImageの矩形（枠位置→モデル位置の変換基準）
    [SerializeField] private Camera uiCamera;                 // Canvasのカメラ（Screen Space Overlayならnullのまま）
    [SerializeField] private float depth = 10f;               // 専用カメラからモデルを置く奥行き
    [SerializeField] private float listViewSize = 1.2f;       // リスト行の部位モデルの最大辺
    [SerializeField] private float detailViewSize = 3f;       // 詳細ビューの大表示モデルの最大辺

    [Header("右カラム（素体・ポイント・現在ステータス）")]
    [SerializeField] private Text formLabel;          // 素体名
    [SerializeField] private Text poolLabel;          // 捕食ポイントの数値（例 30/100）
    [SerializeField] private Image poolFill;          // 捕食ポイントゲージ（Image Type=Filled）
    [SerializeField] private Text statsLabel;         // 現在の実効ステータス（最大HP・攻撃・防御・移動補正）

    // 表示中の候補1件＝（スロット番号・候補番号）。行→この対をDemonCore.EvolvePartへ渡す。
    private struct Candidate
    {
        public int slot;
        public int option;
        public string label;
        public float cost;
        public PartData current;
        public PartData target;
    }

    private readonly List<Candidate> candidates = new List<Candidate>();
    private readonly List<EvolutionOptionSlot> slots = new List<EvolutionOptionSlot>();
    private int detailIndex = -1;     // 詳細ビュー中の候補（-1=リスト表示中）
    private bool open;
    private DemonCore demon;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (partCamera != null) partCamera.enabled = false;
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (backButton != null) backButton.onClick.AddListener(ShowListView);
        open = false;
    }

    private void Update()
    {
        // ポイント・HPは戦闘で常に変わるので開いている間は毎フレーム更新。
        //   （進化で体が組み直った時の見た目レイヤー当て直しはTabMenuControllerが毎フレーム行う。）
        if (open) RefreshDynamic();
    }

    // --- IMenuTab（TabMenuControllerから呼ばれる。クローズアップ・キー入力・相互排他はメニュー側） ---

    public void TabShow()
    {
        if (open) return;
        demon = ActivePlayer.Exists ? ActivePlayer.Go.GetComponent<DemonCore>() : null;
        if (demon == null) return; // 魔族タブはメニューが魔族操作中にしか出さない（保険）
        open = true;
        if (panel != null) panel.SetActive(true);
        if (partCamera != null) partCamera.enabled = true;
        BuildCandidates();
        BuildSlots();
        ShowListView();
    }

    public void TabHide()
    {
        if (!open) return;
        open = false;
        demon = null;
        detailIndex = -1;
        if (panel != null) panel.SetActive(false);
        if (partCamera != null) partCamera.enabled = false;
        ClearSlots();
        if (partDisplay != null) partDisplay.Clear();
    }

    // ============ 候補の収集・行の生成（開いた時と進化成功時だけ作り直す） ============

    // 全スロットの進化候補をフラットに集める（候補＝スロット番号＋候補番号。tier上限超えは出さない）。
    private void BuildCandidates()
    {
        candidates.Clear();
        if (demon == null || demon.Body == null) return;
        var slotsDef = demon.Body.Slots;
        for (int s = 0; s < demon.SlotCount; s++)
        {
            var part = demon.GetEquippedPart(s);
            if (part == null || part.evolutions == null) continue;
            for (int o = 0; o < part.evolutions.Count; o++)
            {
                var opt = part.evolutions[o];
                if (opt == null || opt.target == null) continue;
                if (opt.target.tier > demon.Body.MaxPartTier) continue; // 素体の進化上限を超える候補は出さない
                string slotName = s < slotsDef.Count ? slotsDef[s].slotName : "";
                candidates.Add(new Candidate
                {
                    slot = s,
                    option = o,
                    cost = opt.cost,
                    current = part,
                    target = opt.target,
                    label = $"{slotName}：{part.partName} → {opt.target.partName}",
                });
            }
        }
    }

    // 候補数ぶん行を生成する（MerchantUIController.BuildSlotsと同じ動的生成パターン）。
    private void BuildSlots()
    {
        ClearSlots();
        if (slotPrefab == null || slotContainer == null) return;
        for (int i = 0; i < candidates.Count; i++)
        {
            int index = i; // クロージャ用
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.Setup(candidates[i].label, candidates[i].cost, candidates[i].target, () => ShowDetailView(index));
            slots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            // Destroyは遅延のため、親から外してから破棄する（外さないと直後のForceRebuildLayoutImmediateが
            //   旧行を含めて高さ計算し、新行の3Dモデル位置がズレる。DemonCore.ApplyBodyと同じ罠対応）。
            slots[i].transform.SetParent(null, false);
            Destroy(slots[i].gameObject);
        }
        slots.Clear();
    }

    // ============ List / Detail 切替 ============

    private void ShowListView()
    {
        detailIndex = -1;
        if (listGroup != null) listGroup.SetActive(true);
        if (detailGroup != null) detailGroup.SetActive(false);
        RefreshListDisplay();
        RefreshDynamic();
    }

    private void ShowDetailView(int index)
    {
        if (index < 0 || index >= candidates.Count) return;
        detailIndex = index;
        var c = candidates[index];
        if (listGroup != null) listGroup.SetActive(false);
        if (detailGroup != null) detailGroup.SetActive(true);
        if (detailNameLabel != null) detailNameLabel.text = $"{c.current.partName} → {c.target.partName}";
        if (detailStatsLabel != null) detailStatsLabel.text = BuildDiffText(c.current, c.target);
        RefreshDetailDisplay(c);
        RefreshDynamic();
    }

    // リスト行の3Dモデル配置：各行のpartFrame位置に進化先部位のprefabを並べる。
    //   VerticalLayoutGroup配下に行を追加した直後はframe.positionが未確定なので、
    //   ForceRebuildLayoutImmediateでレイアウトを即時確定させてから位置を読む（商人と同じ罠対応）。
    private void RefreshListDisplay()
    {
        if (partDisplay == null || partCamera == null || rawImageRect == null) return;
        if (slotContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer);

        var prefabs = new List<GameObject>(slots.Count);
        var positions = new List<Vector3>(slots.Count);
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.Target == null || slot.PartFrame == null) continue;
            prefabs.Add(slot.Target.partPrefab);
            positions.Add(UIModelProjection.FrameToWorld(slot.PartFrame, rawImageRect, uiCamera, partCamera, depth));
        }
        partDisplay.ViewSize = listViewSize;
        partDisplay.UpdateDisplayPrefabs(prefabs, positions, null);
    }

    // 詳細ビューの3Dモデル配置：候補部位1個をdetailPartFrameの位置に大表示する。
    //   SetActive(true)直後はCanvasのlayoutが未確定なため、ForceUpdateCanvasesしてから位置を読む（商人と同じ罠対応）。
    private void RefreshDetailDisplay(Candidate c)
    {
        if (partDisplay == null || partCamera == null || rawImageRect == null || detailPartFrame == null) return;
        Canvas.ForceUpdateCanvases();
        var prefabs = new List<GameObject> { c.target.partPrefab };
        var positions = new List<Vector3>
        {
            UIModelProjection.FrameToWorld(detailPartFrame, rawImageRect, uiCamera, partCamera, depth),
        };
        partDisplay.ViewSize = detailViewSize;
        partDisplay.UpdateDisplayPrefabs(prefabs, positions, null);
    }

    // ステ補正の差分テキスト（現部位→進化先）。差分0の行は出さない。
    private static string BuildDiffText(PartData current, PartData target)
    {
        var sb = new StringBuilder();
        AppendDiff(sb, "HP", target.hpBonus - current.hpBonus);
        AppendDiff(sb, "攻撃", target.attackPowerBonus - current.attackPowerBonus);
        AppendDiff(sb, "防御", target.defenseBonus - current.defenseBonus);
        AppendDiff(sb, "移動", target.moveSpeedBonus - current.moveSpeedBonus);
        if (!Mathf.Approximately(current.damageMultiplier, target.damageMultiplier))
            sb.AppendLine($"被ダメ倍率 {current.damageMultiplier:F1} → {target.damageMultiplier:F1}");
        if (!Mathf.Approximately(current.partHp, target.partHp))
            sb.AppendLine($"部位HP {current.partHp:F0} → {target.partHp:F0}");

        // 付与ワザ（進化先が解放するワザ。表示名＝AttackMove.DisplayName）。
        if (target.grantedMoves != null && target.grantedMoves.Count > 0)
        {
            var names = new List<string>();
            foreach (var m in target.grantedMoves)
                if (m != null) names.Add(m.DisplayName);
            if (names.Count > 0) sb.AppendLine($"付与ワザ：{string.Join("・", names)}");
        }

        if (sb.Length == 0) sb.AppendLine("（補正変化なし）");
        return sb.ToString().TrimEnd();
    }

    private static void AppendDiff(StringBuilder sb, string label, float diff)
    {
        if (Mathf.Approximately(diff, 0f)) return;
        sb.AppendLine($"{label} {(diff > 0 ? "+" : "")}{diff:F1}".Replace(".0", ""));
    }

    // ============ 毎フレーム更新（ゲージ・現在ステータス・押せるか） ============

    private void RefreshDynamic()
    {
        if (demon == null || demon.Body == null) return;
        var pool = demon.DevourPool;

        if (poolLabel != null) poolLabel.text = pool != null ? $"{pool.Current:F0} / {pool.Max:F0}" : "-";
        if (poolFill != null) poolFill.fillAmount = pool != null && pool.Max > 0f ? pool.Current / pool.Max : 0f;
        if (formLabel != null) formLabel.text = $"素体：{demon.Body.BodyName}";

        // 現在の実効ステータス（素体＋部位Σ。DemonCore.ApplyBodyの確定値を読むだけ＝一方向）。
        if (statsLabel != null)
        {
            statsLabel.text =
                $"HP {demon.Current:F0} / {demon.Max:F0}\n" +
                $"攻撃 {demon.AttackPower:F0}　防御 {demon.Defense:F0}\n" +
                $"移動 {(demon.MoveSpeedBonus >= 0 ? "+" : "")}{demon.MoveSpeedBonus:F1}";
        }

        // 行の押せるか＋暗転（リスト中）／進化ボタンの押せるか（詳細中）。
        for (int i = 0; i < slots.Count && i < candidates.Count; i++)
            if (slots[i] != null) slots[i].SetAffordable(pool != null && pool.CanAfford(candidates[i].cost));
        if (confirmButton != null && detailIndex >= 0 && detailIndex < candidates.Count)
            confirmButton.interactable = pool != null && pool.CanAfford(candidates[detailIndex].cost);
    }

    // ============ 進化の確定 ============

    private void OnConfirmClicked()
    {
        if (demon == null || detailIndex < 0 || detailIndex >= candidates.Count) return;
        var c = candidates[detailIndex];
        if (demon.EvolvePart(c.slot, c.option))
        {
            Debug.Log($"[EvolutionUI] 部位進化成功 → {demon.GetEquippedPart(c.slot).partName}");
            // 候補が変わる（進化した部位の次の進化先が出る）ので作り直してリストへ＝連続進化できる。
            BuildCandidates();
            BuildSlots();
            ShowListView();
        }
    }
}
