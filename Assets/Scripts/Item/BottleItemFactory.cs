// 保存先: Assets/Scripts/Item/BottleItemFactory.cs
// 瓶用アイテム（BottleItemCore・2D物理）の生成を担う。生成のみ。初期化は呼び出し側が行う。
//   土台prefab（BottleItemCore＋Rigidbody2Dが付いた、ほぼ空のGameObject）をInstantiateする。
//   見た目の3Dモデルは BottleItemCore.Initialize 内で ItemData.Prefab から子生成される（共通使用）。
//   parent には瓶（Bottle）のTransformを渡し、瓶の2D物理空間の子として配置する。
//   ※ 呼び出し側（BottleStorage・ItemPicker等）は Create の後に core.Initialize(data) を呼ぶこと。
using UnityEngine;

public class BottleItemFactory : MonoBehaviour
{
    [SerializeField] private GameObject baseItemPrefab; // BottleItemCore＋2D物理が付いた土台prefab

    // 確定事項: Factory は生成のみ。初期化(Initialize)は呼び出し側が行う。
    //   position/rotation はワールド基準。parent（瓶）の子として配置する。
    public BottleItemCore Create(ItemData data, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (baseItemPrefab == null)
        {
            Debug.LogError($"[BottleItemFactory] baseItemPrefab が未設定です: {name}");
            return null;
        }
        GameObject obj = Instantiate(baseItemPrefab, position, rotation, parent);
        return obj.GetComponent<BottleItemCore>();
    }
}
