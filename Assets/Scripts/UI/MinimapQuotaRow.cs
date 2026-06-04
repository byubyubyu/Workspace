// 保存先: Assets/Scripts/UI/MinimapQuotaRow.cs
// 兵種1種ぶんの行：兵種名と「− 数 ＋」＋テキスト入力を持ち、現在の数を保持する。
// MinimapControllerが生成時に Setup(兵種, 初期値, 最小, 最大) で結びつける。−／＋／入力は内部で自動結線。
using UnityEngine;
using UnityEngine.UI;

public class MinimapQuotaRow : MonoBehaviour
{
    [SerializeField] private Text nameLabel;        // 兵種名（Legacy Text）
    [SerializeField] private Button minusButton;    // −
    [SerializeField] private Button plusButton;     // ＋
    [SerializeField] private InputField countInput; // 数のテキスト入力（Legacy InputField）

    private int min, max, count;

    public MinionData Type { get; private set; }
    public int Count => count;

    public void Setup(MinionData type, int defaultCount, int min, int max)
    {
        Type = type;
        this.min = min;
        this.max = max;
        if (nameLabel != null) nameLabel.text = type != null ? type.name : "?";

        if (minusButton != null) { minusButton.onClick.RemoveAllListeners(); minusButton.onClick.AddListener(() => SetCount(count - 1)); }
        if (plusButton != null) { plusButton.onClick.RemoveAllListeners(); plusButton.onClick.AddListener(() => SetCount(count + 1)); }
        if (countInput != null) { countInput.onEndEdit.RemoveAllListeners(); countInput.onEndEdit.AddListener(OnInput); }

        SetCount(defaultCount);
    }

    private void OnInput(string text)
    {
        if (int.TryParse(text, out int v)) SetCount(v);
        else UpdateText(); // 不正入力は現在値に戻す
    }

    private void SetCount(int v)
    {
        count = Mathf.Clamp(v, min, max);
        UpdateText();
    }

    private void UpdateText()
    {
        if (countInput != null) countInput.text = count.ToString();
    }
}
