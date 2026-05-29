using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProductionStatData", menuName = "Project/Building/ProductionStatData")]
public class ProductionStatData : ScriptableObject
{
    public List<MinionData> minionDatas;
    public float productionSpeed;
}
