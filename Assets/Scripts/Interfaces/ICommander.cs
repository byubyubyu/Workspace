using System;
using UnityEngine;

namespace Workspace.Interfaces
{
    public interface ICommander
    {
        event Action<Vector3> OnAttackOrdered;
        event Action<Vector3> OnSupplyOrdered;
        event Action<Vector3> OnRepairOrdered;
    }
}