// 保存先: Assets/Scripts/Building/BuildingCore.cs
using System;
using UnityEngine;

public class BuildingCore : MonoBehaviour, IBattleInfo, IHealth
{
    private float currentHp;
    private float maxHp;
    public BuildingType Type { get; private set; }
    public Team Team { get; private set; }
    public Vector3 Position => transform.position; // IBattleInfo

    // IHealth（ゲージ表示用）
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    public event Action OnDestroyed;

    public void Initialize(IBuildingData data, Team team, float startCost)
    {
        currentHp = data.Stat.hp;
        maxHp = data.Stat.hp;
        Type = data.Type;
        Team = team;

        var construction = GetComponent<Construction>();
        if (construction != null)
            construction.Initialize(data, data.BuildStrategy);

        var cityhall = GetComponent<CityhallBehavior>();
        if (cityhall != null && data is CityhallData cityhallData)
            cityhall.Initialize(team, cityhallData.CostMax, cityhallData.CostRecovery, startCost);
    }

    public void SetTeam(Team team) { Team = team; }

    public void TakeDamage(BattleInfo info)
    {
        currentHp -= info.attackPower;
        if (currentHp <= 0)
            OnDestroyed?.Invoke();
    }
}
