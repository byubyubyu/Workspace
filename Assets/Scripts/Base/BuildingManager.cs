// 保存先: Assets/Scripts/Base/BuildingManager.cs
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

    // 追加: 指定種別の建物が今いくつ建っているかを返す（建設上限 maxCountBase の判定に使う）。
    // BuildingCore.Type を問い合わせるだけ。建物の内部構成には依存しない（疎結合）。
    public int CountByType(BuildingType type)
    {
        int count = 0;
        foreach (var building in buildings.Values)
        {
            if (building.Type == type) count++;
        }
        return count;
    }
}
