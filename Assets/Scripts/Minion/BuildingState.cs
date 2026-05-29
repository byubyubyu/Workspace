using UnityEngine;

[RequireComponent(typeof(Builder))]
[RequireComponent(typeof(Vision))]
public class BuildingState : MonoBehaviour, IState
{
    private Builder builder;
    private Vision vision;
    private StateMachine stateMachine;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        builder = GetComponent<Builder>();
        vision = GetComponent<Vision>();

        vision.OnBuildTargetDetected += (target) =>
        {
            builder.Initialize(target);
            stateMachine.ChangeState(this);
        };
        vision.OnBuildTargetLost += () =>
        {
            stateMachine.ChangeState(GetComponent<MovingState>());
        };
    }

    public void Enter() { }

    public void Update()
    {
        builder.Build(Time.deltaTime);
    }

    public void Exit() { }
}
