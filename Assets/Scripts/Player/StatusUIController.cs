// 保存先: Assets/Scripts/Player/StatusUIController.cs
// ステータス画面（人間用・仮キーP）。グラフィカル版（進化画面と同じ3カラム文法・2026-06-11改修）。
//   ・レイアウト：左＝メインカメラの寄り表示（実機キャラ＝装備が見える・装備画面と同じ流儀）／
//     中央＝鍛錬合計バー＋スキル行（StatusSkillRowの動的生成。バッジ↑固↓・値バー・遺伝＋鍛錬内訳）／
//     右＝年齢・能力値（HP/スタミナ/攻撃/防御/軽減。スキル由来分は「（+n）」併記）・武器のワザ一覧。
//   ・読み取り専用＋唯一の書き込みが PlayerSkills.SetMode（スキル行クリックで ↑→固→↓ をトグル）。
//     PlayerSkills/Age/Core/Stamina の公開プロパティを読むだけ＝一方向（進化・転生UIと同じ流儀）。
//   ・人間操作中のみ（魔族は進化画面が実質ステータス画面）。ActivePlayerで判定。
//   ・他UI（瓶/装備/M/商人/プロフィール）が開いている間はPを無視し、開いている最中に他UIが開いたら
//     自分から閉じる（相互閉じと同じ結果を自分の監視で実現＝他UIのコードを触らない）。
//   ・開いている間は毎フレーム更新（スキル値・年齢は常に動くため）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StatusUIController : MonoBehaviour
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

    [Header("左カラム＝メインカメラの寄り表示（装備画面と同じ流儀）")]
    [SerializeField] private Camera mainCamera;        // 未設定ならCamera.main
    [SerializeField] private TPSCamera tpsCamera;
    [SerializeField] private float closeUpDistance = 2.5f;
    [SerializeField] private float closeUpPitch = 5f;
    [SerializeField] private float closeUpHeight = 1.2f;
    [SerializeField] private float closeUpFarClip = 8f;
    [SerializeField] private float leftColumnWidth = 0.3f;

    [SerializeField] private EquipmentUIController equipmentUI; // 相互閉じ判定用（Instanceを持たないため参照で）
    [SerializeField] private Key toggleKey = Key.P;

    private PlayerCombatCore core;
    private PlayerSkills skills;
    private Age age;
    private Stamina stamina;
    private EquipmentHolder equipment;
    private readonly List<StatusSkillRow> rows = new List<StatusSkillRow>();
    private bool open;

    // メインカメラの復元用（装備・進化画面と同じ）。
    private Rect savedCamRect = new Rect(0f, 0f, 1f, 1f);
    private float savedFarClip;
    private CameraClearFlags savedClearFlags;
    private Color savedBgColor;
    private int savedCullingMask;  // 開く前の描画レイヤー（自分以外を消すため絞る）
    private bool cullingMaskSaved; // ※-1(Everything)が正規値のため、保存済みかはboolで判定

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
            // Pキーは「閉じる」専用。開くのはC画面（装備）の「ステータス」ボタンからの遷移のみ。
            if (open) Close();
        }

        if (!open) return;
        if (AnyOtherUIOpen()) { Close(); return; } // 他UIが開いたら自分から閉じる（相互閉じ）
        CloseUpIsolator.Refresh(); // 装備変更等で見た目が作り直された時の当て直し（装備・進化画面と同じ）
        Refresh();
    }

    // 外部（C画面の「ステータス」ボタン）からの遷移用。呼び出し側が他UIを閉じてから呼ぶ。
    public void OpenExternal()
    {
        if (open) return;
        var activeCore = ActivePlayer.Exists ? ActivePlayer.Go.GetComponent<PlayerCombatCore>() : null;
        if (activeCore == null || AnyOtherUIOpen()) return;
        Open(activeCore);
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
        equipment = target.GetComponent<EquipmentHolder>();
        open = true;
        if (panel != null) panel.SetActive(true);

        // 左カラムにメインカメラを絞り、自キャラ正面のクローズアップへ（閉じたら元に戻す）。
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam != null)
        {
            savedCamRect = cam.rect;
            savedFarClip = cam.farClipPlane;
            savedClearFlags = cam.clearFlags;
            savedBgColor = cam.backgroundColor;
            cam.rect = new Rect(0f, 0f, leftColumnWidth, 1f);
            cam.farClipPlane = closeUpFarClip;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            // 自分以外を消す：自分の見た目をCloseUpViewレイヤーへ移し、カメラはそれだけ映す（装備・進化画面と同じ）。
            savedCullingMask = cam.cullingMask;
            cullingMaskSaved = true;
            cam.cullingMask = CloseUpIsolator.Mask;
        }
        if (tpsCamera != null) tpsCamera.BeginCloseUp(closeUpDistance, closeUpPitch, closeUpHeight);
        CloseUpIsolator.Isolate(target.gameObject);

        BuildRows();
        Refresh();
    }

    private void Close()
    {
        open = false;
        if (panel != null) panel.SetActive(false);
        ClearRows();

        // メインカメラを全画面・元の視点に戻す。
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam != null)
        {
            cam.rect = savedCamRect;
            cam.farClipPlane = savedFarClip;
            cam.clearFlags = savedClearFlags;
            cam.backgroundColor = savedBgColor;
            if (cullingMaskSaved) { cam.cullingMask = savedCullingMask; cullingMaskSaved = false; }
        }
        if (tpsCamera != null) tpsCamera.EndCloseUp();
        CloseUpIsolator.Restore(); // 自分の見た目レイヤーを元に戻す
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
