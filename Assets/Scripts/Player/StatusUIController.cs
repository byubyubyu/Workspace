// 保存先: Assets/Scripts/Player/StatusUIController.cs
// ステータス画面（人間用・スキル設定）＝統合メニュー（TabMenuController）の「スキル」タブ。
//   ・レイアウト：左＝メインカメラの寄り表示（実機キャラ＝装備が見える・装備画面と同じ流儀）／
//     中央＝鍛錬合計バー＋スキル行（StatusSkillRowの動的生成。バッジ↑固↓・値バー・遺伝＋鍛錬内訳）／
//     右＝年齢・能力値（HP/スタミナ/攻撃/防御/軽減。スキル由来分は「（+n）」併記）・武器のワザ一覧。
//   ・読み取り専用＋唯一の書き込みが PlayerSkills.SetMode（スキル行クリックで ↑→固→↓ をトグル）。
//     PlayerSkills/Age/Core/Stamina の公開プロパティを読むだけ＝一方向（進化・転生UIと同じ流儀）。
//   ・人間操作中のみ（魔族は進化画面が実質ステータス画面）。ActivePlayerで判定。
//   ・カメラ（クローズアップ）・キー入力・相互排他は TabMenuController が一元管理（2026-06-12統合）。
//     ここはIMenuTab（TabShow/TabHide）でパネルの表示だけを担う。
//   ・開いている間は毎フレーム更新（スキル値・年齢は常に動くため）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StatusUIController : MonoBehaviour, IMenuTab
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;

    [Header("中央カラム（鍛錬キャップ＋スキル行）")]
    [SerializeField] private Image capFill;            // 鍛錬合計バー（Image Type=Filled）
    [SerializeField] private Text capLabel;            // 「鍛錬合計 42.4 / 100」
    [SerializeField] private StatusSkillRow rowPrefab; // スキル1件ぶんの行prefab
    [SerializeField] private RectTransform rowContainer; // 行を並べる親（VerticalLayoutGroup想定）

    [Header("右カラム（年齢・能力値・ワザ）")]
    [SerializeField] private Text ageLabel;            // 年齢・段階・能力倍率
    [SerializeField] private Text vitalLabel;          // 能力値のラベル列（HP/スタミナ/攻撃…左寄せ）
    [SerializeField] private Text vitalValueLabel;     // 能力値の数値列（右寄せ＝桁が揃う。スキル由来は（+n）併記）
    [SerializeField] private Text movesLabel;          // 武器のワザ一覧（上詰め。武器なしなら「（武器なし）」）

    // ※ クローズアップ（カメラ）・キー入力・相互排他は TabMenuController が一元管理（2026-06-12統合）。

    private PlayerCombatCore core;
    private PlayerSkills skills;
    private Age age;
    private Stamina stamina;
    private EquipmentHolder equipment;
    private readonly List<StatusSkillRow> rows = new List<StatusSkillRow>();
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
        open = false;
    }

    private void Update()
    {
        if (!open) return;
        Refresh(); // スキル値・年齢は常に動くため毎フレーム更新
    }

    // --- IMenuTab（TabMenuControllerから呼ばれる） ---

    public void TabShow()
    {
        if (open) return;
        var activeCore = ActivePlayer.Exists ? ActivePlayer.Go.GetComponent<PlayerCombatCore>() : null;
        if (activeCore == null) return;
        Open(activeCore);
    }

    public void TabHide()
    {
        if (open) Close();
    }

    private void Open(PlayerCombatCore target)
    {
        core = target;
        skills = target.GetComponent<PlayerSkills>();
        age = target.GetComponent<Age>();
        stamina = target.GetComponent<Stamina>();
        equipment = target.GetComponent<EquipmentHolder>();
        open = true;
        if (panel != null) panel.SetActive(true);

        BuildRows();
        Refresh();
    }

    private void Close()
    {
        open = false;
        if (panel != null) panel.SetActive(false);
        ClearRows();
    }

    // スキル数ぶん行を生成（カタログは実行中に増えないので開いた時だけ）。値の更新はRefreshが毎フレーム行う。
    private void BuildRows()
    {
        ClearRows();
        if (skills == null || rowPrefab == null || rowContainer == null) return;
        for (int i = 0; i < skills.SkillCount; i++)
        {
            int index = i; // クロージャ用
            var row = Instantiate(rowPrefab, rowContainer);
            row.Setup(() => OnSkillClicked(index));
            rows.Add(row);
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] == null) continue;
            rows[i].transform.SetParent(null, false); // 遅延Destroy対策（進化画面と同じ罠対応）
            Destroy(rows[i].gameObject);
        }
        rows.Clear();
    }

    private void Refresh()
    {
        if (core == null) return;
        float mult = age != null ? age.Multiplier : 1f;

        if (ageLabel != null && age != null)
            ageLabel.text = $"{age.CurrentAge:F0}歳（{age.StageLabel}）\n能力 {age.Multiplier:P0}";

        // 能力値：ラベル列（左寄せ）＋数値列（右寄せ）の2テキストで縦の桁を揃える。
        //   スキル由来分は「（+n）」併記。軽減は全量スキル由来なのでそのまま。
        if (vitalLabel != null) vitalLabel.text = "HP\nスタミナ\n攻撃\n防御\n軽減";
        if (vitalValueLabel != null)
        {
            float hpBonus = skills != null ? skills.GetHpBonus(mult) : 0f;
            float atkBonus = skills != null ? skills.GetAttackBonus(mult) : 0f;
            string atk = core.AttackPower > 0f ? $"{core.AttackPower:F0}{BonusText(atkBonus)}" : "－";
            vitalValueLabel.text =
                $"{core.Current:F0} / {core.Max:F0}{BonusText(hpBonus)}\n" +
                $"{(stamina != null ? stamina.Current : 0f):F0} / {(stamina != null ? stamina.Max : 0f):F0}\n" +
                $"{atk}\n" +
                $"{core.Defense:F0}\n" +
                $"{core.DamageCut:F1}";
        }

        // 武器のワザ一覧（表示名＝AttackMove.DisplayName）。
        if (movesLabel != null)
        {
            var weapon = equipment != null ? equipment.GetWeaponAttack() : null;
            if (weapon == null || weapon.moves == null || weapon.moves.Count == 0)
            {
                movesLabel.text = "（武器なし）";
            }
            else
            {
                var names = new List<string>();
                foreach (var m in weapon.moves)
                    if (m != null) names.Add(m.DisplayName);
                movesLabel.text = string.Join("\n", names);
            }
        }

        if (skills == null) return;

        // スキル行＋鍛錬キャップ。
        float trainedTotal = 0f;
        for (int i = 0; i < rows.Count && i < skills.SkillCount; i++)
        {
            float value = skills.GetValue(i);
            float inheritedPart = skills.GetInherited(i);
            float trainedPart = value - inheritedPart;
            trainedTotal += trainedPart;
            if (rows[i] != null)
                rows[i].SetData(skills.GetSkillName(i), value, skills.MaxSkillValue, inheritedPart, trainedPart, skills.GetMode(i));
        }
        if (capLabel != null) capLabel.text = $"鍛錬合計 {trainedTotal:F1} / {skills.TotalTrainCap:F0}";
        if (capFill != null) capFill.fillAmount = skills.TotalTrainCap > 0f ? Mathf.Clamp01(trainedTotal / skills.TotalTrainCap) : 0f;
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

    private static string BonusText(float bonus)
    {
        return bonus > 0.04f ? $"（+{bonus:F1}）" : ""; // F1表示で0.0にならない値だけ出す
    }
}
