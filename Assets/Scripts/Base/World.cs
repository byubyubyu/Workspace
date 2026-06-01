using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    [SerializeField] private List<Base> bases;
    [SerializeField] private List<Path> paths;

    public List<Base> Bases => bases;
    public List<Path> Paths => paths;

    // Gizmo は各 Base（格子）・各 Path（経路）が自身で描くため、World では描かない。
    // （旧 OnDrawGizmos は空だったので削除）
}