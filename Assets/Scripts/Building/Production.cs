using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BuildingCore))]
[RequireComponent(typeof(Construction))]
public class Production : MonoBehaviour
{
    private MinionFactory minionFactory;
    private ProductionStatData productionData;
    public event Action<MinionCore> OnProduced;

    public void Initialize(BarrackData data, MinionFactory factory)
    {
        productionData = data.Production;
        minionFactory = factory;
    }

    public void Produce(MinionData minionData, Team team, List<Waypoint> waypoints)
    {
        if (!productionData.minionDatas.Contains(minionData)) return;

        MinionCore minion = minionFactory.Create(minionData, transform.position);
        minion.Initialize(minionData, team);
        minion.GetComponent<Movement>()?.SetWaypoints(waypoints);
        OnProduced?.Invoke(minion);
    }
}
