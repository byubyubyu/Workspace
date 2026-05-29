using UnityEngine;

[CreateAssetMenu(fileName = "MinionData", menuName = "Project/Minion/MinionData")]
public class MinionData : ScriptableObject, IMinionData
{
    [SerializeField] private MinionStatData stat;
    [SerializeField] private GameObject prefab;
    public MinionStatData Stat => stat;
    public GameObject Prefab => prefab;
}
