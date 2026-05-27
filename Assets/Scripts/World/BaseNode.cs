using System.Collections.Generic;
using UnityEngine;

namespace Workspace.World
{
    public class BaseNode : MonoBehaviour
    {
        [SerializeField] private List<PathNode> adjacentPaths = new List<PathNode>();

        public IReadOnlyList<PathNode> AdjacentPaths => adjacentPaths;

        public void AddPath(PathNode path)
        {
            if (!adjacentPaths.Contains(path))
                adjacentPaths.Add(path);
        }

        public void RemovePath(PathNode path)
        {
            adjacentPaths.Remove(path);
        }
    }
}