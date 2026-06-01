// 保存先: Assets/Scripts/Minion/Movement.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Movement : MonoBehaviour
{
    [SerializeField] private float arriveRadius = 1.5f;

    private NavMeshAgent agent;
    private List<Waypoint> waypoints;
    private int currentWaypointIndex;
    private bool arrived;
    private MinionCore minionCore;

    private bool chasing = false; // 追尾モード中か（戦闘中）

    private void Awake() { agent = GetComponent<NavMeshAgent>(); }

    public void Initialize(IMinionData data, MinionCore core)
    {
        minionCore = core;
        agent.speed = data.Stat.moveSpeed;
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

    // 戦闘終了 → Waypoint移動を再開
    public void ResumeWaypoint()
    {
        chasing = false;
        if (!arrived) GoToCurrent();
        else StopHere();
    }

    private void Update()
    {
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
