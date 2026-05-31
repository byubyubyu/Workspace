// 保存先: Assets/Scripts/Building/CostPool.cs
using UnityEngine;

public class CostPool : MonoBehaviour
{
    private float currentCost;
    private float maxCost;
    private float recoverySpeed;
    private bool initialized = false;

    // startCurrent: 開始時の現在コスト（Baseごとに変えられる）。max を超えない範囲にクランプ。
    public void Initialize(float max, float speed, float startCurrent)
    {
        maxCost = max;
        recoverySpeed = speed;
        currentCost = Mathf.Clamp(startCurrent, 0f, max);
        initialized = true;
    }

    public bool CanAfford(float amount) => currentCost >= amount;

    public bool TryConsume(float amount)
    {
        if (currentCost < amount) return false;
        currentCost -= amount;
        return true;
    }

    private void Update()
    {
        if (!initialized) return;
        currentCost = Mathf.Min(currentCost + recoverySpeed * Time.deltaTime, maxCost);
    }
}
