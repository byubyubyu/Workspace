using UnityEngine;

[RequireComponent(typeof(MinionCore))]
public class DeadState : MonoBehaviour, IState
{
    private MinionCore minionCore;
    private StateMachine stateMachine;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        minionCore = GetComponent<MinionCore>();
    }

    public void Enter()
    {
        GameObject.Destroy(minionCore.gameObject);
    }

    public void Update() { }
    public void Exit() { }
}
