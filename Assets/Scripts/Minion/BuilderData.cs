// 保存先: Assets/Scripts/Minion/BuilderData.cs
// 兵士の建設に関わるデータ。MinionDataが束ねる分割SOの1つ。
//   buildSpeed：1秒あたりに渡す建設ポイント。Builderが保持し、Build時に内部で掛ける。
//   buildSpeed=1 なら従来の「deltaTimeをそのまま渡す」挙動と同じ（後方互換）。
using UnityEngine;

[CreateAssetMenu(fileName = "BuilderData", menuName = "Project/Minion/BuilderData")]
public class BuilderData : ScriptableObject
{
    public float buildSpeed = 1f;
}
