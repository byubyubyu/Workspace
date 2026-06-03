// 保存先: Assets/Scripts/Minion/StaminaData.cs
// スタミナのデータ。MinionDataが束ねる分割SOの1つ。回避(Dodge・C-2)等が消費し、時間で回復する。
//   recoveryDelay: 使用後この秒数は回復しない（連続使用の抑制＝案2）。
using UnityEngine;

[CreateAssetMenu(fileName = "StaminaData", menuName = "Project/Minion/StaminaData")]
public class StaminaData : ScriptableObject
{
    public float staminaMax;       // スタミナ上限
    public float staminaRecovery;  // 毎秒の回復量
    public float recoveryDelay;    // 使用後、回復が始まるまでの秒数
}
