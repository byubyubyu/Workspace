using UnityEngine;

[CreateAssetMenu(fileName = "BuildingStatData", menuName = "Project/Building/BuildingStatData")]
public class BuildingStatData : ScriptableObject
{
    public float hp;
    public float needBuildPoint;
    public int maxCountBase;
}
