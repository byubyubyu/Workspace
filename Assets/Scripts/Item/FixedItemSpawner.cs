// 保存先: Assets/Scripts/Item/FixedItemSpawner.cs
// 固定配置の供給役。データ駆動で開始時にマップへアイテムを生成する（初期建物のinitialBuildingsと同じ発想）。
//   「どこに・何を置くか」をInspectorのリストで持つ。生成は MapItemFactory に依頼（生成のみ）。
//   拾ったら復活しない（有限）＝一度Spawnするだけ。リスポーンは将来の別Spawnerが担う。
//   起動（Spawn）は InventorySystem が呼ぶ。
using System.Collections.Generic;
using UnityEngine;

public class FixedItemSpawner : MonoBehaviour, IItemSpawner
{
    // 配置1件：何を(data)・どこに(position・ワールド座標)。
    [System.Serializable]
    public struct Placement
    {
        public ItemData data;
        public Vector3 position;
    }

    [SerializeField] private MapItemFactory mapItemFactory;
    [SerializeField] private List<Placement> placements = new List<Placement>();

    public void Initialize(MapItemFactory factory)
    {
        mapItemFactory = factory;
    }

    public void Spawn()
    {
        if (mapItemFactory == null)
        {
            Debug.LogError($"[FixedItemSpawner] MapItemFactory が未設定です: {name}");
            return;
        }

        for (int i = 0; i < placements.Count; i++)
        {
            var p = placements[i];
            if (p.data == null) continue;

            // 生成はFactoryに依頼（生成のみ）。初期化は呼び出し側＝ここで行う。
            MapItemCore core = mapItemFactory.Create(p.data, p.position);
            if (core != null) core.Initialize(p.data);
        }
    }

    // デバッグ用：シーン編集時、固定配置の予定位置を赤マーカーで表示する（ゲーム中は無関係）。
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < placements.Count; i++)
        {
            Gizmos.DrawWireSphere(placements[i].position, 0.3f);
        }
    }
}
