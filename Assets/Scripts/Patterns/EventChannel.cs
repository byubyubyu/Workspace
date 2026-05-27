using System;
using UnityEngine;
using Workspace.Enums;

namespace Workspace.Patterns
{
    [CreateAssetMenu(fileName = "EventChannel", menuName = "Architecture/EventChannel/Void")]
    public class EventChannel : ScriptableObject
    {
        private Action onEventRaised;

        public void AddListener(Action listener) => onEventRaised += listener;
        public void RemoveListener(Action listener) => onEventRaised -= listener;
        public void Raise() => onEventRaised?.Invoke();
    }

    [CreateAssetMenu(fileName = "TeamEventChannel", menuName = "Architecture/EventChannel/Team")]
    public class TeamEventChannel : ScriptableObject
    {
        private Action<Team> onEventRaised;

        public void AddListener(Action<Team> listener) => onEventRaised += listener;
        public void RemoveListener(Action<Team> listener) => onEventRaised -= listener;
        public void Raise(Team team) => onEventRaised?.Invoke(team);
    }

    [CreateAssetMenu(fileName = "FloatEventChannel", menuName = "Architecture/EventChannel/Float")]
    public class FloatEventChannel : ScriptableObject
    {
        private Action<float> onEventRaised;

        public void AddListener(Action<float> listener) => onEventRaised += listener;
        public void RemoveListener(Action<float> listener) => onEventRaised -= listener;
        public void Raise(float value) => onEventRaised?.Invoke(value);
    }
}