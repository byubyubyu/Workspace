using UnityEngine;

[CreateAssetMenu(fileName = "ManualBuildStrategy", menuName = "Project/Building/ManualBuildStrategy")]
public class ManualBuildStrategy : BuildStrategy
{
    public override void UpdateBuildPoint(Construction construction, float deltaTime)
    {
        // 兵士のBuilderからAddBuildPointが直接呼ばれるため何もしない
    }
}
