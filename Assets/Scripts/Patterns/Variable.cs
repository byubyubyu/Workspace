using System;
using UnityEngine;

namespace Workspace.Patterns
{
    public abstract class Variable<T> : ScriptableObject
    {
        [SerializeField] private T initialValue;
        private T runtimeValue;

        public event Action<T> OnValueChanged;

        public T Value
        {
            get => runtimeValue;
            set
            {
                if (Equals(runtimeValue, value)) return;
                runtimeValue = value;
                OnValueChanged?.Invoke(runtimeValue);
            }
        }

        private void OnEnable()
        {
            runtimeValue = initialValue;
        }
    }
}