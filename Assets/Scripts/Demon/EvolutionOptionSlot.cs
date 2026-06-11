// 保存先: Assets/Scripts/Demon/EvolutionOptionSlot.cs
// 進化画面の「候補1件ぶんの行」。MerchantStockSlot流儀の動的生成行（dumb view）。
//   ・partFrame：進化先部位の3Dモデル枠。EvolutionUIControllerがこの位置から裏空間（MerchantDisplay）へ
//     PartData.partPrefabを生成して重ねる。
//   ・nameLabel：「頭：頭 → 硬い頭」形式。costLabel：必要捕食ポイントのバッジ（「30pt」）。
//   ・クリックで詳細ビューへ（コールバックはSetupで注入＝こちらは誰が何をするか知らない）。
//   ・ポイント不足は行ごと暗転（CanvasGroup.alpha）＋クリック不可。
using UnityEngine;
using UnityEngine.UI;

public class EvolutionOptionSlot : MonoBehaviour
{
    [SerializeField] private RectTransform partFrame; // 進化先部位の3Dモデル表示枠
    [SerializeField] private Text nameLabel;          // 「頭：頭 → 硬い頭」
    [SerializeField] private Text costLabel;          // 「30pt」
    [SerializeField] private Button button;           // 行全体クリック（詳細ビューへ）
    [SerializeField] private CanvasGroup canvasGroup; // 不足時の暗転用（行ルートに付ける）

    public RectTransform PartFrame => partFrame;
    public PartData Target { get; private set; }      // 進化先（3Dモデル配置用にControllerが読む）

    // Controllerが生成時に呼ぶ。表示内容とクリック時コールバックを差し込む。
    public void Setup(string label, float cost, PartData target, System.Action onClick)
    {
        Target = target;
        if (nameLabel != null) nameLabel.text = label;
        if (costLabel != null) costLabel.text = $"{cost:F0}pt";
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }

    // バッジ文言を直接指定する（転生画面の「魂50pt」等。null/空＝バッジごと非表示）。Setupの後に呼ぶ。
    public void SetBadge(string text)
    {
        if (costLabel == null) return;
        var badge = costLabel.transform.parent != null ? costLabel.transform.parent.gameObject : costLabel.gameObject;
        bool show = !string.IsNullOrEmpty(text);
        badge.SetActive(show);
        if (show) costLabel.text = text;
    }

    // ポイントの増減で毎フレーム呼ばれる（押せるか＋暗転）。
    public void SetAffordable(bool value)
    {
        if (button != null) button.interactable = value;
        if (canvasGroup != null) canvasGroup.alpha = value ? 1f : 0.45f;
    }
}
