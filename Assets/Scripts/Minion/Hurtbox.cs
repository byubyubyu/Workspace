// 保存先: Assets/Scripts/Minion/Hurtbox.cs
// 食らい判定。兵士・建物の子オブジェクトに付ける（isTrigger Collider＋Hurtboxレイヤー）。
//   自分からは何もしない。Hitboxに当てられる側。
//   本体Core(IBattleInfo)への参照を持ち、HitboxがここからCoreを取得してダメージを渡す。
//   参照はCore初期化時にSetOwnerで渡される（前回確定の案ア・案A）。
using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    public IBattleInfo Owner { get; private set; }
    private Collider col;

    private void Awake() { col = GetComponent<Collider>(); }

    public void SetOwner(IBattleInfo owner)
    {
        Owner = owner;
    }

    // 被弾の受け口。Hitboxはここを呼ぶ（Ownerを直接呼ばない）。
    //   既定は素通し（従来挙動）。部位の倍率・部位HPはPartHurtboxがoverrideして差し込む。
    public virtual void TakeHit(BattleInfo info)
    {
        Owner?.TakeDamage(info);
    }

    // i-frame用：falseで自分のColliderを無効化（Hitboxに当たらなくなる＝無敵）。
    public void SetVulnerable(bool vulnerable)
    {
        if (col == null) col = GetComponent<Collider>();
        if (col != null) col.enabled = vulnerable;
    }
}
