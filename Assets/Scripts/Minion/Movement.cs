// 保存先: Assets/Scripts/Minion/Movement.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Movement : MonoBehaviour, IDasher
{
    [SerializeField] private float arriveRadius = 1.5f;

    private NavMeshAgent agent;
    private List<Waypoint> waypoints;
    private int currentWaypointIndex;
    private bool arrived;
    private MinionCore minionCore;
    private Attack attack;        // 攻撃中の移動ロック判定用（兄弟コンポーネント・無い兵士はnull）

    private bool chasing = false; // 追尾モード中か（戦闘中）

    private bool dashing = false; // 回避ダッシュ中か（DodgeがDash/EndDashで制御）
    private Vector3 dashDir;
    private float dashSpeed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<Attack>();
    }

    public void Initialize(MovementData data, MinionCore core)
    {
        minionCore = core;
        agent.speed = data.moveSpeed;
    }

    public void SetWaypoints(List<Waypoint> waypoints)
    {
        this.waypoints = waypoints;
        currentWaypointIndex = 0;
        arrived = false;
        if (!chasing) GoToCurrent();
    }

    private void GoToCurrent()
    {
        if (waypoints == null || currentWaypointIndex >= waypoints.Count) return;
        if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(waypoints[currentWaypointIndex].Position); }
    }

    // --- 追尾モード（CombatStateが使う） ---
    public void Chase(Vector3 targetPos)
    {
        chasing = true;
        if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(targetPos); }
    }

    // --- 占拠用：到着後に未完成Cityhallへ寄る（Occupierが使う） ---
    // Waypoint移動は終わっている前提（arrived=true）。指定座標へ向かい、視界に入れば
    // BuildingState（優先度1）が建設を始める。到着停止は NavMeshAgent 任せ。
    public void MoveTo(Vector3 targetPos)
    {
        if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(targetPos); }
    }

    public void StopHere()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
    }

    // --- 回避ダッシュ（Dodgeが使う） ---
    // 自前でagent.Moveするのでpathfindingは止める。dirは水平・正規化済み前提。
    public void Dash(Vector3 dir, float speed)
    {
        dashing = true;
        dashDir = dir;
        dashSpeed = speed;
        if (agent.isOnNavMesh) agent.isStopped = true;
    }

    // ダッシュ終了。通常移動を再開できる状態に戻す（次フレームChase/Waypointが再指示する）。
    public void EndDash()
    {
        dashing = false;
        if (agent.isOnNavMesh) agent.isStopped = false;
    }

    // 戦闘終了 → Waypoint移動を再開
    public void ResumeWaypoint()
    {
        chasing = false;
        if (!arrived) GoToCurrent();
        else StopHere();
    }

    private void Update()
    {
        // 攻撃中は移動しない（前隙→判定→後隙の全フェーズ。GDD「攻撃中は移動不可」）。
        // 実体レベルで堅くロックする（Strategyが新行動を起こさないことに頼らない）。
        if (attack != null && attack.IsAttacking)
        {
            if (agent.isOnNavMesh && !agent.isStopped) agent.isStopped = true;
            return;
        }
        if (dashing)                  // 回避ダッシュ中は自前でMove（Waypoint/Chase処理はしない）
        {
            if (agent.isOnNavMesh) agent.Move(dashDir * dashSpeed * Time.deltaTime);
            return;
        }
        if (chasing) return;          // 追尾中はCombatStateが指揮するのでWaypoint処理はしない
        if (arrived) return;
        if (waypoints == null || waypoints.Count == 0) return;
        if (!agent.isOnNavMesh) return;

        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = waypoints[currentWaypointIndex].Position; b.y = 0f;

        if (Vector3.Distance(a, b) <= arriveRadius)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                arrived = true;
                StopHere();
                minionCore.NotifyArrived();
            }
            else GoToCurrent();
        }
    }
}
