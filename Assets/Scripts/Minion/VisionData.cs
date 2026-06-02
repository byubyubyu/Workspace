// 保存先: Assets/Scripts/Minion/VisionData.cs
// 兵士の視野に関わるデータ。MinionDataが束ねる分割SOの1つ。
using UnityEngine;

[CreateAssetMenu(fileName = "VisionData", menuName = "Project/Minion/VisionData")]
public class VisionData : ScriptableObject
{
    public float visionRange;
}
