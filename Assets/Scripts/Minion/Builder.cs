// 保存先: Assets/Scripts/Minion/Builder.cs
using UnityEngine;

public class Builder : MonoBehaviour
{
    private Construction construction;
    private float buildSpeed = 1f; // 1秒あたりの建設ポイント（BuilderDataから受け取る）

    public void Initialize(BuilderData data)
    {
        buildSpeed = data.buildSpeed;
    }

    // 建設対象は視界(Vision)で毎フレーム決まるため、SetConstruction で差し替える。
    public void SetConstruction(Construction target)
    {
        construction = target;
    }

    // deltaTime（経過秒）を受け取り、buildSpeed を掛けて建設ポイントを渡す。
    // 速度はBuilder固有の能力としてここで適用する（呼び出し側は時間を渡すだけ）。
    public void Build(float deltaTime)
    {
        if (construction == null) return;
        construction.Add(buildSpeed * deltaTime);
    }
}
