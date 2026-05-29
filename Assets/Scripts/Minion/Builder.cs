using UnityEngine;

public class Builder : MonoBehaviour
{
    private Construction construction;

    public void Initialize(Construction target)
    {
        construction = target;
    }

    public void Build(float amount)
    {
        construction.AddBuildPoint(amount);
    }
}
