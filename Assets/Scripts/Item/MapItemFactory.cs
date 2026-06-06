// 保存先: Assets/Scripts/Item/MapItemFactory.cs
// マップ用アイテム（MapItemCore・3D物理）の生成を担う。生成のみ。初期化は呼び出し側が行う。
//   土台prefab（MapItemCore＋Rigidbody＋Colliderが付いた、ほぼ空のGameObject）をInstantiateする。
//   見た目の3Dモデルは MapItemCore.Initialize 内で ItemData.Prefab から子生成される（共通使用）。
//   ※ 呼び出し側（ItemPicker等）は Create の後に core.Initialize(data) を呼ぶこと。
using UnityEngine;

public class MapItemFactory : MonoBehaviour
{
    [SerializeField] private GameObject baseItemPrefab; // MapItemCore＋3D物理が付いた土台prefab

    // 確定事項: Factory は生成のみ。初期化(Initialize)は呼び出し側が行う。
    public MapItemCore Create(ItemData data, Vector3 position)
    {
        if (baseItemPrefab == null)
        {
            Debug.LogError($"[MapItemFactory] baseItemPrefab が未設定です: {name}");
            return null;
        }
        GameObject obj = Instantiate(baseItemPrefab, position, Quaternion.identity);
        return obj.GetComponent<MapItemCore>();
    }
}
