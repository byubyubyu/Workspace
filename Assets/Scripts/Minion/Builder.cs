// 保存先: Assets/Scripts/Minion/Builder.cs
using UnityEngine;

public class Builder : MonoBehaviour
{
    private Construction construction;

    // 建設対象は視界(Vision)で毎フレーム決まるため、SetConstruction で差し替える。
    public void SetConstruction(Construction target)
    {
        construction = target;
    }

    public void Build(float amount)
    {
        if (construction == null) return;
        construction.AddBuildPoint(amount);
    }
}
