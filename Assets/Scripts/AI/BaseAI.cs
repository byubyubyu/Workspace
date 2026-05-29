using System.Collections.Generic;
using UnityEngine;

public class BaseAI : MonoBehaviour
{
    [SerializeField] private List<BuildingPriorityData> buildingPriorities;
    [SerializeField] private Team team;

    private BuildingManager buildingManager;
    private MinionManager minionManager;
    private Dictionary<Base, Team> neighborTeams = new Dictionary<Base, Team>();

    // TODO: 将来：補給用兵士の派遣

    public Team Team => team;

    public void Initialize(BuildingManager buildingManager, MinionManager minionManager)
    {
        this.buildingManager = buildingManager;
        this.minionManager = minionManager;

        var cityhall = GetComponent<CityhallBehavior>();
        if (cityhall != null)
            cityhall.OnTeamChanged += (newTeam) => team = newTeam;
    }

    public void UpdateNeighborTeam(Base neighborBase, Team team)
    {
        neighborTeams[neighborBase] = team;
    }

    private void DecideBuilding()
    {
        // BuildingPriorityDataで評価値を計算して建物を決定
    }

    private void DecideMinion()
    {
        // 種類・数をランダムに決定
    }

    private void DecideDispatch()
    {
        // 攻撃用兵士を中立・敵のBaseにランダムで派遣
    }
}
