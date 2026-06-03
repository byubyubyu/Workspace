// 保存先: Assets/Scripts/Minion/DodgeData.cs
// 回避(Dodge)のデータ。MinionDataが束ねる分割SOの1つ。
using UnityEngine;

[CreateAssetMenu(fileName = "DodgeData", menuName = "Project/Minion/DodgeData")]
public class DodgeData : ScriptableObject
{
    public float staminaCost;     // 1回の回避で消費するスタミナ
    public float dashSpeed;       // ダッシュ速度（m/s）
    public float dodgeDuration;   // 回避全体の時間（秒）。この間ダッシュ移動し、攻撃不可
    public float iFrameDuration;  // 無敵時間（秒）。回避開始からこの間Hurtboxを無効化（dodgeDuration以下）
}
