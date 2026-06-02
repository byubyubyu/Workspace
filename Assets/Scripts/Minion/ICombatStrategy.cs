// 保存先: Assets/Scripts/Minion/ICombatStrategy.cs
// 戦闘の立ち回りを判断する差込口。CombatContextを見てCombatActionを返す。
//   初期実装=DumbCombatStrategy（愚直）。賢いAIは将来差し替え（差込口は変えない・非対称）。
public interface ICombatStrategy
{
    CombatAction Decide(CombatContext context);
}
