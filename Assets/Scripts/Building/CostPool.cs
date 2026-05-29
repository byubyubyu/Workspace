using UnityEngine;

public class CostPool : MonoBehaviour
{
    private float currentCost;
    private float maxCost;
    private float recoverySpeed;

    public void Initialize(float max, float speed)
    {
        maxCost = max;
        currentCost = max;
        recoverySpeed = speed;
    }

    public bool TryConsume(float amount)
    {
        if (currentCost < amount) return false;
        currentCost -= amount;
        return true;
    }

    private void Update()
    {
        currentCost = Mathf.Min(currentCost + recoverySpeed * Time.deltaTime, maxCost);
    }
}
