using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    private Dictionary<Vector2Int, BuildingCore> buildings = new Dictionary<Vector2Int, BuildingCore>();
    private Vector2Int gridSize;

    public void Initialize(Vector2Int gridSize)
    {
        this.gridSize = gridSize;
    }

    public bool IsCellEmpty(Vector2Int cell)
    {
        return !buildings.ContainsKey(cell);
    }

    public Vector2Int? GetEmptyCell()
    {
        for (int x = 0; x < gridSize.x; x++)
        for (int y = 0; y < gridSize.y; y++)
        {
            var cell = new Vector2Int(x, y);
            if (!buildings.ContainsKey(cell)) return cell;
        }
        return null;
    }

    public void AddBuilding(BuildingCore building, Vector2Int cell)
    {
        buildings[cell] = building;
        building.OnDestroyed += () => RemoveBuilding(cell);

        var cityhall = building.GetComponent<CityhallBehavior>();
        if (cityhall != null)
            cityhall.OnCityhallDestroyed += DestroyAllBuildings;
    }

    private void RemoveBuilding(Vector2Int cell)
    {
        buildings.Remove(cell);
    }

    public void DestroyAllBuildings()
    {
        foreach (var building in buildings.Values)
            GameObject.Destroy(building.gameObject);
        buildings.Clear();
    }

    public BuildingCore GetBuilding(Vector2Int cell)
    {
        buildings.TryGetValue(cell, out var building);
        return building;
    }

    public CityhallBehavior GetCityhall()
    {
        foreach (var building in buildings.Values)
        {
            var cityhall = building.GetComponent<CityhallBehavior>();
            if (cityhall != null) return cityhall;
        }
        return null;
    }
}
