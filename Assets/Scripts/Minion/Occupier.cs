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
        if (destination == null)
        {
            Debug.Log($"[Occupy] Occupier({name}) arrived but destination is null"); // DEBUG
            return;
        }

        var destAI = destination.GetComponent<BaseAI>();
        Team destTeam = destAI.Team;
        Debug.Log($"[Occupy] Occupier({name}) arrived at {destination.name} destTeam={destTeam} myTeam={minionCore.Team}"); // DEBUG

        if (destTeam == Team.None)
        {
            // 中立 → 占拠要求（土地側が未完成Cityhallを生成する）
            Debug.Log("[Occupy]  -> neutral: requesting occupation"); // DEBUG
            destAI.RequestOccupation(minionCore.Team);
            // 以降、兵士は Vision で自国未完成Construction を見つけ BuildingState で建設する
        }
        else if (destTeam == minionCore.Team)
        {
            // 自国 → すでに自国Cityhallがあるため消滅
            Debug.Log("[Occupy]  -> own land: minion dies"); // DEBUG
            minionCore.Die();
        }
        else
        {
            // 敵国 → 何もしない（Vision/CombatState が敵建物を攻撃で拾う）
            Debug.Log("[Occupy]  -> enemy land: do nothing (combat will handle)"); // DEBUG
        }
    }
}