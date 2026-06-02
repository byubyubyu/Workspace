// 保存先: Assets/Scripts/Minion/DumbCombatStrategy.cs
// 頭の悪いAI：愚直に近づいて、間合いに入ったら振る。回避しない。
//   賢いAI（前隙を見て回避する等）は将来差し替え。賢さは非対称（プレイヤー関与の土地だけ賢く）。
public class DumbCombatStrategy : ICombatStrategy
{
    public CombatAction Decide(CombatContext context)
    {
        if (context.target == null) return CombatAction.Wait;

        // 行動できない（攻撃中など）→ 邪魔せず待つ。進行中の攻撃は自走で振り切る。
        if (!context.canAct) return CombatAction.Wait;

        // 間合い内なら振る、外なら近づく。
        if (context.inRange) return CombatAction.Attack;
        return CombatAction.Approach;
    }
}
