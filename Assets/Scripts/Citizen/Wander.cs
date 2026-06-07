// 保存先: Assets/Scripts/Citizen/Wander.cs
// 市民の徘徊。NavMeshAgentで自分のBaseのグリッド範囲内をランダムに歩き回る。
//   到着 or 一定間隔で次のランダムな目的地を選ぶ。戦闘はしない。
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Wander : MonoBehaviour
{
    private NavMeshAgent agent;
    private Base homeBase;
    private float interval;
    private float timer;

    public void Initialize(CitizenData data, Base homeBase)
    {
        this.homeBase = homeBase;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = data.MoveSpeed;
        interval = data.WanderInterval;
        PickNewDestination();
    }

    private void Update()
    {
        if (agent == null || homeBase == null) return;
        timer += Time.deltaTime;
        // 目的地に着いた、または一定時間が経ったら次の目的地へ。
        if (timer >= interval || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f))
        {
            PickNewDestination();
            timer = 0f;
        }
    }

    // Baseのグリッド内のランダムなマス中心を、NavMesh上に補正して目的地にする。
    private void PickNewDestination()
    {
        var size = homeBase.GridSize;
        if (size.x <= 0 || size.y <= 0) return;
        var cell = new Vector2Int(Random.Range(0, size.x), Random.Range(0, size.y));
        Vector3 target = homeBase.GridToWorld(cell);
        if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }
}
