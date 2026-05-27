using UnityEngine;

namespace Workspace.World
{
    public class PathNode : MonoBehaviour
    {
        [SerializeField] private BaseNode nodeA;
        [SerializeField] private BaseNode nodeB;
        [SerializeField] private Transform[] waypoints;

        public BaseNode NodeA => nodeA;
        public BaseNode NodeB => nodeB;

        public Transform[] GetWaypoints(BaseNode from)
        {
            if (waypoints == null || waypoints.Length == 0) return null;

            if (from == nodeA)
                return waypoints;

            Transform[] reversed = new Transform[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
                reversed[i] = waypoints[waypoints.Length - 1 - i];

            return reversed;
        }
    }
}