// 保存先: Assets/Scripts/Minion/Hurtbox.cs
// 食らい判定。兵士・建物の子オブジェクトに付ける（isTrigger Collider＋Hurtboxレイヤー）。
//   自分からは何もしない。Hitboxに当てられる側。
//   本体Core(IBattleInfo)への参照を持ち、HitboxがここからCoreを取得してダメージを渡す。
//   参照はCore初期化時にSetOwnerで渡される（前回確定の案ア・案A）。
using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    public IBattleInfo Owner { get; private set; }

    public void SetOwner(IBattleInfo owner)
    {
        Owner = owner;
    }
}
