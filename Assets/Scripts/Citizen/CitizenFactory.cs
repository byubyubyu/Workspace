// 保存先: Assets/Scripts/Citizen/CitizenFactory.cs
// 市民の生成のみを担う（MinionFactory流儀）。初期化は呼び出し側(CitizenManager)が行う。
using UnityEngine;

public class CitizenFactory : MonoBehaviour
{
    public CitizenCore Create(CitizenData data, Vector3 position)
    {
        if (data == null || data.Prefab == null)
        {
            Debug.LogError($"[CitizenFactory] CitizenData/Prefab が未設定です: {name}");
            return null;
        }
        GameObject obj = Instantiate(data.Prefab, position, Quaternion.identity);
        return obj.GetComponent<CitizenCore>();
    }
}
