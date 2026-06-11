// 保存先: Assets/Scripts/Player/StatusSkillRow.cs
// ステータス画面の「スキル1件ぶんの行」。EvolutionOptionSlot流儀の動的生成行（dumb view）。
//   ・modeBadge/modeLabel：上げ下げ指定のバッジ（↑緑／固グレー／↓赤。クリックでトグル＝Controllerのコールバック）。
//   ・nameLabel：スキル名。barFill：値バー（実効値/上限）。valueLabel：数値（遺伝＋鍛錬の内訳付き）。
//   ・市民プロフィールはSetSimpleで共用（バッジ・内訳を出さない、値のみ）。
using UnityEngine;
using UnityEngine.UI;

public class StatusSkillRow : MonoBehaviour
{
    [SerializeField] private Image modeBadge;   // バッジ背景（モードで色が変わる）
    [SerializeField] private Text modeLabel;    // 「↑」「固」「↓」
    [SerializeField] private Text nameLabel;    // スキル名
    [SerializeField] private RectTransform barBG; // 値バーの背景（SetSimpleのレイアウト詰めに使う）
    [SerializeField] private Image barFill;     // 値バー（Image Type=Filled・Horizontal。モード色に連動）
    [SerializeField] private Text valueLabel;   // 「62.4（遺伝20.0＋鍛錬42.4）」
    [SerializeField] private Button button;     // 行全体クリック（モードトグル）

    private static readonly Color RaiseColor = new Color(0.23f, 0.43f, 0.07f, 1f); // ↑緑
    private static readonly Color LockColor  = new Color(0.37f, 0.37f, 0.35f, 1f); // 固グレー
    private static readonly Color LowerColor = new Color(0.64f, 0.18f, 0.18f, 1f); // ↓赤
    private static readonly Color SimpleColor = new Color(0.73f, 0.46f, 0.09f, 1f); // 市民用（琥珀）

    // Controllerが生成時に呼ぶ（クリック時コールバックの注入。市民用はonClick=nullで押せない行になる）。
    public void Setup(System.Action onClick)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        if (onClick != null) button.onClick.AddListener(() => onClick());
        else button.interactable = false;
    }

    // ステータス画面用：モード・実効値・内訳を毎フレーム反映する。
    public void SetData(string skillName, float value, float max, float inherited, float trained, PlayerSkills.GrowthMode mode)
    {
        if (nameLabel != null) nameLabel.text = skillName;
        if (barFill != null) barFill.fillAmount = max > 0f ? Mathf.Clamp01(value / max) : 0f;
        if (valueLabel != null) valueLabel.text = $"{value:F1}（遺伝{inherited:F1}＋鍛錬{trained:F1}）";
        if (modeLabel != null)
            modeLabel.text = mode switch
            {
                PlayerSkills.GrowthMode.Raise => "↑",
                PlayerSkills.GrowthMode.Lock => "固",
                _ => "↓",
            };
        var color = mode switch
        {
            PlayerSkills.GrowthMode.Raise => RaiseColor,
            PlayerSkills.GrowthMode.Lock => LockColor,
            _ => LowerColor,
        };
        if (modeBadge != null) modeBadge.color = color;
        if (barFill != null) barFill.color = color; // バーもモード色に連動（一目で↑固↓が分かる）
    }

    // 市民プロフィール用：バッジ・内訳なしの簡易表示（名前＋バー＋値のみ）。
    //   コンパクト窓（幅560想定）に収まるよう、バッジぶんを詰めたレイアウトに組み替える。
    public void SetSimple(string skillName, float value, float max)
    {
        if (modeBadge != null) modeBadge.gameObject.SetActive(false);
        if (nameLabel != null)
        {
            nameLabel.text = skillName;
            var rt = nameLabel.rectTransform;
            rt.offsetMin = new Vector2(16f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(180f, rt.offsetMax.y);
        }
        if (barBG != null)
        {
            barBG.offsetMin = new Vector2(190f, barBG.offsetMin.y);
            barBG.offsetMax = new Vector2(-100f, barBG.offsetMax.y);
        }
        if (barFill != null)
        {
            barFill.fillAmount = max > 0f ? Mathf.Clamp01(value / max) : 0f;
            barFill.color = SimpleColor;
        }
        if (valueLabel != null)
        {
            valueLabel.text = $"{value:F0}";
            var rt = valueLabel.rectTransform;
            rt.offsetMin = new Vector2(-90f, rt.offsetMin.y);
        }
    }
}
