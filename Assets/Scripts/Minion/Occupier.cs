// 保存先: Assets/Scripts/Minion/Occupier.cs
using UnityEngine;

// 攻撃用兵士のみが持つ。行き先Baseを保持し、到着時に分岐する。
[RequireComponent(typeof(MinionCore))]
public class Occupier : MonoBehaviour
{
    private Base destination;
    private MinionCore minionCore;

    private void Awake()
    {
        minionCore = GetComponent<MinionCore>();
        minionCore.OnArrived += OnArrived;
    }

    public void SetDestination(Base destination)
    {
        this.destination = destination;
    }

    private void OnArrived()
    {
        if (destination == null) return;

        var destAI = destination.GetComponent<BaseAI>();
        Team destTeam = destAI.Team;

        if (destTeam == Team.None)
        {
            // 中立 → 占拠要求（土地側が未完成Cityhallを生成し、その core を返す）
            BuildingCore cityhall = destAI.RequestOccupation(minionCore.Team);

            // 生成された未完成Cityhallの位置へ寄る。
            //   Waypoint終端とCityhall位置のズレに依存せず、視界(visionRange)に入れば
            //   BuildingState（優先度1）が建設を始める。
            //   既にCityhallがあった等で null のときは寄らない（その場で待機）。
            if (cityhall != null)
                GetComponent<Movement>()?.MoveTo(cityhall.transform.position);
        }
        else if (destTeam == minionCore.Team)
        {
            // 自国 → すでに自国Cityhallがあるため消滅
            minionCore.Die();
        }
        // 敵国 → 何もしない（Vision/CombatState が敵建物を攻撃で拾う）
    }
}
