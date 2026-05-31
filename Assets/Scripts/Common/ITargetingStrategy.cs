// 保存先: Assets/Scripts/Common/ITargetingStrategy.cs （新規ファイル）
using System.Collections.Generic;

public interface ITargetingStrategy
{
    // 入力: 候補(対象+カテゴリ)のリスト + 現在のターゲット
    // 出力: 攻撃すべき1体（いなければ null）
    IBattleInfo SelectTarget(List<TargetCandidate> candidates, IBattleInfo currentTarget);
}
