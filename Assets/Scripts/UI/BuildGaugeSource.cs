// 保存先: Assets/Scripts/UI/BuildGaugeSource.cs
using UnityEngine;

// 建設進捗ゲージの供給元。同じGameObjectのConstructionを読む。
// Teamは同じGameObjectのBuildingCoreから取得（建物の所属＝外枠の国色）。
[RequireComponent(typeof(Construction))]
public class BuildGaugeSource : MonoBehaviour, IGaugeSource
{
    private Construction construction;
    private BuildingCore core;

    private void Awake()
    {
        construction = GetComponent<Construction>();
        core = GetComponent<BuildingCore>();
    }

    public float Current => construction != null ? construction.CurrentBuildPoint : 0f;
    public float Max => construction != null ? construction.NeedBuildPoint : 0f;
    public GaugeType Type => GaugeType.Build;
    public Team Team => core != null ? core.Team : Team.None;
}
