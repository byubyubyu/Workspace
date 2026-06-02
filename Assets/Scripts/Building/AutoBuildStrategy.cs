using UnityEngine;

[CreateAssetMenu(fileName = "AutoBuildStrategy", menuName = "Project/Building/AutoBuildStrategy")]
public class AutoBuildStrategy : BuildStrategy
{
    [SerializeField] private float buildPointPerSecond = 1f;

    public override void UpdateBuildPoint(Construction construction, float deltaTime)
    {
        construction.Add(buildPointPerSecond * deltaTime);
    }
}
