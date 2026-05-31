// 保存先: Assets/Scripts/Minion/BuildingState.cs
using UnityEngine;

[RequireComponent(typeof(Builder))]
[RequireComponent(typeof(Vision))]
public class BuildingState : MonoBehaviour, IState
{
    private Builder builder;
    private Vision vision;
    private StateMachine stateMachine;

    public int Priority => 1;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        builder = GetComponent<Builder>();
        vision = GetComponent<Vision>();
    }

    public bool CanEnter() => vision.HasBuildTarget();

    public void Enter() { }

    public void Update()
    {
        // 毎フレーム、視界内の自国建設対象に建設ポイントを渡す
        builder.SetConstruction(vision.GetBuildTarget());
        builder.Build(Time.deltaTime);
    }

    public void Exit() { }
}
