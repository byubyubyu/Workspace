// 保存先: Assets/Scripts/AI/BaseAI.cs
using System.Collections.Generic;
using UnityEngine;

public class BaseAI : MonoBehaviour
{
    [System.Serializable]
    public class InitialBuilding
    {
        public Vector2Int cell;
        public ScriptableObject data;
        public bool startCompleted = true;
        public float startCost = 0f;
    }

    [SerializeField] private List<BuildingPriorityData> buildingPriorities;
    [SerializeField] private CityhallData cityhallData;
    [SerializeField] private List<InitialBuilding> initialBuildings;
    [SerializeField] private Team team;
    [SerializeField] private float buildInterval = 3f;
    [SerializeField] private float minionInterval = 2f;
    [SerializeField] private int minMinionCount = 1;
    [SerializeField] private int maxMinionCount = 3;

    private BuildingManager buildingManager;
    private MinionManager minionManager;
    private BuildingFactory buildingFactory;
    private MinionFactory minionFactory;
    private Dictionary<Base, Team> neighborTeams = new Dictionary<Base, Team>();
    private List<(Production prod, BarrackData data)> barracks = new List<(Production, BarrackData)>();

    private bool isRunning = false;
    private float buildTimer = 0f;
    private float minionTimer = 0f;

    public Team Team => team;

    public void Initialize(BuildingManager buildingManager, MinionManager minionManager,
                           BuildingFactory buildingFactory, MinionFactory minionFactory)
    {
        this.buildingManager = buildingManager;
        this.minionManager = minionManager;
        this.buildingFactory = buildingFactory;
        this.minionFactory = minionFactory;

        // 旧: ここで GetComponent<CityhallBehavior>() による自Cityhall購読を行っていたが、
        //     Cityhall は別GameObjectのため null で空振りしていた。
        //     自Cityhallの購読は PlaceBuilding（生成した core から直接）に移動した。
    }

    public void InitializeNeighborTeams()
    {
        Base myBase = GetComponent<Base>();
        foreach (var path in myBase.Paths)
        {
            foreach (var neighborBase in path.ConnectedBases)
            {
                if (neighborBase == myBase) continue;
                var neighborAI = neighborBase.GetComponent<BaseAI>();
                if (neighborAI != null)
                    neighborTeams[neighborBase] = neighborAI.Team;
            }
        }
    }

    public void PlaceInitialBuildings()
    {
        if (initialBuildings == null) return;
        foreach (var entry in initialBuildings)
        {
            var data = entry.data as IBuildingData;
            if (data == null) continue;
            PlaceBuilding(data, entry.cell, team, entry.startCompleted, entry.startCost);
        }
    }

    public void UpdateNeighborTeam(Base neighborBase, Team team) { neighborTeams[neighborBase] = team; }
    public void StartDecision() { isRunning = true; }

    private void Update()
    {
        if (!isRunning) return;
        buildTimer += Time.deltaTime;
        if (buildTimer >= buildInterval) { buildTimer = 0f; DecideBuilding(); }
        minionTimer += Time.deltaTime;
        if (minionTimer >= minionInterval) { minionTimer = 0f; DecideMinion(); }
    }

    private BuildingCore PlaceBuilding(IBuildingData data, Vector2Int cell, Team buildingTeam, bool startCompleted, float startCost)
    {
        Base myBase = GetComponent<Base>();
        Vector3 position = myBase.GridToWorld(cell);
        BuildingCore core = buildingFactory.Create(data, position);
        core.Initialize(data, buildingTeam, startCost);
        buildingManager.AddBuilding(core, cell);

        // Cityhall を建てたとき：
        //   ・自分の team を OnTeamChanged で更新する（占拠完成で None→自国、破壊で None）
        //   ・隣接 Base にもこの Cityhall を購読させる（分散型・Base が張り合いを担う）
        // CompleteImmediately より前に張ることで、初期配置の即発火を取りこぼさない。
        if (data is CityhallData)
        {
            var cityhall = core.GetComponent<CityhallBehavior>();
            if (cityhall != null)
            {
                Debug.Log($"[Occupy] PlaceBuilding cityhall pos={position} team={buildingTeam} startCompleted={startCompleted}"); // DEBUG
                cityhall.OnTeamChanged += (newTeam) => team = newTeam;
                myBase.AnnounceCityhall(cityhall);
            }
        }

        if (startCompleted)
            core.GetComponent<Construction>()?.CompleteImmediately();

        if (data is BarrackData barrackData)
        {
            var production = core.GetComponent<Production>();
            if (production != null)
            {
                production.Initialize(barrackData, minionFactory);
                production.OnProduced += OnMinionProduced;
                barracks.Add((production, barrackData));
            }
        }
        return core;
    }

