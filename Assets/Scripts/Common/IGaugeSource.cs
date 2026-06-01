// 保存先: Assets/Scripts/Common/IGaugeSource.cs
// ゲージ表示に必要な情報を提供する。StatBar はこの抽象だけに依存する（具体型を知らない）。
public interface IGaugeSource
{
    float Current { get; }   // 現在値
    float Max { get; }       // 最大値
    GaugeType Type { get; }  // 種別（StatBarが種別指定で選ぶ）
    Team Team { get; }       // 所属（外枠の国色に使う）
}
