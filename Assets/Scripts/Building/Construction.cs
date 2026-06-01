// 保存先: Assets/Scripts/Building/Construction.cs
using System;
using UnityEngine;

public class Construction : MonoBehaviour
{
    private float currentBuildPoint;
    private float needBuildPoint;
    private BuildStrategy buildStrategy;
    private bool initialized = false;
    public bool IsCompleted { get; private set; }
    public bool IsManual => buildStrategy is ManualBuildStrategy; // 兵士が建設対象にするか（Manual建設のみ）
    public event Action OnCompleted;

    public void Initialize(IBuildingData data, BuildStrategy strategy)
    {
        needBuildPoint = data.Stat.needBuildPoint;
        buildStrategy = strategy;
        initialized = true;
    }

    // 初期配置の建物用: 生成直後に完成状態にする。
    public void CompleteImmediately()
    {
        currentBuildPoint = needBuildPoint;
        if (!IsCompleted)
        {
            IsCompleted = true;
            Debug.Log($"[Occupy] Construction completed (IMMEDIATE) {name}"); // DEBUG
            OnCompleted?.Invoke();
        }
    }

    public void AddBuildPoint(float amount)
    {
        if (IsCompleted) return;
        currentBuildPoint += amount;
        if (currentBuildPoint >= needBuildPoint)
        {
            IsCompleted = true;
            Debug.Log($"[Occupy] Construction completed (BUILT) {name} {currentBuildPoint:F1}/{needBuildPoint:F1}"); // DEBUG
            OnCompleted?.Invoke();
        }
    }

    private void Update()
    {
        if (!initialized) return;
        buildStrategy.UpdateBuildPoint(this, Time.deltaTime);
    }
}