using System;
using UnityEngine;

namespace Workspace.Patterns
{
    [CreateAssetMenu(fileName = "MinionPoolConfig", menuName = "Pool/MinionPoolConfig")]
    public class MinionPoolConfig : ScriptableObject
    {
        [Serializable]
        public class PoolEntry
        {
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 100;
        }

        [SerializeField] private PoolEntry[] entries;
        public PoolEntry[] Entries => entries;
    }
}