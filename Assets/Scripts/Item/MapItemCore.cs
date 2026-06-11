// 保存先: Assets/Scripts/Item/MapItemCore.cs
// マップに落ちているアイテム（3D物理）。プレイヤーに拾われ待ちの状態。
//   責務は軽い：ItemDataを保持する／3D物理で地面に落ちている／見た目（任意で3Dモデル）。
//   検知される側＝自己申告レジストリ（All）に登録し、ItemPickerがNearestFinder.Findで選ぶ。
//   「拾う」橋渡しはItemPickerが主導する（拾われた時のDestroyはItemPicker）。
//   時限消滅：こぼれ・ドロップ品は ItemData.MapLifetime 秒で自然に消える
//   （persistent=true＝初期配置などは消えない。0=その種類は消えない）。
//
//   当たり判定：マップ上では形の戦略性は不要なので一律 BoxCollider（角があり転がりにくい。簡素）。
//   見た目：今は単純図形で進める段階。prefab(3Dモデル)があれば子として生成、無ければ何もしない（仮実装）。
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MapItemCore : MonoBehaviour
{
    // 生存中の全マップアイテム（自己申告レジストリ）。
    public static readonly System.Collections.Generic.List<MapItemCore> All = new System.Collections.Generic.List<MapItemCore>();

    private ItemData data;
    private bool persistent; // true=時限消滅しない（初期配置など）
    private float lifeTimer;

    // ItemPickerが拾うときに読む（種類データの受け渡し）。
    public ItemData Data => data;

    private void OnEnable() { All.Add(this); }
    private void OnDisable() { All.Remove(this); } // 破棄時も呼ばれる＝解除漏れなし

    private void Update()
    {
        // 時限消滅（こぼれ・ドロップ品の掃除。persistentと寿命0の種類は対象外）。
        if (persistent || data == null || data.MapLifetime <= 0f) return;
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= data.MapLifetime) Destroy(gameObject);
    }

    public void Initialize(ItemData itemData, bool persistent = false)
    {
        data = itemData;
        this.persistent = persistent;
        lifeTimer = 0f;

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

        // 当たり判定：一律 BoxCollider（角があり転がりにくい・簡素）。寸法はSize（幅,高さ）、奥行きは幅と同じ。
        var col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        float w = Mathf.Max(0.01f, data.Size.x);
        float h = Mathf.Max(0.01f, data.Size.y);
        col.size = new Vector3(w, h, w);

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
