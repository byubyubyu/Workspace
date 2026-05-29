using System;
using UnityEngine;

public class Vision : MonoBehaviour
{
    private float visionRange;
    private Team team;
    private IBattleInfo detectedEnemy;
    private Construction detectedBuildTarget;

    public event Action<IBattleInfo> OnEnemyDetected;
    public event Action OnEnemyLost;
    public event Action<Construction> OnBuildTargetDetected;
    public event Action OnBuildTargetLost;

    public void Initialize(IMinionData data, Team team)
    {
        visionRange = data.Stat.visionRange;
        this.team = team;
    }

    private void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange);

        IBattleInfo newEnemy = null;
        Construction newBuildTarget = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Minion"))
            {
                var minion = hit.GetComponent<MinionCore>();
                if (minion != null && minion.Team != team)
                    newEnemy = minion;
            }
            else if (hit.CompareTag("Building"))
            {
                var construction = hit.GetComponent<Construction>();
                if (construction != null && !construction.IsCompleted)
                    newBuildTarget = construction;
            }
        }

        if (newEnemy != detectedEnemy)
        {
            detectedEnemy = newEnemy;
            if (detectedEnemy != null) OnEnemyDetected?.Invoke(detectedEnemy);
            else OnEnemyLost?.Invoke();
        }

        if (newBuildTarget != detectedBuildTarget)
        {
            detectedBuildTarget = newBuildTarget;
            if (detectedBuildTarget != null) OnBuildTargetDetected?.Invoke(detectedBuildTarget);
            else OnBuildTargetLost?.Invoke();
        }
    }
}
