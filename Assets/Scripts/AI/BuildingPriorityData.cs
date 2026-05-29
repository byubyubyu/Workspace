using UnityEngine;

[CreateAssetMenu(fileName = "BuildingPriorityData", menuName = "Project/AI/BuildingPriorityData")]
public class BuildingPriorityData : ScriptableObject
{
    [SerializeField] private BuildingType buildingType;
    [SerializeField] private int basePriority;
    public BuildingType BuildingType => buildingType;
    public int BasePriority => basePriority;
}