    private void DecideBuilding()
    {
        if (buildingManager == null || buildingFactory == null) return;
        Vector2Int? emptyCell = buildingManager.GetEmptyCell();
        if (emptyCell == null) return;

        var cityhall = buildingManager.GetCityhall();
        CostPool costPool = cityhall != null ? cityhall.CostPool : null;

        BuildingPriorityData best = null;
        foreach (var priority in buildingPriorities)
        {
            if (priority == null) continue;
            IBuildingData data = priority.BuildingData;
            if (data == null) continue;
            if (buildingManager.CountByType(data.Type) >= data.Stat.maxCountBase) continue;
            if (costPool == null || !costPool.CanAfford(data.Stat.buildCost)) continue;
            if (best == null || priority.BasePriority > best.BasePriority) best = priority;
        }
        if (best == null) return;

        IBuildingData buildData = best.BuildingData;
        costPool.TryConsume(buildData.Stat.buildCost);
        PlaceBuilding(buildData, emptyCell.Value, team, false, 0f);
    }

    // 各Barrackについて：クールダウン明けなら、count匹を「コストが続く範囲で」まとめて生産する。
    // 1匹でも作れたらクールダウン開始。1匹も作れなければ何もしない（待機に入らない）。
    private void DecideMinion()
    {
        if (barracks.Count == 0) return;
        var cityhall = buildingManager.GetCityhall();
        CostPool costPool = cityhall != null ? cityhall.CostPool : null;
        if (costPool == null) return;

        foreach (var entry in barracks)
        {
            if (!entry.prod.CanProduce()) continue;

            Base destination = SelectDispatchTarget();
            if (destination == null) continue;
            List<Waypoint> waypoints = ResolvePath(destination);

            var minionDatas = entry.data.Production.minionDatas;
            if (minionDatas == null || minionDatas.Count == 0) continue;

            MinionData chosen = minionDatas[Random.Range(0, minionDatas.Count)];
            int count = Random.Range(minMinionCount, maxMinionCount + 1); // 作ろうとする数
            float cost = chosen.Stat.productionCost;

            int producedCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (!costPool.CanAfford(cost)) break; // コストが尽きたら打ち切り
                costPool.TryConsume(cost);
                entry.prod.ProduceOne(chosen, team, waypoints, destination);
                producedCount++;
            }

            // 1匹でも作れたときだけクールダウンに入る
            if (producedCount > 0)
                entry.prod.StartCooldown();
        }
    }

    private Base SelectDispatchTarget()
    {
        List<Base> candidates = new List<Base>();
        foreach (var kv in neighborTeams)
            if (kv.Value == Team.None || kv.Value != team) candidates.Add(kv.Key);
        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    private List<Waypoint> ResolvePath(Base destination)
    {
        Base myBase = GetComponent<Base>();
        foreach (var path in myBase.Paths)
        {
            if (path.ConnectedBases.Contains(destination))
            {
                var wps = new List<Waypoint>(path.Waypoints);
                if (path.ConnectedBases.Count >= 2 && path.ConnectedBases[0] == destination)
                    wps.Reverse();
                return wps;
            }
        }
        return new List<Waypoint>();
    }

    private void OnMinionProduced(MinionCore minion) { minionManager.AddMinion(minion); }

    public void RequestOccupation(Team requesterTeam)
    {
        Debug.Log($"[Occupy] RequestOccupation on {name} by team={requesterTeam}"); // DEBUG
        if (buildingManager == null || buildingFactory == null || cityhallData == null)
        {
            Debug.Log($"[Occupy]  -> aborted (null check): bm={buildingManager != null} bf={buildingFactory != null} cityhallData={cityhallData != null}"); // DEBUG
            return;
        }
        if (buildingManager.GetCityhall() != null)
        {
            Debug.Log("[Occupy]  -> aborted: cityhall already exists"); // DEBUG
            return;
        }
        Vector2Int? cell = buildingManager.GetEmptyCell();
        if (cell == null)
        {
            Debug.Log("[Occupy]  -> aborted: no empty cell"); // DEBUG
            return;
        }
        Debug.Log($"[Occupy]  -> spawning uncompleted cityhall at cell={cell.Value} team={requesterTeam}"); // DEBUG
        PlaceBuilding(cityhallData, cell.Value, requesterTeam, false, 0f);
    }
}