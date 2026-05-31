// 保存先: Assets/Scripts/Minion/CombatState.cs
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
    private IBattleInfo currentTarget;

    public int Priority => 2;

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

        // 選ばれた直後にも破壊済みチェック（偽nullガード）
        if (currentTarget != null && (currentTarget as Object) == null)
            currentTarget = null;

        attack.SetTarget(currentTarget);
        if (currentTarget == null) return;

        if (attack.IsInRange())
            movement.StopHere();
        else
            movement.Chase(currentTarget.Position);
    }

    public void Exit()
    {
        currentTarget = null;
        attack.SetTarget(null);
        movement.ResumeWaypoint();
    }
}
