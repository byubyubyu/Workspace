// 保存先: Assets/Scripts/UI/StaminaGaugeSource.cs
using UnityEngine;

// スタミナゲージの供給元。同じGameObjectのStaminaを読む。
// StatBar は IGaugeSource としてこれを参照する（具体型を知らない）。
[RequireComponent(typeof(Stamina))]
public class StaminaGaugeSource : MonoBehaviour, IGaugeSource
{
    private Stamina stamina;

    private void Awake()
    {
        stamina = GetComponent<Stamina>();
    }

    public float Current => stamina != null ? stamina.Current : 0f;
    public float Max => stamina != null ? stamina.Max : 0f;
    public GaugeType Type => GaugeType.Stamina;
    public Team Team => stamina != null ? stamina.Team : Team.None;
}
