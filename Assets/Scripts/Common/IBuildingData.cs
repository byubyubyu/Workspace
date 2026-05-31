// 保存先: Assets/Scripts/Common/IBuildingData.cs
using UnityEngine;

public interface IBuildingData
{
    BuildingStatData Stat { get; }
    GameObject Prefab { get; }
    BuildingType Type { get; }          // 追加: 建物の種別（種別カウント・建設判断に使う）
    BuildStrategy BuildStrategy { get; } // 追加: 建設の進み方（Manual/Auto）
}
