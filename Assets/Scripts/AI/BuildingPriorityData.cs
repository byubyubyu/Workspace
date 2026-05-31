// 保存先: Assets/Scripts/AI/BuildingPriorityData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingPriorityData", menuName = "Project/AI/BuildingPriorityData")]
public class BuildingPriorityData : ScriptableObject
{
    // (a) 建てる建物データへの参照。
    // インターフェース(IBuildingData)は Inspector にそのまま出せないため、
    // ScriptableObject 型で受け、IBuildingData にキャストして返す。
    // ここに CityhallData / BarrackData をアサインする。
    [SerializeField] private ScriptableObject buildingData;
    [SerializeField] private int basePriority;

    public IBuildingData BuildingData => buildingData as IBuildingData;
    public int BasePriority => basePriority;
    // ※ 旧 BuildingType フィールドは削除。種別は BuildingData.Type から取得する。
}
