using UnityEngine;

[CreateAssetMenu(fileName = "CityhallData", menuName = "Project/Building/CityhallData")]
public class CityhallData : ScriptableObject, IBuildingData
{
    [SerializeField] private BuildingStatData stat;
    [SerializeField] private GameObject prefab;
    public BuildingStatData Stat => stat;
    public GameObject Prefab => prefab;
}
