using System;
using UnityEngine;

public class Construction : MonoBehaviour
{
    private float currentBuildPoint;
    private float needBuildPoint;
    private BuildStrategy buildStrategy;
    public bool IsCompleted { get; private set; }
    public event Action OnCompleted;

    public void Initialize(IBuildingData data, BuildStrategy strategy)
    {
        needBuildPoint = data.Stat.needBuildPoint;
        buildStrategy = strategy;
    }

    public void AddBuildPoint(float amount)
    {
        if (IsCompleted) return;
        currentBuildPoint += amount;
        if (currentBuildPoint >= needBuildPoint)
        {
            IsCompleted = true;
            OnCompleted?.Invoke();
        }
    }

    private void Update()
    {
        buildStrategy.UpdateBuildPoint(this, Time.deltaTime);
    }
}
