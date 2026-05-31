// 保存先: Assets/Scripts/Base/GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private World world;

    private void Start()
    {
        InitializeBases();
        PlaceInitialBuildings();   // 先にCityhallを生成してTeamを確定させる
        InitializeNeighborTeams(); // その後で隣接Teamを読む（空振り防止）
        SubscribeNeighborChanges();
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

    // 起動時に隣接Teamを直接読む（イベント任せにしない）
    private void InitializeNeighborTeams()
    {
        foreach (var b in world.Bases)
            b.GetComponent<BaseAI>().InitializeNeighborTeams();
    }

    // 以降のTeam変化(占拠など)はイベントで更新する。Cityhall生成後なので購読も張れる。
    private void SubscribeNeighborChanges()
    {
        foreach (var b in world.Bases)
        {
            var baseAI = b.GetComponent<BaseAI>();
            foreach (var path in b.Paths)
            {
                foreach (var neighborBase in path.ConnectedBases)
                {
                    if (neighborBase == b) continue;
                    var cityhall = neighborBase.GetComponentInChildren<CityhallBehavior>();
                    if (cityhall != null)
                        cityhall.OnTeamChanged += (team) => baseAI.UpdateNeighborTeam(neighborBase, team);
                }
            }
        }
    }

    private void StartGameLoop()
    {
        foreach (var b in world.Bases)
            b.GetComponent<BaseAI>().StartDecision();
    }
}
