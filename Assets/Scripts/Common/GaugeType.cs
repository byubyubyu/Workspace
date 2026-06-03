// 保存先: Assets/Scripts/Common/GaugeType.cs
// ゲージの種別。StatBar はこの種別を指定し、同じ種別の IGaugeSource を表示する。
public enum GaugeType
{
    Hp,
    Build,    // 建設進捗
    Stamina,  // スタミナ
    // 将来: Mana など
}
