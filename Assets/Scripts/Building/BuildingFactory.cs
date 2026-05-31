// 保存先: Assets/Scripts/Building/BuildingFactory.cs
using UnityEngine;

public class BuildingFactory : MonoBehaviour
{
    // 確定事項: Factory は生成のみ。初期化(Initialize)は呼び出し側が行う。
    public BuildingCore Create(IBuildingData data, Vector3 position)
    {
        GameObject obj = Instantiate(data.Prefab, position, Quaternion.identity);
        return obj.GetComponent<BuildingCore>();
    }
}
