// 保存先: Assets/Scripts/Minion/MovingState.cs
using UnityEngine;

[RequireComponent(typeof(Movement))]
public class MovingState : MonoBehaviour, IState
{
    private Movement movement;
    private StateMachine stateMachine;

    public int Priority => 0;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        movement = GetComponent<Movement>();
    }

    public bool CanEnter() => true;

    public void Enter()
    {
        movement.ResumeWaypoint(); // 戦闘などから戻ってきたらWaypoint移動を再開
    }

    public void Tick() { }
    public void Exit() { }
}
