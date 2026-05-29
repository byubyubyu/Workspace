using System;
using UnityEngine;

public class BuildingCore : MonoBehaviour, IBattleInfo
{
    private float currentHp;
    public event Action OnDestroyed;

    public void Initialize(IBuildingData data)
    {
        currentHp = data.Stat.hp;
    }

    public void TakeDamage(BattleInfo info)
    {
        currentHp -= info.attackPower;
        if (currentHp <= 0)
        {
            OnDestroyed?.Invoke();
        }
    }
}
