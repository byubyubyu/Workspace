// 保存先: Assets/Scripts/Base/GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private World world;

    private void Start()
    {
        InitializeBases();
        PlaceInitialBuildings();   // 先にCityhallを生成してTeamを確定させる。
                                   // この生成時に各Baseが隣接へ購読を張る（AnnounceCityhall）。
        InitializeNeighborTeams(); // その後で隣接Teamの初期値を直接読む（空振り防止）。
        StartGameLoop();
    }

    private void InitializeBases()
    {
        foreach (var b in world.Bases)
        {
            var buildingManager = b.GetComponent<BuildingManager>();
            buildingManager.Initialize(b.GridSize);
            b.GetComponent<BaseAI>().Initialize(
                buildingManager,
                b.GetComponent<MinionManager>(),
                b.GetComponent<BuildingFactory>(),
                b.GetComponent<MinionFactory>()
            );
        }
    }

    private void PlaceInitialBuildings()
    {
        foreach (var b in world.Bases)
            b.GetComponent<BaseAI>().PlaceInitialBuildings();
    }

    // 起動時に隣接Teamを直接読む（イベント任せにしない）。
    private void InitializeNeighborTeams()
    {
        foreach (var b in world.Bases)
            b.GetComponent<BaseAI>().InitializeNeighborTeams();
    }

    // 旧 SubscribeNeighborChanges は廃止。
    //   理由: 隣接 Cityhall を GetComponentInChildren で探していたが Cityhall は別GameObjectのため空振り、
    //         かつ Start で1回だけのため占拠の後付け Cityhall を購読できなかった。
    //   現在: 隣接購読は Cityhall 生成時の Base.AnnounceCityhall に一本化（初期配置・占拠の両方を網羅）。

    private void StartGameLoop()
    {
        foreach (var b in world.Bases)
            b.GetComponent<BaseAI>().StartDecision();
    }
}