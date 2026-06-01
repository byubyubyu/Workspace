// 保存先: Assets/Scripts/Minion/MinionCore.cs
using System;
using UnityEngine;

public class MinionCore : MonoBehaviour, IBattleInfo, IHealth
{
    private float currentHp;
    private float maxHp;
    private StateMachine stateMachine;
    private IState[] states;
    public Team Team { get; private set; }
    public bool IsDead => currentHp <= 0;
    public Vector3 Position => transform.position; // IBattleInfo

    // IHealth（ゲージ表示用）
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    public event Action OnDestroyed;
    public event Action OnArrived;

    public void Initialize(IMinionData data, Team team)
    {
        currentHp = data.Stat.hp;
        maxHp = data.Stat.hp;
        Team = team;

        // B-2: データ由来の初期設定をまとめて流し込む
        GetComponent<Movement>()?.Initialize(data, this);
        GetComponent<Vision>()?.Initialize(data, team);
        GetComponent<Attack>()?.Initialize(data);

        states = GetComponents<IState>();
        stateMachine = new StateMachine(states);
        foreach (var state in states)
            state.Initialize(stateMachine);
    }

    public void TakeDamage(BattleInfo info)
    {
        currentHp -= info.attackPower;
    }

    public void Die()
    {
        OnDestroyed?.Invoke();
        Destroy(gameObject);
    }

    public void NotifyArrived()
    {
        OnArrived?.Invoke();
    }

    private void Update()
    {
        stateMachine?.Update();
    }
}
