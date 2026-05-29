using UnityEngine;

[RequireComponent(typeof(Attack))]
[RequireComponent(typeof(Vision))]
public class CombatState : MonoBehaviour, IState
{
    private Attack attack;
    private Vision vision;
    private StateMachine stateMachine;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        attack = GetComponent<Attack>();
        vision = GetComponent<Vision>();

        vision.OnEnemyDetected += (target) =>
        {
            attack.SetTarget(target);
            stateMachine.ChangeState(this);
        };
        vision.OnEnemyLost += () =>
        {
            stateMachine.ChangeState(GetComponent<MovingState>());
        };
    }

    public void Enter() { }
    public void Update() { }

    public void Exit()
    {
        attack.SetTarget(null);
    }
}
