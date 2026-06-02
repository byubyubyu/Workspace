// 保存先: Assets/Scripts/Minion/AttackMove.cs
// 1つの攻撃技を表すSO。タイミング・数値のみを持ち、モデル/アニメは別層なので別モデルで使い回せる。
//   塊2（道A-1・オートバトル）で使うのは powerMultiplier と reach、
//   および周期 = windupTime + activeTime + recoveryTime の合計。
//   windup/active/recovery のフェーズ区別と staggerDuration は塊3（アクション化）で本格的に使う。
using UnityEngine;

[CreateAssetMenu(fileName = "AttackMove", menuName = "Project/Minion/AttackMove")]
public class AttackMove : ScriptableObject
{
    public float powerMultiplier = 1f; // 実威力 = AttackData.attackPower × powerMultiplier
    public float windupTime;           // 前隙
    public float activeTime;           // 判定発生
    public float recoveryTime;         // 後隙
    public float staggerDuration;      // ひるみ時間（塊2では未使用、塊3で攻撃側がBattleInfoで渡す）
    public float reach;                // AI判断用の間合い目安（実際の当たりはHitboxのColliderサイズ）

    // 1回の攻撃サイクルの長さ（オートバトルの攻撃周期に使う）。
    public float TotalTime => windupTime + activeTime + recoveryTime;
}
