// 保存先: Assets/Scripts/Minion/CombatAction.cs
// 戦闘の立ち回りの「意図」。ICombatStrategyが返し、CombatStateが実際の操作に変換する。
//   Strategyは意図を返すだけ。Movementを動かす・Attackを振るのはCombatStateの責務。
public enum CombatAction
{
    Approach, // 対象へ近づく
    Attack,   // その場で攻撃を仕掛ける
    Wait,     // 何もしない（進行中の攻撃を邪魔しない等）
    // 将来：Dodge（回避）
}
