// 保存先: Assets/Scripts/Path/Path.cs
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    [SerializeField] private List<Base> connectedBases;
    [SerializeField] private List<Waypoint> waypoints;
    // TODO: 将来：Pathの移動速度倍率による速度変化
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private Color pathGizmoColor = new Color(1f, 0.8f, 0.2f, 0.9f); // Gizmoの経路色（Inspectorで変更可）
    [SerializeField] private float waypointGizmoRadius = 0.3f;                       // 経路点の球の大きさ

    public List<Base> ConnectedBases => connectedBases;
    public List<Waypoint> Waypoints => waypoints;

    // シーン配置を見やすくするための経路表示（エディタ専用・ゲーム動作に影響なし）。
    // Waypoint を順に線で結び、各 Waypoint に球を描く。
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = pathGizmoColor;

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Vector3 pos = waypoints[i].Position;

            // 経路点の球
            Gizmos.DrawSphere(pos, waypointGizmoRadius);

            // 次の Waypoint への線
            if (i + 1 < waypoints.Count && waypoints[i + 1] != null)
                Gizmos.DrawLine(pos, waypoints[i + 1].Position);
        }
    }
}