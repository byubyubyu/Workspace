// 保存先: Assets/Scripts/Minion/PriorityTargetingStrategy.cs （新規ファイル）
using System.Collections.Generic;

// 現ターゲット維持・上位カテゴリ出現時のみ乗り換え（同カテゴリ内では乗り換えない）。
// カテゴリ順: 敵兵士(Minion) > 敵建物(Building) ※ enum の値が小さいほど高優先。
// 将来: スコアリング型(ユーティリティAI)に差し替え可能。差込口(ITargetingStrategy)は変えない。
public class PriorityTargetingStrategy : ITargetingStrategy
{
    public IBattleInfo SelectTarget(List<TargetCandidate> candidates, IBattleInfo currentTarget)
    {
        if (candidates == null || candidates.Count == 0) return null;

        // 候補の中での現ターゲットのカテゴリを調べる（生存＆視界内かの確認も兼ねる）
        TargetCategory? currentCategory = null;
        foreach (var c in candidates)
        {
            if (c.Target == currentTarget) { currentCategory = c.Category; break; }
        }

        // 候補中の最高優先カテゴリ（enum値が小さいほど高優先）
        TargetCategory bestCategory = candidates[0].Category;
        IBattleInfo bestTarget = candidates[0].Target;
        foreach (var c in candidates)
        {
            if ((int)c.Category < (int)bestCategory)
            {
                bestCategory = c.Category;
                bestTarget = c.Target;
            }
        }

        // 現ターゲットが有効(視界内に存在)で、より上位カテゴリが出ていなければ維持
        if (currentCategory.HasValue && (int)bestCategory >= (int)currentCategory.Value)
            return currentTarget;

        // それ以外は最高優先カテゴリの対象へ
        return bestTarget;
    }
}
