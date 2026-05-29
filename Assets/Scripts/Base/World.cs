using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    [SerializeField] private List<Base> bases;
    [SerializeField] private List<Path> paths;

    public List<Base> Bases => bases;
    public List<Path> Paths => paths;

    private void OnDrawGizmos()
    {
        // GizmoでBaseとPathを視覚化
    }
}
