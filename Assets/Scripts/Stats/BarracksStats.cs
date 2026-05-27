using UnityEngine;
using Workspace.Enums;
using Workspace.Interfaces;

namespace Workspace.Stats
{
    [CreateAssetMenu(fileName = "BarracksStats", menuName = "Stats/BarracksStats")]
    public class BarracksStats : ScriptableObject, IStats
    {
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float spawnInterval = 15f;
        [SerializeField] private float spawnDelay = 0.5f;
        [SerializeField] private int waveCount = 5;
        [SerializeField] private GameObject[] minionPrefabs;

        public float SpawnInterval => spawnInterval;
        public float SpawnDelay => spawnDelay;
        public int WaveCount => waveCount;
        public GameObject[] MinionPrefabs => minionPrefabs;

        public float GetStat(StatType type)
        {
            switch (type)
            {
                case StatType.HP: return maxHP;
                default: return 0f;
            }
        }
    }
}