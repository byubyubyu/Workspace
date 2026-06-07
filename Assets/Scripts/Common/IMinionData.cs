// 保存先: Assets/Scripts/Common/IMinionData.cs
// 兵士の種類データの窓口。分割SO（案C）に対応し、各コンポーネントは自分のSOだけ受け取る。
//   productionCost は特定コンポーネントに属さない兵士全体のメタ情報なので直接フィールド（MinionDataが持つ）。
using System.Collections.Generic;
using UnityEngine;

public interface IMinionData
{
    VitalityData Vitality { get; }
    MovementData Movement { get; }
    AttackData Attack { get; }
    VisionData Vision { get; }
    BuilderData Builder { get; }
    StaminaData Stamina { get; }
    DodgeData Dodge { get; }
    float ProductionCost { get; }
    GameObject Prefab { get; }
    List<ItemData> InitialItems { get; } // 生成時に瓶へ入れる初期アイテム（種類のみ・位置は開いた時に物理で決める）
}
