// 保存先: Assets/Scripts/Demon/ReincarnationUIController.cs
// 転生画面（GDDセクション15）。魔族の死亡→転生待ちで自動的に開き、素体を選んで復活する。
//   ・DemonCore.OnAwaitReincarnation を購読して開く（CoreはUIを参照しない＝一方向の疎結合）。
//   ・表示：魂ポイント／素体カタログのボタン一覧（未解放＝グレー＋必要pt表示）。
//   ・選択→DemonCore.Reincarnate(素体番号)→成功で閉じる。番号指定＝マルチ方針のID参照。
//   ・死亡中専用の画面なので、I/M/C等との相互閉じは不要（死亡中は他UIの操作対象がない）。
//   ・見た目は進化画面と同じ簡易文法。素体の3D表示などグラフィカル化は磨きフェーズ。
using UnityEngine;
using UnityEngine.UI;

public class ReincarnationUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;     // 転生画面のパネル
    [SerializeField] private Text titleLabel;      // 見出し（「転生先を選ぶ」）
    [SerializeField] private Text soulLabel;       // 魂ポイントの数値
    [SerializeField] private Button[] bodyButtons; // 素体ボタン（カタログ数より多いぶんは非表示）
    [SerializeField] private Text[] bodyLabels;    // 各ボタンの文字（素体名＋解放状態）

    private DemonCore demon;   // 購読中の魔族（陣営選択後にUpdateで遅延フック）
    private DemonSoul soul;
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
        for (int i = 0; i < bodyButtons.Length; i++)
        {
            int index = i; // クロージャ用
            if (bodyButtons[i] != null) bodyButtons[i].onClick.AddListener(() => OnBodyClicked(index));
        }
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

        if (open) Refresh(); // 開いている間は毎フレーム更新（魂ptは変わらないが解放状態の取りこぼし防止）
    }

    private void Open()
    {
        if (open || demon == null) return;
        open = true;
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    private void Close()
    {
        if (!open) return;
        open = false;
        if (panel != null) panel.SetActive(false);
    }

    // 表示更新：魂ポイント・素体ボタン（未解放はグレー＋必要pt）。
    private void Refresh()
    {
        if (demon == null || demon.Catalog == null) return;
        var bodies = demon.Catalog.Bodies;

        if (titleLabel != null) titleLabel.text = "転生先を選ぶ";
        if (soulLabel != null) soulLabel.text = $"魂 {(soul != null ? soul.Points : 0f):F0}pt";

        if (bodies.Count > bodyButtons.Length)
            Debug.LogWarning($"[ReincarnationUI] 素体{bodies.Count}件がボタン数{bodyButtons.Length}を超過（あふれた分は非表示）");

        for (int i = 0; i < bodyButtons.Length; i++)
        {
            bool exists = i < bodies.Count && bodies[i] != null;
            if (bodyButtons[i] != null) bodyButtons[i].gameObject.SetActive(exists);
            if (!exists) continue;

            bool unlocked = demon.IsBodyUnlocked(i);
            if (bodyLabels[i] != null)
                bodyLabels[i].text = unlocked
                    ? bodies[i].BodyName
                    : $"{bodies[i].BodyName}\n必要 魂{bodies[i].RequiredSoulPoints:F0}pt";
            bodyButtons[i].interactable = unlocked;
        }
    }

    private void OnBodyClicked(int index)
    {
        if (demon == null) return;
        if (demon.Reincarnate(index))
        {
            Debug.Log($"[ReincarnationUI] 転生 → {demon.Body.BodyName}");
            Close();
        }
    }
}
