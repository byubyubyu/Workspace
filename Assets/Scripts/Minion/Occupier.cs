// 保存先: Assets/Scripts/Minion/Occupier.cs
// 攻撃用兵士のみが持つ。目的地Baseへ占拠に向かい、到着後は継続的に状況へ寄り直す。
//   ・到着後、手が空いている（戦闘中でも建設中でもない）時だけ、目的地の今のTeamで分岐して動く。
//     自国 → Die（占拠完了）／中立 → 未完成Cityhallへ寄って建設／敵国 → Baseへ寄って視界で敵Cityhallを拾う。
//   ・戦闘で逸れても、手が空けば毎回 MoveTo で寄り直す（塊3-D 寄り直し）。
//   ・敵Cityhallを壊して中立化したら、手が空いた時に「中立」と判断して再占拠する（塊3-D 再占拠）。
//   ・戦闘/建設中はCombatState/BuildingStateに譲る（視界クエリで判定＝StateMachineと同じCombat>Building>移動の序列）。
using UnityEngine;

[RequireComponent(typeof(MinionCore))]
public class Occupier : MonoBehaviour
{
    private enum Mode { None, ToCityhall, ToBase } // 直近に発行したMoveToの種類（目標変化の検出用）

    private Base destination;
    private MinionCore minionCore;
    private Movement movement;
    private Vision vision;

    private bool arrived;            // Waypoint移動を終えて目的地Baseに着いたか
    private BuildingCore cityhall;   // 中立占拠で受け取った自国の未完成Cityhall（取得済みなら保持）
    private bool wasEngaged;         // 前フレーム、戦闘or建設中だったか（手が空いた瞬間の検出用）
    private Mode lastMode = Mode.None;

    private void Awake()
    {
        minionCore = GetComponent<MinionCore>();
        movement = GetComponent<Movement>();
        vision = GetComponent<Vision>();
        minionCore.OnArrived += OnArrived;
    }

    public void SetDestination(Base destination)
    {
        this.destination = destination;
    }

    private void OnArrived()
    {
        arrived = true; // 以降の判断は Update が継続的に行う
    }

    private void Update()
    {
        if (!arrived || destination == null) return;
        if (vision == null || movement == null) return;

        // 戦闘中・建設中はそれぞれの状態に譲る（手は出さない）。
        if (vision.HasEnemy() || vision.HasBuildTarget())
        {
            wasEngaged = true;
            return;
        }

        // 手が空いている。目的地の今のTeamで分岐する。
        var destAI = destination.GetComponent<BaseAI>();
        Team destTeam = destAI.Team;

        if (destTeam == minionCore.Team)
        {
            minionCore.Die(); // 占拠完了（自国化）。役目を終えて消滅。
            return;
        }

        bool justFreed = wasEngaged; // 戦闘/建設明けの最初のフレームか
        wasEngaged = false;

        if (destTeam == Team.None)
        {
            // 中立：自国の未完成Cityhallが未取得（または壊れた）なら要求して受け取る。
            if (cityhall == null || (cityhall as Object) == null)
            {
                cityhall = destAI.RequestOccupation(minionCore.Team);
                lastMode = Mode.None; // 取り直したので再発行させる
            }
            // 手が空いた瞬間、または目標種別が変わった時だけ MoveTo を出し直す（無駄な再計算を避ける）。
            if (cityhall != null && (justFreed || lastMode != Mode.ToCityhall))
            {
                movement.MoveTo(cityhall.transform.position);
                lastMode = Mode.ToCityhall;
            }
        }
        else
        {
            // 敵国：敵Cityhallの位置へ寄れば、視界が敵建物を拾いCombatStateが攻撃する。
            //   Base隅(transform.position)だと視界外で拾えないことがあるため本体位置へ寄る。
            //   Cityhallが無い瞬間はBase位置にフォールバック。
            Vector3 target = destination.transform.position;
            Vector3? chPos = destAI.GetCityhallPosition();
            if (chPos.HasValue) target = chPos.Value;

            if (justFreed || lastMode != Mode.ToBase)
            {
                movement.MoveTo(target);
                lastMode = Mode.ToBase;
            }
        }
    }
}
