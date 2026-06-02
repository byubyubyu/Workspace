// 保存先: Assets/Scripts/Minion/AttackData.cs
// 兵士の攻撃に関わるデータ。MinionDataが束ねる分割SOの1つ。
//   旧 attackInterval は削除（攻撃周期は AttackMove のフェーズ時間合計で表す）。
//   旧 attackRange は削除（間合いは AttackMove.reach へ移動）。
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Project/Minion/AttackData")]
public class AttackData : ScriptableObject
{
    public float attackPower;            // 基礎攻撃力（実威力 = attackPower × AttackMove.powerMultiplier）
    public List<AttackMove> moves = new List<AttackMove>(); // 初期は1つ。オートバトルは moves[0] を使う
}
