// 保存先: Assets/Scripts/UI/MinimapDragPanner.cs
// ミニマップのパネルに付け、ドラッグをMinimapControllerへ転送してカメラをパンさせる。
//   ・EventSystem方式（IDragHandler）。Baseマーカーのクリック（IPointerClickHandler）と同じ系統で競合しにくい。
//   ・付ける場所：背景RawImageとBaseマーカーの「共通の親」になるパネル（mapPanel等）に付けること。
//     ドラッグは押した子から親へバブリングして届くため、共通の親に1つあれば空白部分・マーカー上の
//     どちらから始めても拾える。背景RawImageは Raycast Target = true にしておく。
using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapDragPanner : MonoBehaviour, IDragHandler
{
    [SerializeField] private MinimapController controller;

    public void OnDrag(PointerEventData eventData)
    {
        if (controller != null) controller.OnMinimapDrag(eventData);
    }
}
