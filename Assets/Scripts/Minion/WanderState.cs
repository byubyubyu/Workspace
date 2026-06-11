// 保存先: Assets/Scripts/Minion/WanderState.cs
// 徘徊状態（優先度0・常時入れる＝MovingStateの代替）。Waypoint行軍ではなく、
//   NavMesh上のランダムな地点を選んで歩き回る（世界を放浪するモンスター用）。
//   敵が見えればCombatState(20)が勝ち、戦闘明けはEnterでまた徘徊に戻る。
//   MovingStateと同じ「器」方式：移動の実体はMovementに任せ、ここは目的地を選ぶだけ。
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Movement))]
public class WanderState : MonoBehaviour, IState
{
    [SerializeField] private float wanderRadius = 40f;     // 次の目的地を選ぶ半径（現在地基準）
    [SerializeField] private float interval = 8f;          // 到着しなくてもこの秒数で選び直す
    [SerializeField] private float arriveRadius = 1.5f;    // 到着とみなす距離
    [SerializeField] private float sampleMaxDistance = 6f; // 抽選点をNavMeshへ吸着させる許容距離

    private Movement movement;
    private StateMachine stateMachine;
    private Vector3 destination;
    private bool hasDestination;
    private float timer;

    public int Priority => 0;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        movement = GetComponent<Movement>();
    }

    public bool CanEnter() => true;

    public void Enter()
    {
        PickNewDestination();
    }

    public void Tick()
    {
        timer += Time.deltaTime;
        bool arrived = false;
        if (hasDestination)
        {
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = destination; b.y = 0f;
            arrived = Vector3.Distance(a, b) <= arriveRadius;
        }
        if (timer >= interval || arrived || !hasDestination)
            PickNewDestination();
    }

    public void Exit() { }

    // 現在地の周囲からランダムな地点を選び、NavMesh上に吸着させて目的地にする。
    //   吸着に失敗（崖の外など）したら hasDestination=false のまま次のTickで再抽選。
    private void PickNewDestination()
    {
        timer = 0f;
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = transform.position + new Vector3(r.x, 0f, r.y);
        if (NavMesh.SamplePosition(candidate, out var hit, sampleMaxDistance, NavMesh.AllAreas))
        {
            destination = hit.position;
            hasDestination = true;
            movement.MoveTo(destination);
        }
        else
        {
            hasDestination = false;
        }
    }
}
