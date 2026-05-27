using UnityEngine;
using Workspace.Core;
using Workspace.Enums;
using Workspace.Patterns;

namespace Workspace.Building
{
    public class CityHall : MonoBehaviour
    {
        [SerializeField] private EventChannel onDeathChannel;
        [SerializeField] private EventChannel onCityHallDestroyed;
        [SerializeField] private TeamEventChannel onTeamChanged;

        private TeamMember teamMember;

        void Start()
        {
            teamMember = GetComponent<Core.TeamMember>();

            if (teamMember != null)
                teamMember.OnTeamChanged += OnTeamChanged;

            if (onDeathChannel != null)
                onDeathChannel.AddListener(OnDeath);
        }

        private void OnTeamChanged(Team newTeam)
        {
            onTeamChanged?.Raise(newTeam);
        }

        private void OnDeath()
        {
            onCityHallDestroyed?.Raise();
        }

        void OnDestroy()
        {
            if (teamMember != null)
                teamMember.OnTeamChanged -= OnTeamChanged;

            if (onDeathChannel != null)
                onDeathChannel.RemoveListener(OnDeath);
        }
    }
}