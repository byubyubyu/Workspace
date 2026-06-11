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
    private bool held;                // 足止め中か（プレイヤーに話しかけられている＝立ち話）
    private Transform holdFaceTarget; // 足止め中に向く相手（null可）
    private const float HoldTurnSpeed = 360f; // 相手の方を向く回頭速度（度/秒）

    public void Initialize(CitizenData data, Base homeBase)
    {
        this.homeBase = homeBase;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = data.MoveSpeed;
        interval = data.WanderInterval;
        PickNewDestination();
    }

    // 足止め（市民プロフィール・商人UIのOpen/Closeから呼ばれる）。
    //   立ち話の間は徘徊を止め、faceTargetが指定されていれば相手の方をゆっくり向く。
    //   解除時は次のUpdateで通常の徘徊判定に戻る（目的地は残っているのでそのまま歩き出す）。
    public void SetHold(bool value, Transform faceTarget = null)
    {
        held = value;
        holdFaceTarget = value ? faceTarget : null;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = value;
    }

    private void Update()
    {
        if (agent == null || homeBase == null) return;
        if (held) { FaceHoldTarget(); return; } // 立ち話中：徘徊しない（タイマーも進めない）
        timer += Time.deltaTime;
        // 目的地に着いた、または一定時間が経ったら次の目的地へ。
        if (timer >= interval || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f))
        {
            PickNewDestination();
            timer = 0f;
        }
    }

    // 足止め中、話し相手の方へY軸だけゆっくり回頭する（高低差は無視）。
    private void FaceHoldTarget()
    {
        if (holdFaceTarget == null) return;
        Vector3 dir = holdFaceTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        var look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, HoldTurnSpeed * Time.deltaTime);
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
