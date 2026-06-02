// 保存先: Assets/Scripts/Minion/DeadState.cs
using UnityEngine;

[RequireComponent(typeof(MinionCore))]
public class DeadState : MonoBehaviour, IState
{
    private MinionCore minionCore;
    private StateMachine stateMachine;

    public int Priority => 40;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        minionCore = GetComponent<MinionCore>();
    }

    public bool CanEnter() => minionCore.IsDead;

    public void Enter()
    {
        minionCore.Die(); // OnDestroyed 発火＋消滅
    }

    public void Update() { }
    public void Exit() { }
}
