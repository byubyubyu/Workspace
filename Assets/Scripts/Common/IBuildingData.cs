using UnityEngine;

public interface IBuildingData
{
    BuildingStatData Stat { get; }
    GameObject Prefab { get; }
}
