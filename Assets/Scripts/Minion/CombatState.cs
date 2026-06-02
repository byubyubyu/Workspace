// 保存先: Assets/Scripts/Minion/CombatState.cs
// 戦闘状態の「器」。判断はICombatStrategyに委譲し、Strategyが返すCombatActionを実際の操作に変換する。
//   ターゲット選定（誰を狙うか）はITargetingStrategyを流用（敵兵士>敵建物）。
//   立ち回り（近づく/振る/待つ）はICombatStrategyが決める。
//   攻撃の実体(Attack)は自走する。CombatStateはStartAttackで指令を出すだけで、進行中の攻撃を止めない。
using UnityEngine;

[RequireComponent(typeof(Attack))]
[RequireComponent(typeof(Vision))]
[RequireComponent(typeof(Movement))]
public class CombatState : MonoBehaviour, IState
{
    private Attack attack;
    private Vision vision;
    private Movement movement;
    private StateMachine stateMachine;
    private ITargetingStrategy targeting = new PriorityTargetingStrategy();
    private ICombatStrategy combat = new DumbCombatStrategy(); // 初期＝愚直AI（将来差し替え・非対称）
    private IBattleInfo currentTarget;

    public int Priority => 20;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        attack = GetComponent<Attack>();
        vision = GetComponent<Vision>();
        movement = GetComponent<Movement>();
    }

    public bool CanEnter() => vision.HasEnemy();

    public void Enter() { }

    public void Update()
    {
        // 前フレームのターゲットが破壊済みなら捨てる
        if (currentTarget != null && (currentTarget as Object) == null)
            currentTarget = null;

        currentTarget = targeting.SelectTarget(vision.GetAttackCandidates(), currentTarget);
        if (currentTarget != null && (currentTarget as Object) == null)
            currentTarget = null;

        attack.SetTarget(currentTarget);
        if (currentTarget == null) return;

        // 判断材料を組み立ててStrategyに渡す
        var ctx = new CombatContext
        {
            target = currentTarget,
            inRange = IsInRange(currentTarget),
            canAct = !attack.IsAttacking, // 攻撃中は新しい行動を起こさない（自走を邪魔しない）
        };

        CombatAction action = combat.Decide(ctx);

        switch (action)
        {
            case CombatAction.Approach:
                movement.Chase(currentTarget.Position);
                break;

            case CombatAction.Attack:
                movement.StopHere();
                FaceTarget(currentTarget); // 振る前に対象を向く（命中は今の向きで決まる）
                attack.StartAttack();
                break;

            case CombatAction.Wait:
                // 何もしない（進行中の攻撃を邪魔しない）。塊3-13で攻撃中の移動ロックを本格対応。
                break;
        }
    }

    public void Exit()
    {
        // 攻撃の実体には触らない（進行中の攻撃は自走で振り切る＝実体とStateの独立性の原則）。
        currentTarget = null;
        attack.SetTarget(null);
        // Movementの戻し（ResumeWaypoint）はMovingStateのEnterが担うのでここでは呼ばない。
    }

    private bool IsInRange(IBattleInfo t)
    {
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = t.Position; b.y = 0f;
        return Vector3.Distance(a, b) <= attack.AttackRange;
    }

    private void FaceTarget(IBattleInfo t)
    {
        Vector3 dir = t.Position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
