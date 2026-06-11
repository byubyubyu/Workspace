// 保存先: Assets/Scripts/Demon/ReincarnationUIController.cs
// 転生画面（GDDセクション15）。魔族の死亡→転生待ちで自動的に開き、素体を選んで復活する。グラフィカル版（2026-06-11）。
//   ・DemonCore.OnAwaitReincarnation を購読して開く（CoreはUIを参照しない＝一方向の疎結合）。
//   ・レイアウト：全画面黒。タイトル＋右上に魂ポイント／
//     中央＝素体リスト（EvolutionOptionSlot共用の動的生成行。3Dプレビュー＝骨格＋初期部位を
//     DemonBodyPreviewで組み立て。未解放は「魂50pt」バッジ＋暗転）。
//   ・行クリック→詳細ビュー：大3D＋ステ一覧（HP/防御/攻撃/部位進化上限/体格）＋「転生」確定／「戻る」
//     （転生は不可逆のため確定式。進化画面と同じ文法）。
//   ・3D表示は進化画面の裏空間（EvolutionDisplay・専用カメラ・EvolutionRT）を共用
//     （死亡中は進化画面が開かないため衝突しない。保険としてOpen時に進化画面をCloseする）。
//   ・選択→DemonCore.Reincarnate(素体番号)→成功で閉じる。番号指定＝マルチ方針のID参照。
//   ・死亡中専用の画面なので、I/M/C等との相互閉じは不要（死亡中は他UIの操作対象がない）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReincarnationUIController : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleLabel;      // 「転生先を選ぶ」
    [SerializeField] private Text soulLabel;       // 魂ポイント（右上）

    [Header("素体リスト（動的生成・進化画面の行を共用）")]
    [SerializeField] private EvolutionOptionSlot slotPrefab;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private GameObject listGroup;

    [Header("詳細ビュー（行クリックで切替）")]
    [SerializeField] private GameObject detailGroup;
    [SerializeField] private RectTransform detailBodyFrame; // 素体の大表示枠
    [SerializeField] private Text detailNameLabel;          // 素体名
    [SerializeField] private Text detailStatsLabel;         // ステ一覧
    [SerializeField] private Button confirmButton;          // 「転生」（未解放はdisabled）
    [SerializeField] private Button backButton;             // 「戻る」

    [Header("3Dモデル表示（進化画面の裏空間を共用）")]
    [SerializeField] private MerchantDisplay bodyDisplay;   // EvolutionDisplay（stripBehaviours=true・centerBounds=true）
    [SerializeField] private Camera displayCamera;          // EvolutionCamera
    [SerializeField] private RectTransform rawImageRect;    // このパネル上のRawImage（EvolutionRTを映す）
    [SerializeField] private Camera uiCamera;               // Screen Space Overlayならnull
    [SerializeField] private float depth = 10f;
    [SerializeField] private float listViewSize = 1.6f;     // リスト行の素体モデルの最大辺
    [SerializeField] private float detailViewSize = 4f;     // 詳細の大表示モデルの最大辺

    private readonly List<EvolutionOptionSlot> slots = new List<EvolutionOptionSlot>();
    private DemonCore demon;   // 購読中の魔族（陣営選択後にUpdateで遅延フック）
    private DemonSoul soul;
    private int detailIndex = -1; // 詳細ビュー中の素体番号（-1=リスト表示中）
    private bool open;

    public bool IsOpen => open;
    public static ReincarnationUIController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (demon != null) demon.OnAwaitReincarnation -= Open;
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (backButton != null) backButton.onClick.AddListener(ShowListView);
        open = false;
    }

    private void Update()
    {
        // 魔族はシーン開始時非アクティブ（陣営選択後に有効化）のため、ここで遅延フックする。
        if (demon == null && ActivePlayer.Exists)
        {
            var activeDemon = ActivePlayer.Go.GetComponent<DemonCore>();
            if (activeDemon != null)
            {
                demon = activeDemon;
                soul = activeDemon.GetComponent<DemonSoul>();
                demon.OnAwaitReincarnation += Open;
                if (demon.AwaitingReincarnation) Open(); // フック前に死んでいた場合の保険
            }
        }

        if (open) RefreshDynamic(); // 魂pt・解放状態を毎フレーム反映（F10デバッグ加算等の取りこぼし防止）
    }

    private void Open()
    {
        if (open || demon == null) return;
        // 進化画面が開いたまま死んだ場合の保険（裏空間・カメラを共用しているため先に閉じる）。
        if (EvolutionUIController.Instance != null && EvolutionUIController.Instance.IsOpen)
            EvolutionUIController.Instance.Close();
        open = true;
        if (panel != null) panel.SetActive(true);
        if (displayCamera != null) displayCamera.enabled = true;
        if (titleLabel != null) titleLabel.text = "転生先を選ぶ";
        BuildRows();
        ShowListView();
    }

    private void Close()
    {
        if (!open) return;
        open = false;
        detailIndex = -1;
        if (panel != null) panel.SetActive(false);
        if (displayCamera != null) displayCamera.enabled = false;
        ClearRows();
        if (bodyDisplay != null) bodyDisplay.Clear();
    }

    // ============ リストの生成 ============

    private void BuildRows()
    {
        ClearRows();
        if (demon == null || demon.Catalog == null || slotPrefab == null || slotContainer == null) return;
        var bodies = demon.Catalog.Bodies;
        for (int i = 0; i < bodies.Count; i++)
        {
            if (bodies[i] == null) continue;
            int index = i; // クロージャ用
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.Setup(bodies[i].BodyName, 0f, null, () => ShowDetailView(index));
            slot.SetBadge(demon.IsBodyUnlocked(i) ? null : $"魂{bodies[i].RequiredSoulPoints:F0}pt");
            slots.Add(slot);
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            slots[i].transform.SetParent(null, false); // 遅延Destroy対策（進化画面と同じ罠対応）
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
        if (demon == null || demon.Catalog == null) return;
        var bodies = demon.Catalog.Bodies;
        if (index < 0 || index >= bodies.Count || bodies[index] == null) return;
        detailIndex = index;
        var body = bodies[index];
        if (listGroup != null) listGroup.SetActive(false);
        if (detailGroup != null) detailGroup.SetActive(true);
        if (detailNameLabel != null) detailNameLabel.text = body.BodyName;
        if (detailStatsLabel != null) detailStatsLabel.text = BuildStatsText(body, demon.IsBodyUnlocked(index));
        RefreshDetailDisplay(body);
        RefreshDynamic();
    }

    // リスト行の3Dプレビュー配置：各行のpartFrame位置に「骨格＋初期部位」の組み立て済みプレビューを置く。
    //   行生成直後はレイアウト未確定のためForceRebuildLayoutImmediate（進化・商人と同じ罠対応）。
    private void RefreshListDisplay()
    {
        if (bodyDisplay == null || displayCamera == null || rawImageRect == null) return;
        if (demon == null || demon.Catalog == null) return;
        if (slotContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer);

        var bodies = demon.Catalog.Bodies;
        var instances = new List<GameObject>(slots.Count);
        var positions = new List<Vector3>(slots.Count);
        for (int i = 0; i < slots.Count && i < bodies.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.PartFrame == null || bodies[i] == null) continue;
            instances.Add(DemonBodyPreview.Build(bodies[i]));
            positions.Add(UIModelProjection.FrameToWorld(slot.PartFrame, rawImageRect, uiCamera, displayCamera, depth));
        }
        bodyDisplay.ViewSize = listViewSize;
        bodyDisplay.UpdateDisplayInstances(instances, positions, null);
    }

    // 詳細ビューの3D配置：選んだ素体1体をdetailBodyFrameの位置に大表示する。
    private void RefreshDetailDisplay(BodyData body)
    {
        if (bodyDisplay == null || displayCamera == null || rawImageRect == null || detailBodyFrame == null) return;
        Canvas.ForceUpdateCanvases();
        var instances = new List<GameObject> { DemonBodyPreview.Build(body) };
        var positions = new List<Vector3>
        {
            UIModelProjection.FrameToWorld(detailBodyFrame, rawImageRect, uiCamera, displayCamera, depth),
        };
        bodyDisplay.ViewSize = detailViewSize;
        bodyDisplay.UpdateDisplayInstances(instances, positions, null);
    }

    // 素体のステ一覧（絶対値表示。死亡時は前の体が初期化されるため差分表記はしない）。
    private static string BuildStatsText(BodyData body, bool unlocked)
    {
        var sb = new System.Text.StringBuilder();
        if (body.Vitality != null)
        {
            sb.AppendLine($"HP {body.Vitality.hp:F0}　防御 {body.Vitality.defense:F0}");
        }
        sb.AppendLine($"攻撃 {body.AttackPower:F0}");
        sb.AppendLine($"部位進化の上限 Tier{body.MaxPartTier}");
        sb.AppendLine($"体格 ×{body.Scale:F1}");
        if (!unlocked) sb.AppendLine($"未解放（必要 魂{body.RequiredSoulPoints:F0}pt）");
        return sb.ToString().TrimEnd();
    }

    // ============ 毎フレーム更新（魂pt・解放状態） ============

    private void RefreshDynamic()
    {
        if (demon == null || demon.Catalog == null) return;
        if (soulLabel != null) soulLabel.text = $"魂 {(soul != null ? soul.Points : 0f):F0}pt";

        var bodies = demon.Catalog.Bodies;
        for (int i = 0; i < slots.Count && i < bodies.Count; i++)
        {
            if (slots[i] == null || bodies[i] == null) continue;
            bool unlocked = demon.IsBodyUnlocked(i);
            slots[i].SetAffordable(unlocked);
            slots[i].SetBadge(unlocked ? null : $"魂{bodies[i].RequiredSoulPoints:F0}pt");
        }
        if (confirmButton != null && detailIndex >= 0)
            confirmButton.interactable = demon.IsBodyUnlocked(detailIndex);
    }

    // ============ 転生の確定 ============

    private void OnConfirmClicked()
    {
        if (demon == null || detailIndex < 0) return;
        if (demon.Reincarnate(detailIndex))
        {
            Debug.Log($"[ReincarnationUI] 転生 → {demon.Body.BodyName}");
            Close();
        }
    }
}
