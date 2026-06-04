// 保存先: Assets/Scripts/UI/MinimapTypeButton.cs
// 兵種選択ボタン1個。MinimapControllerが生成時に Setup(兵種, onClick) で結びつけ、
// 押されたらその兵種を通知する。表示名は MinionData のアセット名を使う。
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MinimapTypeButton : MonoBehaviour
{
    [SerializeField] private Text label; // ボタン上の表示テキスト（Legacy Text）

    private MinionData data;
    private Action<MinionData> onClicked;

    public void Setup(MinionData data, Action<MinionData> onClicked)
    {
        this.data = data;
        this.onClicked = onClicked;
        if (label != null) label.text = data != null ? data.name : "?";

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => this.onClicked?.Invoke(this.data));
    }
}
