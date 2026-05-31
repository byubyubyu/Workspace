// 保存先: Assets/Scripts/Building/BarrackData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "BarrackData", menuName = "Project/Building/BarrackData")]
public class BarrackData : ScriptableObject, IBuildingData
{
    [SerializeField] private BuildingStatData stat;
    [SerializeField] private ProductionStatData production;
    [SerializeField] private GameObject prefab;
    [SerializeField] private BuildStrategy buildStrategy; // AutoBuildStrategy をアサイン

    public BuildingStatData Stat => stat;
    public ProductionStatData Production => production;
    public GameObject Prefab => prefab;
    public BuildingType Type => BuildingType.Barrack;
    public BuildStrategy BuildStrategy => buildStrategy;
}
