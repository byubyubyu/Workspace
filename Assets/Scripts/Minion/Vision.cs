// 保存先: Assets/Scripts/Minion/Vision.cs
using System.Collections.Generic;
using UnityEngine;

public class Vision : MonoBehaviour
{
    private float visionRange;
    private bool targetBuildings;
    private Team team;

    private List<TargetCandidate> attackCandidates = new List<TargetCandidate>();
    private Construction buildTarget;

    public void Initialize(VisionData data, Team team)
    {
        visionRange = data.visionRange;
        targetBuildings = data.targetBuildings;
        this.team = team;
    }

    // MinionCoreが毎フレーム、状態選択(StateMachine)の直前に呼ぶ（プル型を明示）。
    //   自前Updateでの自動検出はやめ、駆動主体をMinionCoreに一本化＝更新順の非保証を排除する。
    //   これで生成直後の初回フレームから敵を検出済みにできる。
    public void Refresh()
    {
        attackCandidates.Clear();
        buildTarget = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange);
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            if (hit.CompareTag("Minion"))
            {
                var minion = hit.GetComponent<MinionCore>();
                // 破壊済み(偽null)・死亡済み(IsDead)は候補に入れない
                if (minion == null) continue;
                if (minion.IsDead) continue;
                if (minion.Team != team)
                {
                    // 視線が障害物（木）に遮られている敵は見えない＝候補にしない
                    if (IsHiddenByObstacle(minion.Position)) continue;
                    attackCandidates.Add(new TargetCandidate(minion, TargetCategory.Minion));
                }
            }
            else if (hit.CompareTag("Building"))
            {
                var core = hit.GetComponent<BuildingCore>();
                if (core == null) continue; // 破壊済み(偽null)は弾く

                if (core.Team != team)
                {
                    // 建物を攻撃対象にしない設定（モンスター等）なら候補に入れない
                    if (!targetBuildings) continue;
                    // 視線が障害物（木）に遮られている敵建物は見えない＝候補にしない
                    if (IsHiddenByObstacle(core.Position)) continue;
                    attackCandidates.Add(new TargetCandidate(core, TargetCategory.Building));
                }
                else
                {
                    var construction = hit.GetComponent<Construction>();
                    // 兵士が建設するのは Manual建設のみ（AutoBuild の Barrack 等は手伝わない）
                    if (construction != null && !construction.IsCompleted && construction.IsManual)
                        buildTarget = construction;
                }
            }
            else if (hit.CompareTag("Player"))
            {
                // プレイヤー。人間（PlayerCombatCore）・魔族（DemonCore）どちらもIBattleInfoとして検出する。
                // 敵Teamなら攻撃候補（優先度は敵兵士の次・敵建物より上）
                var player = hit.GetComponent<IBattleInfo>();
                if (player == null) continue;
                // 死亡中（リスポーン待ちの魔族等）は狙わない（IHealth実装者のみ判定。人間は被ダメnoopで対象外）
                if (player is IHealth ph && ph.Max > 0f && ph.Current <= 0f) continue;
                if (player.Team != team)
                {
                    // 視線が障害物（木）に遮られているプレイヤーは見えない＝候補にしない
                    if (IsHiddenByObstacle(player.Position)) continue;
                    attackCandidates.Add(new TargetCandidate(player, TargetCategory.Player));
                }
            }
        }
    }

    public bool HasEnemy() => attackCandidates.Count > 0;
    public List<TargetCandidate> GetAttackCandidates() => attackCandidates;
    public bool HasBuildTarget() => buildTarget != null;
    public Construction GetBuildTarget() => buildTarget;

    // 自分から対象への視線が障害物（木）に遮られているか。
    //   SightBlocker が無い（シーンに未配置・木なし）場合は遮られていない扱い＝従来どおり見える。
    private bool IsHiddenByObstacle(Vector3 targetPos)
    {
        return SightBlocker.Instance != null
            && SightBlocker.Instance.IsBlocked(transform.position, targetPos);
    }
}
