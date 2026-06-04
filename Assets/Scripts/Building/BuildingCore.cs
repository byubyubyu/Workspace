// 保存先: Assets/Scripts/Building/BuildingCore.cs
using System;
using UnityEngine;

public class BuildingCore : MonoBehaviour, IBattleInfo, IHealth
{
    private Health health;
    private bool destroyed; // 二重破壊防止
    public BuildingType Type { get; private set; }
    public Team Team { get; private set; }
    public Vector3 Position => transform.position; // IBattleInfo

    // IHealth（ゲージ表示用）。中身はHealthに委譲する。
    public float Current => health != null ? health.Current : 0f;
    public float Max => health != null ? health.Max : 0f;

    public event Action OnDestroyed;

    public void Initialize(IBuildingData data, Team team, float startCost)
    {
        health = new Health(data.Stat.hp);
        Type = data.Type;
        Team = team;

        var construction = GetComponent<Construction>();
        if (construction != null)
            construction.Initialize(data, data.BuildStrategy);

        var cityhall = GetComponent<CityhallBehavior>();
        if (cityhall != null && data is CityhallData cityhallData)
            cityhall.Initialize(team, cityhallData.CostMax, cityhallData.CostRecovery, startCost);

        // 子のHurtboxに自分(IBattleInfo)を渡す（Hitboxがここから取得してダメージを渡す）。
        var hurtbox = GetComponentInChildren<Hurtbox>(true);
        if (hurtbox != null) hurtbox.SetOwner(this);
    }

    public void SetTeam(Team team) { Team = team; }

    public void TakeDamage(BattleInfo info)
    {
        if (destroyed) return; // 既に破壊済みなら無視（同フレーム多段ヒットの二重発火防止）

        // BattleInfoの解釈はCore側の責務。建物は当面 defense=0（素のダメージをそのまま受ける）。
        //   将来、兵士と同様にdefenseを持たせる余地を残す（DamageCalculatorに渡す値を変えるだけ）。
        // HP減算・0検知はHealthに委譲し、破壊の「始末」（OnDestroyed発火＋本体Destroy）はCoreが行う。
        float damage = DamageCalculator.Calc(info.attackPower, 0f);
        health.TakeDamage(damage);
        if (health.IsEmpty)
        {
            destroyed = true;
            OnDestroyed?.Invoke();  // 辞書からの除外・Cityhallなら同土地全消滅 等の通知
            Destroy(gameObject);    // 建物本体を実際に消す（Cityhall以外もこれで壊れる）
        }
    }
}
