using UnityEngine;

public class BuildingFactory : MonoBehaviour
{
    public BuildingCore Create(IBuildingData data, Vector3 position)
    {
        GameObject obj = Instantiate(data.Prefab, position, Quaternion.identity);
        BuildingCore core = obj.GetComponent<BuildingCore>();
        core.Initialize(data);
        return core;
    }
}
