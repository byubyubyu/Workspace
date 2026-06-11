// 保存先: Assets/Scripts/Player/StatusUIController.cs
// ステータス画面（人間用・仮キーP）。年齢・身体値・スキル一覧を数値で見る＋上げ下げ指定の操作を行う。
//   ・読み取り専用＋唯一の書き込みが PlayerSkills.SetMode（スキル行クリックで ↑→🔒→↓ をトグル）。
//     PlayerSkills/Age/Core/Stamina の公開プロパティを読むだけ＝一方向（進化・転生UIと同じ流儀）。
//   ・人間操作中のみ（魔族は進化画面が実質ステータス画面）。ActivePlayerで判定。
//   ・他UI（瓶/装備/M/商人）が開いている間はPを無視し、開いている最中に他UIが開いたら自分から閉じる
//     （相互閉じと同じ結果を自分の監視で実現＝他UIのコードを触らない）。
//   ・開いている間は毎フレーム更新（スキル値・年齢は常に動くため）。
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StatusUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;       // ステータス画面のパネル
    [SerializeField] private Text titleLabel;        // 見出し（「ステータス」）
    [SerializeField] private Text ageLabel;          // 年齢・段階・能力倍率
    [SerializeField] private Text vitalLabel;        // HP・スタミナ・攻撃・防御・軽減
    [SerializeField] private Text capLabel;          // キャップ使用量
    [SerializeField] private Button[] skillButtons;  // スキル行（クリック＝上げ下げトグル。スキル数より多いぶんは非表示）
    [SerializeField] private Text[] skillLabels;     // スキル行の文字
    [SerializeField] private EquipmentUIController equipmentUI; // 相互閉じ判定用（Instanceを持たないため参照で）
    [SerializeField] private Key toggleKey = Key.P;

    private PlayerCombatCore core;
    private PlayerSkills skills;
    private Age age;
    private Stamina stamina;
    private bool open;

    public bool IsOpen => open;
    public static StatusUIController Instance { get; private set; }

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
        if (panel != null) panel.SetActive(false);
        for (int i = 0; i < skillButtons.Length; i++)
        {
            int index = i; // クロージャ用
            if (skillButtons[i] != null) skillButtons[i].onClick.AddListener(() => OnSkillClicked(index));
        }
        open = false;
    }

    private void Update()
    {
        // 人間を操作している時だけ対象（魔族中はPを無視し、開いていたら閉じる）。
        var activeCore = ActivePlayer.Exists ? ActivePlayer.Go.GetComponent<PlayerCombatCore>() : null;
        if (activeCore == null)
        {
            if (open) Close();
            return;
        }

        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
        {
            if (open) Close();
            else if (!AnyOtherUIOpen()) Open(activeCore); // 他UI中のPは無視（画面切替は将来そろえる）
        }

        if (!open) return;
        if (AnyOtherUIOpen()) { Close(); return; } // 他UIが開いたら自分から閉じる（相互閉じ）
        Refresh();
    }

    private bool AnyOtherUIOpen()
    {
        if (BottleUIController.Instance != null && BottleUIController.Instance.IsOpen) return true;
        if (MinimapController.Instance != null && MinimapController.Instance.IsOpen) return true;
        if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen) return true;
        if (CitizenProfileUIController.Instance != null && CitizenProfileUIController.Instance.IsOpen) return true;
        if (equipmentUI != null && equipmentUI.IsOpen) return true;
        return false;
    }

    private void Open(PlayerCombatCore target)
    {
        core = target;
        skills = target.GetComponent<PlayerSkills>();
        age = target.GetComponent<Age>();
        stamina = target.GetComponent<Stamina>();
        open = true;
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    private void Close()
    {
        open = false;
        if (panel != null) panel.SetActive(false);
    }

    private void Refresh()
    {
        if (core == null) return;
        if (titleLabel != null) titleLabel.text = "ステータス";

        if (ageLabel != null && age != null)
            ageLabel.text = $"{age.CurrentAge:F0}歳（{age.StageLabel}）　能力 {age.Multiplier:P0}";

        if (vitalLabel != null)
            vitalLabel.text = $"HP {core.Current:F0}/{core.Max:F0}　スタミナ {(stamina != null ? stamina.Current : 0f):F0}/{(stamina != null ? stamina.Max : 0f):F0}";

        if (skills == null) return;

        // スキル行：名前・実効値（遺伝＋鍛錬の内訳）・上げ下げ指定（クリックでトグル）。
        if (skills.SkillCount > skillButtons.Length)
            Debug.LogWarning($"[StatusUI] スキル{skills.SkillCount}件が行数{skillButtons.Length}を超過（あふれた分は非表示）");
        float trainedTotal = 0f;
        for (int i = 0; i < skillButtons.Length; i++)
        {
            bool exists = i < skills.SkillCount;
            if (skillButtons[i] != null) skillButtons[i].gameObject.SetActive(exists);
            if (!exists) continue;

            float value = skills.GetValue(i);
            float inheritedPart = value - TrainedOf(i);
            trainedTotal += TrainedOf(i);
            string mode = skills.GetMode(i) switch
            {
                PlayerSkills.GrowthMode.Raise => "↑",
                PlayerSkills.GrowthMode.Lock => "固",
                _ => "↓",
            };
            string name = SkillNameOf(i);
            if (skillLabels[i] != null)
                skillLabels[i].text = $"[{mode}] {name}　{value:F1}（遺伝{inheritedPart:F1}＋鍛錬{TrainedOf(i):F1}）";
        }

        if (capLabel != null)
            capLabel.text = $"鍛錬合計 {trainedTotal:F1} / {CapOf():F0}";
    }

    // スキル行クリック＝上げ下げ指定のトグル（↑→固定→↓→↑…）。唯一の書き込み操作。
    private void OnSkillClicked(int index)
    {
        if (skills == null || index >= skills.SkillCount) return;
        var next = skills.GetMode(index) switch
        {
            PlayerSkills.GrowthMode.Raise => PlayerSkills.GrowthMode.Lock,
            PlayerSkills.GrowthMode.Lock => PlayerSkills.GrowthMode.Lower,
            _ => PlayerSkills.GrowthMode.Raise,
        };
        skills.SetMode(index, next);
        Refresh();
    }

    // --- PlayerSkillsの表示用補助（公開プロパティに無い内訳はここで計算） ---
    private float TrainedOf(int i) => skills.GetValue(i) - InheritedOf(i);
    private float InheritedOf(int i) => skills.GetInherited(i);
    private string SkillNameOf(int i) => skills.GetSkillName(i);
    private float CapOf() => skills.TotalTrainCap;
}
