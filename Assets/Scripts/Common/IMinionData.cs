using UnityEngine;

public interface IMinionData
{
    MinionStatData Stat { get; }
    GameObject Prefab { get; }
}
