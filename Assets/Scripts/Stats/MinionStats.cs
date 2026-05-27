using UnityEngine;
using Workspace.Enums;
using Workspace.Interfaces;

namespace Workspace.Stats
{
    [CreateAssetMenu(fileName = "MinionStats", menuName = "Stats/MinionStats")]
    public class MinionStats : ScriptableObject, IStats
    {
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float speed = 3f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float repairAmount = 0f;
        [SerializeField] private float buildPower = 0f;

        public float GetStat(StatType type)
        {
            switch (type)
            {
                case StatType.HP: return maxHP;
                case StatType.Speed: return speed;
                case StatType.AttackDamage: return attackDamage;
                case StatType.AttackRange: return attackRange;
                case StatType.DetectionRange: return detectionRange;
                case StatType.AttackInterval: return attackInterval;
                case StatType.RepairAmount: return repairAmount;
                case StatType.BuildPower: return buildPower;
                default: return 0f;
            }
        }
    }
}