using System.Collections.Generic;
using UnityEngine;

public class Base : MonoBehaviour
{
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private List<Path> paths;

    public Vector2Int GridSize => gridSize;
    public List<Path> Paths => paths;
}
