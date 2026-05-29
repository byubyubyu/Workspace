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

    public void Initialize(Team team)
    {
        costPool = GetComponent<CostPool>();
        var buildingCore = GetComponent<BuildingCore>();
        buildingCore.OnDestroyed += () =>
        {
            OnCityhallDestroyed?.Invoke();
            OnTeamChanged?.Invoke(Team.None);
        };
        OnTeamChanged?.Invoke(team);
    }
}
