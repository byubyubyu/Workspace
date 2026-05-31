// 保存先: Assets/Scripts/Common/IBattleInfo.cs
using UnityEngine;

public interface IBattleInfo
{
    void TakeDamage(BattleInfo info);
    Vector3 Position { get; }   // 追加: 攻撃対象の位置（追尾・射程判定に使う）
}
