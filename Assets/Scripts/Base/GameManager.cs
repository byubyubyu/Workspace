using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private World world;
    [SerializeField] private List<CityhallBehavior> initialCityhalls;

    private void Start()
    {
        InitializeBases();
        InitializePaths();
        InitializeNeighborSubscriptions();
        InitializeInitialCityhalls();
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
                b.GetComponent<MinionManager>()
            );
        }
    }

    private void InitializePaths()
    {
        foreach (var path in world.Paths)
        {
            path.Initialize(path.ConnectedBases, path.Waypoints);
        }
    }

    private void InitializeNeighborSubscriptions()
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

    private void InitializeInitialCityhalls()
    {
        foreach (var cityhall in initialCityhalls)
        {
            var baseAI = cityhall.GetComponentInParent<BaseAI>();
            cityhall.Initialize(baseAI.Team);
        }
    }

    private void StartGameLoop()
    {
        // ゲームループ開始
    }
}
