using UnityEngine;

public class StatCalculator : MonoBehaviour
{
    [SerializeField] private ScriptableObject stats;

    private IStats iStats;
    private StatModifier modifier;

    void Awake()
    {
        iStats = stats as IStats;
        if (iStats == null)
            Debug.LogError($"[StatCalculator] {gameObject.name} の Stats が" +
                $"IStatsを実装していません");

        modifier = GetComponent<StatModifier>();
    }

    public float GetStat(StatType type)
    {
        float baseValue = iStats != null ? iStats.GetStat(type) : 0f;
        float bonus = modifier != null ? modifier.GetTotal(type) : 0f;
        return baseValue + bonus;
    }
}