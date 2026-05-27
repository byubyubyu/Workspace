using UnityEngine;

[CreateAssetMenu(fileName = "CityHallStats", menuName = "Stats/CityHallStats")]
public class CityHallStats : ScriptableObject, IStats
{
    [SerializeField] private float maxHP = 500f;
    [SerializeField] private float buildRequired = 100f;

    public float BuildRequired => buildRequired;

    public float GetStat(StatType type)
    {
        switch (type)
        {
            case StatType.HP: return maxHP;
            default: return 0f;
        }
    }
}