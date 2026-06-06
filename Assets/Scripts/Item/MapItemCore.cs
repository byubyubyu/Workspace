// 保存先: Assets/Scripts/Item/MapItemCore.cs
// マップに落ちているアイテム（3D物理）。プレイヤーに拾われ待ちの状態。
//   責務は軽い：ItemDataを保持する／3D物理で地面に落ちている／見た目（任意で3Dモデル）。
//   タグ"Item"でItemPickerの検知に引っかかる（検知される側）。
//   「拾う」橋渡しはItemPickerが主導する。MapItemCoreは Data を公開するだけで、
//   自分からは消えない（消滅もItemPickerがDestroyする＝受け身）。
//
//   当たり判定：マップ上では形の戦略性は不要なので一律 SphereCollider（簡素）。
//   見た目：今は単純図形で進める段階。prefab(3Dモデル)があれば子として生成、無ければ何もしない（仮実装）。
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MapItemCore : MonoBehaviour
{
    private ItemData data;

    // ItemPickerが拾うときに読む（種類データの受け渡し）。
    public ItemData Data => data;

    public void Initialize(ItemData itemData)
    {
        data = itemData;

        if (data == null)
        {
            Debug.LogError($"[MapItemCore] ItemData が null です: {name}");
            return;
        }

        // タグ（ItemPickerのOverlapSphere＋タグ判定で検知される側）。
        //   ※ プロジェクトに "Item" タグを追加しておくこと（未登録だとSetTagで例外）。
        gameObject.tag = "Item";

        // 3D物理：質量はItemDataから（瓶用と値を共通化できる）。
        var rb = GetComponent<Rigidbody>();
        rb.mass = data.Mass;

        // 当たり判定：一律 SphereCollider（簡素）。半径はSizeのxを直径とみなし半分。
        var col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.radius = Mathf.Max(0.01f, data.Size.x * 0.5f);

        // 見た目（仮実装）：3Dモデルprefabがあれば子として生成。無ければ単純図形のまま。
        if (data.Prefab != null)
        {
            var view = Instantiate(data.Prefab, transform);
            view.transform.localPosition = Vector3.zero;
            view.transform.localRotation = Quaternion.identity;
            // 元モデルの大きさはまちまちなので、マップでの目標サイズ（視認性基準）に等比で合わせる。
            ItemViewScaler.FitToSize(view, data.MapViewSize);
        }
    }
}
