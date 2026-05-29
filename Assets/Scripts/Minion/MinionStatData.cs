using UnityEngine;

[CreateAssetMenu(fileName = "MinionStatData", menuName = "Project/Minion/MinionStatData")]
public class MinionStatData : ScriptableObject
{
    public float hp;
    public float moveSpeed;
    public float attackPower;
    public float attackInterval;
    public float visionRange;
    public float productionCost;
}
