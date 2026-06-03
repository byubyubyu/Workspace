// 保存先: Assets/Scripts/Common/IDasher.cs
// ダッシュ移動の抽象。Dodge（回避の実体）が「誰が動かすか」を知らずにダッシュさせるための差込口。
//   兵士は Movement（NavMeshAgent）、プレイヤーは PlayerMovement（CharacterController）が実装する。
//   これにより同じ Dodge 実体をプレイヤー・兵士の両方で使える（共有実体）。
using UnityEngine;

public interface IDasher
{
    void Dash(Vector3 dir, float speed); // dir は水平・正規化済み前提
    void EndDash();
}
