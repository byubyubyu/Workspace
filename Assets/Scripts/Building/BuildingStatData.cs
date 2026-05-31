// 保存先: Assets/Scripts/Building/BuildingStatData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingStatData", menuName = "Project/Building/BuildingStatData")]
public class BuildingStatData : ScriptableObject
{
    public float hp;
    public float needBuildPoint;
    public int maxCountBase;
    public float buildCost; // 追加: 着工時に CostPool から消費する建設費（(Y)）
}
