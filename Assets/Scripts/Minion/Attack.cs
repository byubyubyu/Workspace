// 保存先: Assets/Scripts/Minion/Attack.cs
// アクション戦闘の攻撃実体（プレイヤー・兵士共通の「振る」動作）。
//   StartAttack()で開始し、3フェーズ（前隙→判定→後隙）を自分のUpdateで自走する。
//   今の向きに振る（引数なし・自動で吸い付かない）。判定フェーズ中だけHitboxを有効化する。
//   フェーズ管理は初期=時間タイマー（方法A）。将来アニメーション駆動（方法B）へ差し替え可。
//   実体とStateの独立性：進行中の攻撃はState遷移では止まらない（ひるみ/回避後隙のForceCancelを除く）。
using UnityEngine;

public class Attack : MonoBehaviour
{
    private enum Phase { None, Windup, Active, Recovery }

    [SerializeField] private Hitbox hitbox; // 子のHitbox（Inspectorでアサイン推奨。未設定ならInChildrenで取得）

    private float attackPower;     // 実威力 = AttackData.attackPower × moves[0].powerMultiplier
    private float staggerDuration; // 命中時に相手へ渡すひるみ時間
    private float reach;           // 攻撃間合い（AI判断用。実際の当たりはHitboxのCollider）
    private float windupTime;
    private float activeTime;
    private float recoveryTime;
    private GameObject swingEffect; // 振った瞬間（Active開始）に出す攻撃側エフェクト
    private GameObject hitEffect;   // 命中時にHitboxが接触点へ出すエフェクト

    private Phase phase = Phase.None;
    private float phaseTimer;
    private IBattleInfo target; // 立ち回り用の参照（攻撃自体は今の向きに振るので命中には使わない）
    private Dodge dodge;        // 回避中は攻撃不可の判定用（兄弟コンポーネント・無い兵士はnull）

    public float AttackRange => reach;
    public bool IsAttacking => phase != Phase.None;
    public bool CanCancel => phase == Phase.Recovery; // 後隙のみキャンセル可

    public void Initialize(AttackData data)
    {
        attackPower = data.attackPower;

        if (hitbox == null) hitbox = GetComponentInChildren<Hitbox>(true);
        if (hitbox != null) hitbox.Setup(GetComponent<IBattleInfo>()); // 兵士=MinionCore / プレイヤー=PlayerCombatCore
        dodge = GetComponent<Dodge>();

        if (data.moves == null || data.moves.Count == 0)
        {
            Debug.LogError($"[Attack] AttackData.moves が空です: {name}");
            reach = 1f; windupTime = 0.2f; activeTime = 0.1f; recoveryTime = 0.5f; staggerDuration = 0f;
            return;
        }

        AttackMove move = data.moves[0];
        attackPower *= move.powerMultiplier;
        reach = move.reach;
        windupTime = move.windupTime;
        activeTime = move.activeTime;
        recoveryTime = move.recoveryTime;
        staggerDuration = move.staggerDuration;
        swingEffect = move.swingEffect;
        hitEffect = move.hitEffect;
    }

    // 立ち回り用に対象を渡す（向き直りに使う）。命中判定自体はHitboxの物理で行う。
    public void SetTarget(IBattleInfo target)
    {
        this.target = target;
    }

    // 攻撃を開始する。今の向きに振る（自動で吸い付かない）。攻撃中は受け付けない。
    public void StartAttack()
    {
        if (IsAttacking) return;
        if (dodge != null && dodge.IsDodging) return; // 回避中は攻撃不可
        phase = Phase.Windup;
        phaseTimer = 0f;
    }

    // 強制中断（ひるみ・回避の後隙キャンセルで使う。塊3-Bで本格利用）。
    public void ForceCancel()
    {
        if (hitbox != null) hitbox.Deactivate();
        phase = Phase.None;
        phaseTimer = 0f;
    }

    private void Update()
    {
        if (phase == Phase.None) return;

        phaseTimer += Time.deltaTime;

        switch (phase)
        {
            case Phase.Windup:
                if (phaseTimer >= windupTime)
                {
                    phase = Phase.Active;
                    phaseTimer = 0f;
                    // 判定フェーズ開始：今回の数値を構え、多段リストclear、Collider有効化。hitEffectもHitboxへ渡す。
                    if (hitbox != null) hitbox.Activate(attackPower, staggerDuration, hitEffect);
                    // 攻撃側エフェクト（振った瞬間）。Hitboxの位置・向きに出す（無ければ本体）。当たり外れに関係なく出す。
                    if (swingEffect != null)
                    {
                        Transform at = hitbox != null ? hitbox.transform : transform;
                        Instantiate(swingEffect, at.position, at.rotation);
                    }
                }
                break;

            case Phase.Active:
                if (phaseTimer >= activeTime)
                {
                    phase = Phase.Recovery;
                    phaseTimer = 0f;
                    if (hitbox != null) hitbox.Deactivate(); // 判定フェーズ終了：Collider無効化
                }
                break;

            case Phase.Recovery:
                if (phaseTimer >= recoveryTime)
                {
                    phase = Phase.None;
                    phaseTimer = 0f;
                }
                break;
        }
    }
}
