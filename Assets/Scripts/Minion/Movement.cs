using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private float moveSpeed;
    private List<Waypoint> waypoints;
    private int currentWaypointIndex;
    private MinionCore minionCore;

    public void Initialize(IMinionData data, MinionCore core)
    {
        moveSpeed = data.Stat.moveSpeed;
        minionCore = core;
    }

    public void SetWaypoints(List<Waypoint> waypoints)
    {
        this.waypoints = waypoints;
        currentWaypointIndex = 0;
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Waypoint target = waypoints[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.Position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.Position) < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                minionCore.NotifyArrived();
            }
        }
    }
}
