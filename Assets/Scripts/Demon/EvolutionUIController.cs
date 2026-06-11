// 保存先: Assets/Scripts/Demon/EvolutionUIController.cs
// 進化画面（魔族のC画面。人間の装備画面に相当）。GDDセクション14。
//   ・Cキーで開閉（魔族操作中のみ。人間のときはEquipmentUIControllerが従来通り担当）。
//   ・進化は部位単位：候補ボタン＝「全スロットの進化候補」のフラット一覧
//     （スロット名：現部位 → 進化先＋必要ポイント。不足時は押せない）。
//   ・候補クリック→DemonCore.EvolvePart(スロット番号, 候補番号)→成功でRefresh（閉じない＝連続進化できる）。
//   ・I/M/商人との相互閉じは既存の流儀（各UIが開く時にこちらをCloseする）。
//   ・見た目は現状シンプル版。候補の体を3Dで見せるグラフィカル版は磨きフェーズで（商人画面の文法を流用予定）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EvolutionUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;        // 進化画面のパネル
    [SerializeField] private Text formLabel;          // 現在形態名
    [SerializeField] private Text poolLabel;          // 捕食ポイントの数値（例 30/100）
    [SerializeField] private Image poolFill;          // 捕食ポイントゲージ（Image Type=Filled）
    [SerializeField] private Button[] optionButtons;  // 進化候補ボタン（候補数より多いぶんは非表示）
    [SerializeField] private Text[] optionLabels;     // 各ボタンの文字（行き先名＋必要ポイント）
    [SerializeField] private Key toggleKey = Key.C;

    // 表示中の候補1件＝（スロット番号・候補番号）。ボタン番号→この対をDemonCore.EvolvePartへ渡す。
    private struct Candidate
    {
        public int slot;
        public int option;
        public string label;
        public float cost;
    }

    private readonly List<Candidate> candidates = new List<Candidate>();
    private bool open;
    private DemonCore demon;

    public bool IsOpen => open;
    public static EvolutionUIController Instance { get; private set; }

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
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // クロージャ用
            if (optionButtons[i] != null) optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
        }
        open = false;
    }

    private void Update()
    {
        // 魔族を操作している時だけCを引き受ける（人間のCはEquipmentUIControllerが担当）。
        var activeDemon = ActivePlayer.Exists ? ActivePlayer.Go.GetComponent<DemonCore>() : null;
        if (activeDemon == null) return;

        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
        {
            // 商人UI中のCは取引キャンセル→進化画面（画面の切り替え・既存の流儀）。
            if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen)
            {
                MerchantUIController.Instance.Close();
                Open(activeDemon);
                return;
            }
            // M画面中のCはMを閉じて進化画面。
            if (MinimapController.Instance != null && MinimapController.Instance.IsOpen)
            {
                MinimapController.Instance.Close();
                Open(activeDemon);
                return;
            }
            if (open) Close();
            else Open(activeDemon);
        }

        if (open) Refresh(); // ポイントは戦闘で常に変わるので開いている間は毎フレーム更新
    }

    public void Open(DemonCore target)
    {
        if (open || target == null) return;
        demon = target;
        open = true;
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (!open) return;
        open = false;
        demon = null;
        if (panel != null) panel.SetActive(false);
    }

    // 表示更新：素体名・ポイントゲージ・部位進化の候補ボタン（不足時はinteractable=false）。
    private void Refresh()
    {
        if (demon == null || demon.Body == null) return;
        var pool = demon.DevourPool;

        if (poolLabel != null) poolLabel.text = pool != null ? $"{pool.Current:F0} / {pool.Max:F0}" : "-";
        if (poolFill != null) poolFill.fillAmount = pool != null && pool.Max > 0f ? pool.Current / pool.Max : 0f;

        // 全スロットの進化候補をフラットに集める（候補＝スロット番号＋候補番号）。
        candidates.Clear();
        var slots = demon.Body.Slots;
        for (int s = 0; s < demon.SlotCount; s++)
        {
            var part = demon.GetEquippedPart(s);
            if (part == null || part.evolutions == null) continue;
            for (int o = 0; o < part.evolutions.Count; o++)
            {
                var opt = part.evolutions[o];
                if (opt == null || opt.target == null) continue;
                if (opt.target.tier > demon.Body.MaxPartTier) continue; // 素体の進化上限を超える候補は出さない
                string slotName = s < slots.Count ? slots[s].slotName : "";
                candidates.Add(new Candidate
                {
                    slot = s,
                    option = o,
                    cost = opt.cost,
                    label = $"{slotName}：{part.partName} → {opt.target.partName}\n必要 {opt.cost:F0}pt",
                });
            }
        }
        if (candidates.Count > optionButtons.Length)
            Debug.LogWarning($"[EvolutionUI] 進化候補{candidates.Count}件がボタン数{optionButtons.Length}を超過（あふれた分は非表示）");

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool exists = i < candidates.Count;
            if (optionButtons[i] != null) optionButtons[i].gameObject.SetActive(exists);
            if (!exists) continue;
            if (optionLabels[i] != null) optionLabels[i].text = candidates[i].label;
            optionButtons[i].interactable = pool != null && pool.CanAfford(candidates[i].cost);
        }

        if (formLabel != null)
            formLabel.text = candidates.Count > 0 ? $"素体：{demon.Body.BodyName}" : $"素体：{demon.Body.BodyName}（進化先なし）";
    }

    private void OnOptionClicked(int index)
    {
        if (demon == null || index < 0 || index >= candidates.Count) return;
        var c = candidates[index];
        if (demon.EvolvePart(c.slot, c.option))
        {
            Debug.Log($"[EvolutionUI] 部位進化成功 → {demon.GetEquippedPart(c.slot).partName}");
            Refresh(); // 部位単位なので閉じずに続けて進化できる
        }
    }
}
