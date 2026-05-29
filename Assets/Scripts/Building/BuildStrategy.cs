using UnityEngine;

public abstract class BuildStrategy : ScriptableObject
{
    public abstract void UpdateBuildPoint(Construction construction, float deltaTime);
}
