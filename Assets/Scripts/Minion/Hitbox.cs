// 保存先: Assets/Scripts/Minion/Hitbox.cs
// 攻撃判定。兵士の子オブジェクトに付ける（isTrigger Box Collider＋Hitboxレイヤー）。
//   普段はColliderを無効化。Attackが判定フェーズ中だけ有効化する（GameObjectは常にアクティブ）。
//   OnTriggerEnterで相手Hurtboxを検知し、相手Core(IBattleInfo)へダメージを渡す（前回確定の案A）。
//   多段ヒット防止：この一振りで当てた相手を記録し、同じ相手に二重に当てない（範囲攻撃＝複数相手には当たる）。
//   Hitbox×Hurtboxのみのマトリクスなので、相手レイヤーのチェックは不要（来るのはHurtboxだけ）。
//   デバッグ可視化：Collider有効中だけ、判定範囲のBox枠を赤で描く（OnDrawGizmos）。
//   Sceneビューに表示。Game Viewのツールバーで Gizmos をオンにすれば Game Viewにも出る（エディタのみ）。
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Hitbox : MonoBehaviour
{
    private BoxCollider col;
    private float attackPower;     // 今回の一撃の実威力（Attackが設定）
    private float staggerDuration; // 今回の一撃のひるみ時間（Attackが設定）
    private GameObject hitEffect;  // 命中時に接触点へ出すエフェクト（Attackが設定）
    private IBattleInfo owner;     // 攻撃する本人（自分を殴らないためのガード用）
    private readonly HashSet<IBattleInfo> hitThisSwing = new HashSet<IBattleInfo>(); // この振りで当てた相手

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.enabled = false; // 普段は無効。判定フェーズ中だけ有効化する
    }

    public void Setup(IBattleInfo owner)
    {
        this.owner = owner;
    }

    // 判定フェーズ開始時にAttackが呼ぶ：今回の数値を構え、当てた相手リストをクリアしてColliderを有効化。
    public void Activate(float attackPower, float staggerDuration, GameObject hitEffect)
    {
        this.attackPower = attackPower;
        this.staggerDuration = staggerDuration;
        this.hitEffect = hitEffect;
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
        if (victim.Team == owner.Team) return; // 味方は殴らない（A-1：中立Team.Noneには当たる）
        if (hitThisSwing.Contains(victim)) return; // この振りで既に当てた相手は二重ヒットさせない

        hitThisSwing.Add(victim);
        victim.TakeDamage(new BattleInfo { attackPower = attackPower, staggerDuration = staggerDuration });

        // 受け手：命中した接触点に hitEffect を出す（ワールド・親なし。ParticleSystemなら自動消滅）。
        if (hitEffect != null)
        {
            Vector3 hitPos = other.ClosestPoint(transform.position); // 相手Collider上の最も近い点＝接触点の目安
            Instantiate(hitEffect, hitPos, Quaternion.identity);
        }
    }

    private void OnDrawGizmos()
    {
        // 判定が出ている（Collider有効）間だけ、判定範囲のBox枠を描く（デバッグ用・エディタのみ）。
        //   Sceneビューに表示。Game Viewのツールバーで Gizmos をオンにすれば Game Viewにも出る。
        var box = col != null ? col : GetComponent<BoxCollider>();
        if (box == null || !box.enabled) return;

        // BoxColliderのcenter/sizeを、回転・スケール・位置を反映して描く。
        Gizmos.color = Color.red;
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = prev;
    }
}
