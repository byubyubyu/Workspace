// 保存先: Assets/Scripts/Building/Production.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BuildingCore))]
[RequireComponent(typeof(Construction))]
public class Production : MonoBehaviour
{
    private MinionFactory minionFactory;
    private ProductionStatData productionData;
    private Construction construction; // 建設完了判定に使う（RequireConstructionで必ず同居）
    private float cooldownTimer;
    public event Action<MinionCore> OnProduced;

    public void Initialize(BarrackData data, MinionFactory factory)
    {
        productionData = data.Production;
        minionFactory = factory;
        construction = GetComponent<Construction>();
        cooldownTimer = 0f;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // 受付可能か（クールダウン明け かつ 建設完了済み）。建設中のBarrackは生産しない。
    public bool CanProduce() => cooldownTimer <= 0f && construction != null && construction.IsCompleted;

    // 1匹だけ生産する（コスト判断は呼び出し側=BaseAIが行う）。
    public void ProduceOne(MinionData minionData, Team team, List<Waypoint> waypoints, Base destination)
    {
        if (construction == null || !construction.IsCompleted) return; // 建設中は生産しない（保険）
        if (!productionData.minionDatas.Contains(minionData)) return;

        MinionCore minion = minionFactory.Create(minionData, transform.position);
        minion.Initialize(minionData, team);
        minion.GetComponent<Movement>()?.SetWaypoints(waypoints);
        minion.GetComponent<Occupier>()?.SetDestination(destination);
        OnProduced?.Invoke(minion);
    }

    // まとめて生産し終えたあとにクールダウンを開始する（1匹以上作れたときだけ呼ぶ）。
    public void StartCooldown()
    {
        cooldownTimer = productionData.productionSpeed; // productionSpeed = 間隔(秒)
    }
}
