using System;
using UnityEngine;
using Workspace.Enums;

namespace Workspace.Core
{
    public class TeamMember : MonoBehaviour
    {
        [SerializeField] private Team team = Team.None;

        public Team Team => team;

        public event Action<Team> OnTeamChanged;

        public void SetTeam(Team newTeam)
        {
            if (team == newTeam) return;
            team = newTeam;
            OnTeamChanged?.Invoke(team);
        }
    }
}