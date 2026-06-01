// 保存先: Assets/Scripts/Minion/Vision.cs
using System.Collections.Generic;
using UnityEngine;

public class Vision : MonoBehaviour
{
    private float visionRange;
    private Team team;

    private List<TargetCandidate> attackCandidates = new List<TargetCandidate>();
    private Construction buildTarget;
    private Construction prevBuildTarget; // DEBUG: 無→有の変化検出用

    public void Initialize(IMinionData data, Team team)
    {
        visionRange = data.Stat.visionRange;
        this.team = team;
    }

    private void Update()
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
                    attackCandidates.Add(new TargetCandidate(minion, TargetCategory.Minion));
            }
            else if (hit.CompareTag("Building"))
            {
                var core = hit.GetComponent<BuildingCore>();
                if (core == null) continue; // 破壊済み(偽null)は弾く

                if (core.Team != team)
                {
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
        }

        // DEBUG: 建設対象を新たに捉えた瞬間だけログ（毎フレーム抑制）
        if (buildTarget != null && prevBuildTarget == null)
            Debug.Log($"[Occupy] Vision({name}) caught buildTarget {buildTarget.name} myPos={transform.position} targetPos={buildTarget.transform.position} dist={Vector3.Distance(transform.position, buildTarget.transform.position):F2} visionRange={visionRange}");
        prevBuildTarget = buildTarget;
    }

    public bool HasEnemy() => attackCandidates.Count > 0;
    public List<TargetCandidate> GetAttackCandidates() => attackCandidates;
    public bool HasBuildTarget() => buildTarget != null;
    public Construction GetBuildTarget() => buildTarget;
}