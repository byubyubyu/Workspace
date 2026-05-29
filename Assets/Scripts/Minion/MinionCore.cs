using System;
using UnityEngine;

public class MinionCore : MonoBehaviour, IBattleInfo
{
    private float currentHp;
    private StateMachine stateMachine;
    private IState[] states;
    public Team Team { get; private set; }

    public event Action OnDestroyed;
    public event Action OnArrived;

    public void Initialize(IMinionData data, Team team)
    {
        currentHp = data.Stat.hp;
        Team = team;
        stateMachine = new StateMachine();
        states = GetComponents<IState>();

        foreach (var state in states)
        {
            state.Initialize(stateMachine);
        }

        stateMachine.ChangeState(GetComponent<MovingState>());
    }

    public void TakeDamage(BattleInfo info)
    {
        currentHp -= info.attackPower;
        if (currentHp <= 0)
        {
            stateMachine.ChangeState(GetComponent<DeadState>());
            OnDestroyed?.Invoke();
        }
    }

    public void NotifyArrived()
    {
        OnArrived?.Invoke();
    }

    private void Update()
    {
        stateMachine.Update();
    }
}
