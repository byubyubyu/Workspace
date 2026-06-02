// 保存先: Assets/Scripts/Minion/VitalityData.cs
// 兵士の生存に関わるデータ。MinionDataが束ねる分割SOの1つ。
//   defense（防御力）は「守る側が持つ」。将来DamageCalculatorが受け手のdefenseを参照する（塊3）。
using UnityEngine;

[CreateAssetMenu(fileName = "VitalityData", menuName = "Project/Minion/VitalityData")]
public class VitalityData : ScriptableObject
{
    public float hp;
    public float defense; // 防御力（守る側が持つ。塊2では未使用、塊3のDamageCalculatorで使う）
}
