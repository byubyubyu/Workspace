using System.Collections.Generic;
using UnityEngine;

public class StatModifier : MonoBehaviour
{
    private Dictionary<StatType, List<float>> modifiers
        = new Dictionary<StatType, List<float>>();

    public void AddModifier(StatType type, float value)
    {
        if (!modifiers.ContainsKey(type))
            modifiers[type] = new List<float>();
        modifiers[type].Add(value);
    }

    public void RemoveModifier(StatType type, float value)
    {
        if (modifiers.ContainsKey(type))
            modifiers[type].Remove(value);
    }

    public float GetTotal(StatType type)
    {
        if (!modifiers.ContainsKey(type)) return 0f;
        float total = 0f;
        foreach (float value in modifiers[type])
            total += value;
        return total;
    }
}