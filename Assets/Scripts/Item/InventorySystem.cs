// 保存先: Assets/Scripts/Item/InventorySystem.cs
// アイテム機能全体のFacade。プレイヤーに紐づく（瓶はプレイヤーの持ち物）。
//   役割は「初期化の起動順序の保証」に絞る（部品間の参照はInspectorで繋ぐ＝各部品が自己完結）。
//   起動順序：
//     1. Bottle.Build（壁・ゾーンを組む）
//     2. BottleStorage.Initialize（Bottle・Factoryを渡す）
//     3. 供給役（IItemSpawner）の初期化と Spawn（固定配置を開始時に生成）
//   世界（Base・兵士）の初期化とは独立して、自身のタイミングで初期化する。
//
//   ※ 初期インベントリ（開始時に瓶に入っているアイテム）は将来対応（供給源と一緒に詰める）。
//      Baseの初期建物のようにInspectorで設定する余地を残す（フィールド未追加・将来ここに）。
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("瓶まわり")]
    [SerializeField] private Bottle bottle;
    [SerializeField] private BottleData bottleData;
    [SerializeField] private BottleStorage storage;
    [SerializeField] private BottleItemFactory bottleItemFactory;

    [Header("供給")]
    [SerializeField] private MapItemFactory mapItemFactory;
    [SerializeField] private FixedItemSpawner fixedSpawner; // 初期の供給役（固定配置）

    private void Start()
    {
        InitializeAll();
    }

    private void InitializeAll()
    {
        // 1. 瓶の物理空間（壁・ゾーン）を組む。
        if (bottle != null)
        {
            bottle.Build(bottleData);
        }
        else
        {
            Debug.LogError($"[InventorySystem] Bottle が未設定です: {name}");
        }

        // 2. 保持データに Bottle・Factory を渡す。
        if (storage != null && bottle != null && bottleItemFactory != null)
        {
            storage.Initialize(bottle, bottleItemFactory);
        }

        // 3. 供給役を初期化して起動（固定配置を開始時に生成）。
        if (fixedSpawner != null && mapItemFactory != null)
        {
            fixedSpawner.Initialize(mapItemFactory);
            IItemSpawner spawner = fixedSpawner;
            spawner.Spawn();
        }
    }
}
