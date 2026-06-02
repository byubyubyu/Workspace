// 保存先: Assets/Scripts/Common/BattleInfo.cs
// 攻撃側が運ぶ戦闘情報。攻撃のたびに作られ、受け手のTakeDamageに渡される。
//   防御計算は受け手側がDamageCalculatorで行う（攻撃側は素のattackPowerを入れる）。
public class BattleInfo
{
    public float attackPower;      // 素の攻撃力（実威力 = AttackData.attackPower × AttackMove.powerMultiplier）
    public float staggerDuration;  // ひるみ時間（攻撃側が渡す。塊3-Bのひるみ実装で受け手が使う）

    // 将来：防御貫通・状態異常・クリティカル・回復量
}
