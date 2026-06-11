// 保存先: Assets/Scripts/UI/CanvasSortOverride.cs
// 入れ子Canvasの描画順を強制的に手前にする。
//   Canvas.overrideSorting は「非アクティブ時に設定不可・無効化のたびにfalseへリセット」というUnityの癖があり、
//   プレハブに焼き込めない。そのためOnEnableのたびにコードで再適用する（これが定石）。
//   用途：商人の在庫スロットの在庫数・価格ラベルを、3Dモデル表示(DisplayRawImage)より手前に出す等。
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CanvasSortOverride : MonoBehaviour
{
    [SerializeField] private int sortingOrder = 1; // 親Canvas(0)より大きければ手前

    private void OnEnable()
    {
        var canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
    }
}
