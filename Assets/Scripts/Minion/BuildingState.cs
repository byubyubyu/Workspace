// 保存先: Assets/Scripts/Minion/BuildingState.cs
using UnityEngine;

[RequireComponent(typeof(Builder))]
[RequireComponent(typeof(Vision))]
public class BuildingState : MonoBehaviour, IState
{
    private Builder builder;
    private Vision vision;
    private MinionCore minionCore;
    private StateMachine stateMachine;
    private Construction current; // 今建てている対象（完成検知のため保持）

    public int Priority => 10;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        builder = GetComponent<Builder>();
        vision = GetComponent<Vision>();
        minionCore = GetComponent<MinionCore>();
    }

    public bool CanEnter() => vision.HasBuildTarget();

    public void Enter() { }

    public void Tick()
    {
        var target = vision.GetBuildTarget();
        if (target != null)
        {
            current = target;
            builder.SetConstruction(target);
            builder.Build(Time.deltaTime);
        }

        // 建てていた対象が完成したら、役目を終えて消滅する。
        // 完成すると Vision は未完成のみ拾うため target は null になるが、
        // current に参照を保持しているので IsCompleted を直接見て判定する。
        if (current != null && current.IsCompleted)
            minionCore.Die();
    }

    public void Exit() { }
}