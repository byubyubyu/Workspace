// 保存先: Assets/Scripts/Minion/MinionStatData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "MinionStatData", menuName = "Project/Minion/MinionStatData")]
public class MinionStatData : ScriptableObject
{
    public float hp;
    public float moveSpeed;
    public float attackPower;
    public float attackInterval;
    public float attackRange;   // 追加: この距離内に入ったら攻撃する（visionRange より小さく）
    public float visionRange;
    public float productionCost;
}
