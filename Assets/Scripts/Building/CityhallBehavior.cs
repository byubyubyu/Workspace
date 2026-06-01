// 保存先: Assets/Scripts/Building/CityhallBehavior.cs
using System;
using UnityEngine;

[RequireComponent(typeof(CostPool))]
[RequireComponent(typeof(BuildingCore))]
[RequireComponent(typeof(Construction))]
public class CityhallBehavior : MonoBehaviour
{
    private CostPool costPool;
    private Team team; // 完成時に OnTeamChanged で発火するため保持
    public CostPool CostPool => costPool;
    public event Action OnCityhallDestroyed;
    public event Action<Team> OnTeamChanged;

    // startCost: 開始時の現在コスト。max / recovery は CityhallData（共通）、startCost は土地ごと。
    public void Initialize(Team team, float costMax, float costRecovery, float startCost)
    {
        this.team = team;

        costPool = GetComponent<CostPool>();
        costPool.Initialize(costMax, costRecovery, startCost);

        var buildingCore = GetComponent<BuildingCore>();
        buildingCore.OnDestroyed += () =>
        {
            OnCityhallDestroyed?.Invoke();
            OnTeamChanged?.Invoke(Team.None); // 破壊→中立化を通知
        };

        // 完成したら自国化を通知する。
        //   初期配置 : CompleteImmediately() で即 OnCompleted → ここで即発火
        //   占拠     : 兵士が建設して満タンで OnCompleted → ここで発火
        // 発火経路を OnCompleted の1本に統一（Initialize での即 Invoke は廃止）。
        var construction = GetComponent<Construction>();
        construction.OnCompleted += () => OnTeamChanged?.Invoke(this.team);
    }
}