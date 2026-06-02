// 保存先: Assets/Scripts/Minion/MovementData.cs
// 兵士の移動に関わるデータ。MinionDataが束ねる分割SOの1つ。
using UnityEngine;

[CreateAssetMenu(fileName = "MovementData", menuName = "Project/Minion/MovementData")]
public class MovementData : ScriptableObject
{
    public float moveSpeed;
}
