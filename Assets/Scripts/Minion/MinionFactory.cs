using UnityEngine;

public class MinionFactory : MonoBehaviour
{
    public MinionCore Create(IMinionData data, Vector3 position)
    {
        GameObject obj = Instantiate(data.Prefab, position, Quaternion.identity);
        MinionCore core = obj.GetComponent<MinionCore>();
        core.Initialize(data);
        return core;
    }
}
