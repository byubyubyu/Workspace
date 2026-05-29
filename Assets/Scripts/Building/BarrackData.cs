using UnityEngine;

[CreateAssetMenu(fileName = "BarrackData", menuName = "Project/Building/BarrackData")]
public class BarrackData : ScriptableObject, IBuildingData
{
    [SerializeField] private BuildingStatData stat;
    [SerializeField] private ProductionStatData production;
    [SerializeField] private GameObject prefab;
    public BuildingStatData Stat => stat;
    public ProductionStatData Production => production;
    public GameObject Prefab => prefab;
}
