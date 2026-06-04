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

    // プレイヤー指示（M-3）：指示中だけ派遣先をdirectedTargetに固定。タイマー切れで自動解除、新指示で上書き。
    private Base directedTarget;
    private float directedTimer = 0f;
    // 兵種ごとのノルマ（行き先は1つ）。M-3c-2：複数兵種を同時指定
    private readonly List<(MinionData type, int remaining)> directedQuotas = new List<(MinionData, int)>();

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

    // プレイヤーからの派遣先指示。指定時間だけ有効。再呼び出しで上書き。
    public void SetDirectedTarget(Base target, List<(MinionData type, int count)> quotas, float duration)
    {
        directedQuotas.Clear();
        if (quotas != null)
            foreach (var q in quotas)
                if (q.type != null && q.count > 0) directedQuotas.Add((q.type, q.count));

        if (directedQuotas.Count == 0) { ClearDirectedTarget(); return; } // 有効なノルマなし
        directedTarget = target;
        directedTimer = duration;
    }
    public void ClearDirectedTarget() { directedTarget = null; directedQuotas.Clear(); directedTimer = 0f; }
    public Base DirectedTarget => directedTimer > 0f ? directedTarget : null; // 表示用（指示中のみ）

    // この土地のBarrackが生産できる兵種の重複なしリスト（M-3b：指示UIの兵種候補に使う）。
    public List<MinionData> GetProducibleMinions()
    {
        barracks.RemoveAll(b => b.prod == null); // 破壊済みBarrackを除外
        var result = new List<MinionData>();
        foreach (var entry in barracks)
        {
            var list = entry.data.Production.minionDatas;
            if (list == null) continue;
            foreach (var m in list)
                if (m != null && !result.Contains(m)) result.Add(m);
        }
        return result;
    }

    // 自分のCityhallの位置（無ければnull）。占拠兵が敵Base攻撃時の寄り先に使う。
    //   位置(値)だけ返す＝建物本体には触れさせない（Facade経由・疎結合）。
    public Vector3? GetCityhallPosition()
    {
        if (buildingManager == null) return null;
        var cityhall = buildingManager.GetCityhall();
        if (cityhall == null) return null;
        return cityhall.transform.position;
    }

    private void Update()
    {
        if (!isRunning) return;

        if (directedTimer > 0f) { directedTimer -= Time.deltaTime; if (directedTimer <= 0f) ClearDirectedTarget(); }

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
        costPool.Consume(buildData.Stat.buildCost);
        PlaceBuilding(buildData, emptyCell.Value, team, false, 0f);
    }

    // 各Barrackについて：クールダウン明けなら、count匹を「コストが続く範囲で」まとめて生産する。
    // 1匹でも作れたらクールダウン開始。1匹も作れなければ何もしない（待機に入らない）。
    private void DecideMinion()
    {
        // 破壊済みBarrackの掃除（HP0破壊／Cityhall全消去の両経路に対応）。
        //   UnityのObjectは破壊済みだと == null が true になる。これでDestroy後の参照を弾く。
        barracks.RemoveAll(b => b.prod == null);
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

            bool directed = directedTimer > 0f && directedQuotas.Count > 0;
            MinionData chosen;
            int count;            // 今サイクルでこのBarrackが作る上限
            int quotaIndex = -1;
            if (directed)
            {
                // このBarrackが作れて、まだ残っているノルマを1件選ぶ
                quotaIndex = FindProducibleQuota(minionDatas);
                if (quotaIndex < 0) continue; // 作れるノルマがない→このBarrackはスキップ
                chosen = directedQuotas[quotaIndex].type;
                count = Mathf.Min(directedQuotas[quotaIndex].remaining, maxMinionCount); // 変更1：一気量は最大量基準
            }
            else
            {
                chosen = minionDatas[Random.Range(0, minionDatas.Count)];
                count = Random.Range(minMinionCount, maxMinionCount + 1);
            }
            float cost = chosen.ProductionCost;

            int producedCount = 0;
            for (int i = 0; i < count; i++)
            {
                // Consumeはアトミック（払えれば消費しtrue、払えなければfalse）。払えなくなったら打ち切り。
                if (!costPool.Consume(cost)) break;
                entry.prod.ProduceOne(chosen, team, waypoints, destination);
                producedCount++;
                if (directed) { var q = directedQuotas[quotaIndex]; directedQuotas[quotaIndex] = (q.type, q.remaining - 1); }
            }

            // 1匹でも作れたときだけクールダウンに入る
            if (producedCount > 0)
                entry.prod.StartCooldown();
        }

        // 全ノルマを送り切ったら指示解除（ランダムに戻る）
        if (directedTimer > 0f && directedQuotas.Count > 0 && AllQuotasDone())
            ClearDirectedTarget();
    }

    // このBarrackが作れて、まだ残っているノルマのインデックス（無ければ-1）。
    private int FindProducibleQuota(List<MinionData> producible)
    {
        for (int i = 0; i < directedQuotas.Count; i++)
            if (directedQuotas[i].remaining > 0 && producible.Contains(directedQuotas[i].type)) return i;
        return -1;
    }

    private bool AllQuotasDone()
    {
        for (int i = 0; i < directedQuotas.Count; i++)
            if (directedQuotas[i].remaining > 0) return false;
        return true;
    }

    private Base SelectDispatchTarget()
    {
        // 指示中かつ有効（隣接・中立or敵）なら、プレイヤー指定の行き先を優先。
        if (directedTarget != null && directedTimer > 0f && IsValidDispatch(directedTarget))
            return directedTarget;

        List<Base> candidates = new List<Base>();
        foreach (var kv in neighborTeams)
            if (kv.Value == Team.None || kv.Value != team) candidates.Add(kv.Key);
        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    // 指示先が派遣可能か：隣接していて、中立または敵であること。
    private bool IsValidDispatch(Base b)
    {
        if (b == null) return false;
        if (!neighborTeams.TryGetValue(b, out var t)) return false;
        return t == Team.None || t != team;
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

    // 占拠要求に応じて、建設しに行くべき未完成Cityhallの core を返す（呼び出し側=Occupierが位置を使う）。
    //   ・Cityhallが無ければ生成して返す（最初の占拠兵）
    //   ・既に未完成Cityhallがあればそれを返す（後続の占拠兵も同じCityhallに寄って一緒に建てる）
    //   ・既に完成済みCityhallがある／DI不足／空きセル無し のときは null（寄る必要なし or 生成不可）
    public BuildingCore RequestOccupation(Team requesterTeam)
    {
        if (buildingManager == null || buildingFactory == null || cityhallData == null) return null;

        var existing = buildingManager.GetCityhall();
        if (existing != null)
        {
            // 既にCityhallがある場合：
            //   ・要求した国(requesterTeam)の未完成Cityhallなら、それを建設対象として返す
            //     （後続の占拠兵も同じCityhallに寄って一緒に建てる）
            //   ・完成済み／別の国のCityhall なら null（寄らない。別Teamのものは戦闘側が処理）
            var construction = existing.GetComponent<Construction>();
            var core = existing.GetComponent<BuildingCore>();
            if (construction != null && !construction.IsCompleted && core != null && core.Team == requesterTeam)
                return core;
            return null;
        }

        Vector2Int? cell = buildingManager.GetEmptyCell();
        if (cell == null) return null;
        return PlaceBuilding(cityhallData, cell.Value, requesterTeam, false, 0f);
    }
}