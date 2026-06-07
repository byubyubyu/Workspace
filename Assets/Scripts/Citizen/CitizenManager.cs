// 保存先: Assets/Scripts/Citizen/CitizenManager.cs
// Base付属。自分のBaseの市民を生成・管理する（兵士のMinionManagerに相当）。
//   CityhallのOnTeamChangedを購読し、完成/占拠でTeamが決まったらそのTeamの市民を湧かせ、
//   占拠でTeamが変わったら入れ替え、破壊(None)で消す。徘徊範囲は自分のBase。
//   将来：人数(Population)リソースで上限管理（今は固定数 count）。
using System.Collections.Generic;
using UnityEngine;

public class CitizenManager : MonoBehaviour
{
    [SerializeField] private CitizenFactory factory;
    [SerializeField] private CitizenData citizenData; // にぎやかし市民（将来：商人など複数種に拡張）
    [SerializeField] private int count = 3;           // にぎやかし市民の人数（将来：人口リソース化）
    [SerializeField] private CitizenData merchantData; // 商人（固定・1人）。nullなら商人なし
    [SerializeField] private Vector2Int merchantCell;  // 商人の固定位置（Baseのマス座標）

    private Base homeBase;
    private readonly List<CitizenCore> citizens = new List<CitizenCore>();

    private void Awake()
    {
        homeBase = GetComponent<Base>();
    }

    // Base.AnnounceCityhall から呼ばれ、自分のBaseのCityhallのTeam変化を購読する。
    public void BindCityhall(CityhallBehavior cityhall)
    {
        if (cityhall == null) return;
        cityhall.OnTeamChanged += OnCityhallTeamChanged;

        // 既に完成・自国なら即生成（初期配置の即完成を購読前に逃した場合の保険。順序非依存にする）。
        var construction = cityhall.GetComponent<Construction>();
        var core = cityhall.GetComponent<BuildingCore>();
        if (construction != null && construction.IsCompleted && core != null && core.Team != Team.None)
            OnCityhallTeamChanged(core.Team);
    }

    // Cityhallが完成/占拠/破壊でTeamが変わったとき。既存市民を消して、自国なら新しく湧かせる。
    private void OnCityhallTeamChanged(Team team)
    {
        ClearCitizens();
        if (team != Team.None) SpawnCitizens(team);
    }

    private void SpawnCitizens(Team team)
    {
        if (factory == null || citizenData == null || homeBase == null) return;
        var size = homeBase.GridSize;
        if (size.x <= 0 || size.y <= 0) return;

        for (int i = 0; i < count; i++)
        {
            var cell = new Vector2Int(Random.Range(0, size.x), Random.Range(0, size.y));
            Vector3 pos = homeBase.GridToWorld(cell);
            var citizen = factory.Create(citizenData, pos);
            if (citizen == null) continue;
            citizen.Initialize(citizenData, team, homeBase);
            citizens.Add(citizen);
        }

        // 商人（固定位置・1人）。Wanderを持たない前提なので徘徊しない。
        if (merchantData != null)
        {
            Vector3 mpos = homeBase.GridToWorld(merchantCell);
            var merchant = factory.Create(merchantData, mpos);
            if (merchant != null)
            {
                merchant.Initialize(merchantData, team, homeBase);
                citizens.Add(merchant);
            }
        }
    }

    private void ClearCitizens()
    {
        foreach (var c in citizens)
            if (c != null) Destroy(c.gameObject);
        citizens.Clear();
    }
}
