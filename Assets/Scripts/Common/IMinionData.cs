// 保存先: Assets/Scripts/Common/IMinionData.cs
// 兵士の種類データの窓口。分割SO（案C）に対応し、各コンポーネントは自分のSOだけ受け取る。
//   productionCost は特定コンポーネントに属さない兵士全体のメタ情報なので直接フィールド（MinionDataが持つ）。
using UnityEngine;

public interface IMinionData
{
    VitalityData Vitality { get; }
    MovementData Movement { get; }
    AttackData Attack { get; }
    VisionData Vision { get; }
    BuilderData Builder { get; }
    StaminaData Stamina { get; }
    float ProductionCost { get; }
    GameObject Prefab { get; }
}
