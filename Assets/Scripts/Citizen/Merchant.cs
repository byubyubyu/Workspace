// 保存先: Assets/Scripts/Citizen/Merchant.cs
// 商人（市民の一種）。プレイヤーが話しかける対象のマーカー。固定位置に立つ（徘徊しない＝Wanderを付けない）。
//   段階2：話しかけ対象。段階3で在庫・価格・売買（お金＋物々交換）を持たせる。
using UnityEngine;

public class Merchant : MonoBehaviour
{
    // 段階3で：売り物リスト・買取・価格・支払い方法（お金/物々交換）を持たせる。
    //   今は ItemPicker が GetComponent<Merchant> で「商人かどうか」を判定するためのマーカー。
}
