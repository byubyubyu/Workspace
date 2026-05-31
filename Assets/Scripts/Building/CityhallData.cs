// 保存先: Assets/Scripts/Building/CityhallData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "CityhallData", menuName = "Project/Building/CityhallData")]
public class CityhallData : ScriptableObject, IBuildingData
{
    [SerializeField] private BuildingStatData stat;
    [SerializeField] private GameObject prefab;
    [SerializeField] private BuildStrategy buildStrategy; // ManualBuildStrategy をアサイン

    [Header("CostPool")]
    [SerializeField] private float costMax;       // 追加(A): コスト上限
    [SerializeField] private float costRecovery;  // 追加(A): 1秒あたりの回復量

    public BuildingStatData Stat => stat;
    public GameObject Prefab => prefab;
    public BuildingType Type => BuildingType.Cityhall;
    public BuildStrategy BuildStrategy => buildStrategy;
    public float CostMax => costMax;
    public float CostRecovery => costRecovery;
}
