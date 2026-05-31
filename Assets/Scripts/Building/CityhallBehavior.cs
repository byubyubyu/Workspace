// 保存先: Assets/Scripts/Building/CityhallBehavior.cs
using System;
using UnityEngine;

[RequireComponent(typeof(CostPool))]
[RequireComponent(typeof(BuildingCore))]
[RequireComponent(typeof(Construction))]
public class CityhallBehavior : MonoBehaviour
{
    private CostPool costPool;
    public CostPool CostPool => costPool;
    public event Action OnCityhallDestroyed;
    public event Action<Team> OnTeamChanged;

    // startCost: 開始時の現在コスト。max / recovery は CityhallData（共通）、startCost は土地ごと。
    public void Initialize(Team team, float costMax, float costRecovery, float startCost)
    {
        costPool = GetComponent<CostPool>();
        costPool.Initialize(costMax, costRecovery, startCost);

        var buildingCore = GetComponent<BuildingCore>();
        buildingCore.OnDestroyed += () =>
        {
            OnCityhallDestroyed?.Invoke();
            OnTeamChanged?.Invoke(Team.None);
        };
        OnTeamChanged?.Invoke(team);
    }
}
