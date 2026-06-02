// 保存先: Assets/Scripts/Minion/CombatContext.cs
// ICombatStrategyに渡す判断材料。CombatStateが組み立てて渡す。
//   将来項目（スタミナ・敵の攻撃フェーズ・距離など）はここに足していく。差込口は変えない。
public class CombatContext
{
    public IBattleInfo target; // 攻撃対象（ターゲット選定で選ばれた1体）
    public bool inRange;       // 攻撃の間合い(reach)内に対象がいるか
    public bool canAct;        // 今、新しい行動を起こせるか（攻撃中・ひるみ中でない）
}
