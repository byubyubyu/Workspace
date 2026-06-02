// 保存先: Assets/Scripts/Minion/Hitbox.cs
// 攻撃判定。兵士の子オブジェクトに付ける（isTrigger Collider＋Hitboxレイヤー）。
//   普段はColliderを無効化。Attackが判定フェーズ中だけ有効化する（GameObjectは常にアクティブ）。
//   OnTriggerEnterで相手Hurtboxを検知し、相手Core(IBattleInfo)へダメージを渡す（前回確定の案A）。
//   多段ヒット防止：この一振りで当てた相手を記録し、同じ相手に二重に当てない。
//   Hitbox×Hurtboxのみのマトリクスなので、相手レイヤーのチェックは不要（来るのはHurtboxだけ）。
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    private Collider col;
    private float attackPower;     // 今回の一撃の実威力（Attackが設定）
    private float staggerDuration; // 今回の一撃のひるみ時間（Attackが設定）
    private IBattleInfo owner;     // 攻撃する本人（自分を殴らないためのガード用）
    private readonly HashSet<IBattleInfo> hitThisSwing = new HashSet<IBattleInfo>(); // この振りで当てた相手

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        col.enabled = false; // 普段は無効。判定フェーズ中だけ有効化する
    }

    // Attackが攻撃の本人を設定する（初期化時に1回）。
    public void Setup(IBattleInfo owner)
    {
        this.owner = owner;
    }

    // 判定フェーズ開始時にAttackが呼ぶ：今回の数値を構え、当てた相手リストをクリアしてColliderを有効化。
    public void Activate(float attackPower, float staggerDuration)
    {
        this.attackPower = attackPower;
        this.staggerDuration = staggerDuration;
        hitThisSwing.Clear();
        if (col != null) col.enabled = true;
    }

    // 判定フェーズ終了時にAttackが呼ぶ：Colliderを無効化。
    public void Deactivate()
    {
        if (col != null) col.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        var hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox == null) return;          // Hurtbox以外は無視（マトリクス上来ないはずだが保険）
        IBattleInfo victim = hurtbox.Owner;
        if (victim == null) return;
        if (victim == owner) return;          // 自分自身は殴らない
        if (hitThisSwing.Contains(victim)) return; // この振りで既に当てた相手は二重ヒットさせない

        hitThisSwing.Add(victim);
        victim.TakeDamage(new BattleInfo { attackPower = attackPower, staggerDuration = staggerDuration });
    }
}
