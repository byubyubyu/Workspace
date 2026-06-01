// 保存先: Assets/Scripts/Common/IHealth.cs
// HPを持つもの（兵士・建物）の共通インターフェース。HpGaugeSource がこれを読んで表示する。
public interface IHealth
{
    float CurrentHp { get; }
    float MaxHp { get; }
    Team Team { get; }
}
