// 保存先: Assets/Scripts/Common/TargetCandidate.cs （新規ファイル）
public class TargetCandidate
{
    public IBattleInfo Target { get; }
    public TargetCategory Category { get; }

    public TargetCandidate(IBattleInfo target, TargetCategory category)
    {
        Target = target;
        Category = category;
    }
    // 将来: スコアリング型のために距離などのフィールドを追加可能
}
