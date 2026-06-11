// 保存先: Assets/Scripts/Player/CitizenProfileUIController.cs
// 市民プロフィール＝婚活画面（GDDセクション15）。市民（商人以外）に近づいてEで開く（ItemPickerが振り分け）。
//   ・表示：個体値一覧・結納金・所持コイン。「求婚」＝Family.TryPropose（結納金が払えれば成立）。
//   ・読み取り＋TryPropose呼び出しのみの一方向（進化・転生・ステータスUIと同じ流儀）。
//   ・市民から離れたら自動Close（商人UIと同じ）。他UIが開いたら自分から閉じる（自己監視方式）。
//   ・市民の名前は未実装＝「市民」固定表記（名前ランダムは家系図とセットで将来）。
using UnityEngine;
using UnityEngine.UI;

public class CitizenProfileUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleLabel;       // 「市民」
    [SerializeField] private Text skillsLabel;      // 個体値一覧（複数行）
    [SerializeField] private Text priceLabel;       // 結納金・所持コイン
    [SerializeField] private Button proposeButton;  // 求婚（払えない/既婚中は押せない）
    [SerializeField] private Text proposeLabel;
    [SerializeField] private EquipmentUIController equipmentUI; // 相互閉じ判定用（Instanceを持たないため参照で）
    [SerializeField] private float autoCloseRange = 3.5f;       // 相手から離れたら自動Close

    private CitizenSkills target;
    private Family family;
    private PlayerSkills playerSkills; // スキル名の取得用（市民とカタログ共通）
    private bool open;

    public bool IsOpen => open;
    // 開ける状態か（操作中プレイヤーが家系持ち＝人間か）。ItemPickerがE振り分け前に見る（魔族のEを無駄に食わない）。
    public bool CanOpen => ActivePlayer.Exists && ActivePlayer.Go.GetComponent<Family>() != null;
    public static CitizenProfileUIController Instance { get; private set; }

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
        if (proposeButton != null) proposeButton.onClick.AddListener(OnProposeClicked);
        open = false;
    }

    // ItemPickerのEから呼ばれる。同じ相手にもう一度Eで閉じる（トグル）。
    public void Open(CitizenSkills citizen)
    {
        if (citizen == null) return;
        if (open && citizen == target) { Close(); return; }

        var go = ActivePlayer.Exists ? ActivePlayer.Go : null;
        family = go != null ? go.GetComponent<Family>() : null;
        playerSkills = go != null ? go.GetComponent<PlayerSkills>() : null;
        if (family == null) return; // 人間（家系持ち）以外は対象外

        target = citizen;
        open = true;
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        open = false;
        target = null;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (!open) return;

        // 相手が消えた／離れた／他UIが開いた、で自分から閉じる。
        if (target == null || family == null) { Close(); return; }
        if ((target.transform.position - family.transform.position).sqrMagnitude > autoCloseRange * autoCloseRange) { Close(); return; }
        if (AnyOtherUIOpen()) { Close(); return; }

        Refresh();
    }

    private bool AnyOtherUIOpen()
    {
        if (BottleUIController.Instance != null && BottleUIController.Instance.IsOpen) return true;
        if (MinimapController.Instance != null && MinimapController.Instance.IsOpen) return true;
        if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen) return true;
        if (StatusUIController.Instance != null && StatusUIController.Instance.IsOpen) return true;
        if (equipmentUI != null && equipmentUI.IsOpen) return true;
        return false;
    }

    private void Refresh()
    {
        if (titleLabel != null) titleLabel.text = "市民";

        if (skillsLabel != null)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < target.SkillCount; i++)
            {
                string name = playerSkills != null ? playerSkills.GetSkillName(i) : $"スキル{i}";
                sb.AppendLine($"{name}　{target.GetValue(i):F0}");
            }
            skillsLabel.text = sb.ToString();
        }

        float price = family.PriceFor(target);
        float coins = family.CoinTotal;
        bool canPropose = !family.HasSpouse && coins >= price;
        if (priceLabel != null)
            priceLabel.text = family.HasSpouse ? "既婚（世代交代まで求婚不可）" : $"結納金 {price:F0}枚　（所持 {coins:F0}枚）";
        if (proposeLabel != null) proposeLabel.text = "求婚する";
        if (proposeButton != null) proposeButton.interactable = canPropose;
    }

    private void OnProposeClicked()
    {
        if (family == null || target == null) return;
        if (family.TryPropose(target))
        {
            Debug.Log("[CitizenProfileUI] 求婚成立");
            Close();
        }
    }
}
