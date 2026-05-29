using UnityEngine;

[RequireComponent(typeof(Movement))]
public class MovingState : MonoBehaviour, IState
{
    private Movement movement;
    private StateMachine stateMachine;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        movement = GetComponent<Movement>();
    }

    public void Enter()
    {
        movement.enabled = true;
    }

    public void Update() { }

    public void Exit()
    {
        movement.enabled = false;
    }
}
