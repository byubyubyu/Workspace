// 保存先: Assets/Scripts/UI/HpGaugeSource.cs
using UnityEngine;

// HPゲージの供給元。同じGameObjectのIHealth(MinionCore/BuildingCore)を読む。
// StatBar は IGaugeSource としてこれを参照する（具体的なCoreは知らない）。
public class HpGaugeSource : MonoBehaviour, IGaugeSource
{
    private IHealth health;

    private void Awake()
    {
        health = GetComponent<IHealth>();
    }

    public float Current => health != null ? health.CurrentHp : 0f;
    public float Max => health != null ? health.MaxHp : 0f;
    public GaugeType Type => GaugeType.Hp;
    public Team Team => health != null ? health.Team : Team.None;
}
