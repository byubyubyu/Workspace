using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    [SerializeField] private List<Base> connectedBases;
    [SerializeField] private List<Waypoint> waypoints;
    // TODO: 将来：Pathの移動速度倍率による速度変化
    [SerializeField] private float speedMultiplier = 1f;

    public List<Base> ConnectedBases => connectedBases;
    public List<Waypoint> Waypoints => waypoints;
}
