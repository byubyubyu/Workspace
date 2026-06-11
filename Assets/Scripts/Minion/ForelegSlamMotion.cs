// 保存先: Assets/Scripts/Minion/ForelegSlamMotion.cs
// 技モーション「前脚振り下ろし」。Attackのフェーズに連動して前脚（ピボット=肩）を回転させる。
//   ・前隙＝振りかぶり（脚を上へ）→ 判定＝前へ振り下ろし → 後隙＝元の姿勢へ戻す
//   ・対象の技は AttackMove.motionId で紐づける（自分の担当IDと一致した時だけ動く）。
//     技を増やす時は「AttackMove SO＋Motionコンポーネント」を1セット足すだけ（呼び出し側に分岐を足さない）。
//   ・Hitboxを脚の先端に置けば、当たり判定が振り下ろしの軌道と同期する。
using UnityEngine;

public class ForelegSlamMotion : MonoBehaviour
{
    [SerializeField] private string motionId = "ForelegSlam"; // 担当する技のmotionId
    [SerializeField] private Transform foreleg;               // 動かす前脚のピボット（肩に置く）
    [SerializeField] private float windupAngle = -80f;        // 振りかぶり角（X軸・負=後ろ上へ）
    [SerializeField] private float slamAngle = 45f;           // 振り下ろし角（X軸・正=前へ）
    [SerializeField] private float returnSpeed = 12f;         // 非攻撃時に元の姿勢へ戻る速さ

    private Attack attack;
    private Quaternion baseRotation; // 直立時の脚の姿勢

    private void Awake()
    {
        // 自分か親のAttackを参照する（モンスター＝ルート直付け／魔族＝形態の見た目prefab内に同梱、の両対応）。
        attack = GetComponentInParent<Attack>();
        if (foreleg != null) baseRotation = foreleg.localRotation;
    }

    private void LateUpdate()
    {
        if (foreleg == null || attack == null) return;

        var move = attack.CurrentMove;
        bool mine = attack.IsAttacking && move != null && move.motionId == motionId;
        if (!mine)
        {
            // 担当外・非攻撃時はなめらかに直立へ戻す。
            foreleg.localRotation = Quaternion.Slerp(foreleg.localRotation, baseRotation, returnSpeed * Time.deltaTime);
            return;
        }

        float t = attack.PhaseProgress;
        float angle;
        switch (attack.CurrentPhase)
        {
            case Attack.Phase.Windup:
                angle = Mathf.Lerp(0f, windupAngle, 1f - (1f - t) * (1f - t)); // easeOut：すっと振りかぶる
                break;
            case Attack.Phase.Active:
                angle = Mathf.Lerp(windupAngle, slamAngle, t * t);             // easeIn：加速して振り下ろす
                break;
            case Attack.Phase.Recovery:
                angle = Mathf.Lerp(slamAngle, 0f, t);                          // ゆっくり戻す
                break;
            default:
                angle = 0f;
                break;
        }
        foreleg.localRotation = baseRotation * Quaternion.Euler(angle, 0f, 0f);
    }
}
