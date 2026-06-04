// 保存先: Assets/Scripts/UI/MinimapMarker.cs
// ミニマップのBaseドット1個。クリックされたら、保持しているBaseを通知する。
//   ・MinimapControllerが生成時に Setup(base, onClicked) で結びつける。
//   ・クリック判定はuGUI標準（IPointerClickHandler＋CanvasのGraphicRaycaster＋EventSystem）。
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapMarker : MonoBehaviour, IPointerClickHandler
{
    public Base Base { get; private set; }
    private Action<Base> onClicked;

    public void Setup(Base baseRef, Action<Base> onClicked)
    {
        this.Base = baseRef;
        this.onClicked = onClicked;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke(Base);
    }
}
