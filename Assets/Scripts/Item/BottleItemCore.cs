// 保存先: Assets/Scripts/Item/BottleItemCore.cs
// 瓶の中のアイテム（2D物理）。受け身／自己完結の責務だけを持つ。
//   ・ItemData保持。2D物理（Rigidbody2D＋Collider2D。mass/frictionをItemDataから適用）。
//   ・状態(ItemState)を持つ。状態は外（Bottle・操作役）から変えられる（受け身）。
//   ・口から取り出されたら使用される（効果Use）。
//   ・見た目の3Dモデルは子オブジェクト（物理は2D・見た目は3D＝分離。今は仮実装）。
//   「掴む・ドラッグ操作・こぼれ検知」など能動処理は持たない（Bottle・BottleDraggerが担う）。
//
//   形に応じた当たり判定：Circle→CircleCollider2D / Box・Long→BoxCollider2D。
//   摩擦：PhysicsMaterial2D を生成し friction を設定して割り当てる。
//   ※ 収納済み判定（内側ゾーン）との連携は③Bottleで繋ぐ。ここでは MarkStored 等の受け口だけ用意する。
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BottleItemCore : MonoBehaviour
{
    private ItemData data;
    private ItemState state = ItemState.Falling; // 生成直後は落下中

    public ItemData Data => data;
    public ItemState State => state;

    public void Initialize(ItemData itemData)
    {
        data = itemData;

        if (data == null)
        {
            Debug.LogError($"[BottleItemCore] ItemData が null です: {name}");
            return;
        }

        // 2D物理：質量をItemDataから。
        var rb = GetComponent<Rigidbody2D>();
        rb.mass = data.Mass;

        // 摩擦：PhysicsMaterial2D を作って friction を設定。
        var mat = new PhysicsMaterial2D($"{data.ItemName}_PhysMat")
        {
            friction = data.Friction,
            bounciness = 0f,
        };

        // 形に応じた当たり判定（瓶の中は形が戦略になる本番）。
        Collider2D col = BuildCollider();
        if (col != null) col.sharedMaterial = mat;
        rb.sharedMaterial = mat; // 念のためRigidbody側にも（衝突相手にmaterialが無い場合の保険）

        // 見た目（仮実装）：3Dモデルprefabがあれば子として生成。正面固定2D風の見せ方は後で詰める。
        if (data.Prefab != null)
        {
            var view = Instantiate(data.Prefab, transform);
            view.transform.localPosition = Vector3.zero;
            view.transform.localRotation = Quaternion.identity;
            // 元モデルの大きさはまちまちなので、瓶での目標サイズ（当たり判定＝Sizeに合わせる）に等比で合わせる。
            ItemViewScaler.FitToSize(view, data.BottleViewSize);
        }

        state = ItemState.Falling;
    }

    // 形（ItemShape）に対応するCollider2Dを付けて返す。Sizeは寸法（Circleはx=直径）。
    private Collider2D BuildCollider()
    {
        switch (data.Shape)
        {
            case ItemShape.Circle:
            {
                var c = gameObject.AddComponent<CircleCollider2D>();
                c.radius = Mathf.Max(0.01f, data.Size.x * 0.5f);
                return c;
            }
            case ItemShape.Box:
            case ItemShape.Long: // LongはBoxの細長い比率（Sizeで表現）
            {
                var b = gameObject.AddComponent<BoxCollider2D>();
                b.size = new Vector2(Mathf.Max(0.01f, data.Size.x), Mathf.Max(0.01f, data.Size.y));
                return b;
            }
            default:
                Debug.LogError($"[BottleItemCore] 未対応のItemShape: {data.Shape} ({name})");
                return null;
        }
    }

    // --- 状態の受け口（外＝Bottle・操作役が呼ぶ。BottleItemCoreは受け身） ---

    // 内側ゾーンに入って収納済みになったときに呼ばれる（③Bottleが検知して呼ぶ）。
    public void MarkStored()
    {
        // 落下中→収納済みのみ許可（こぼれ中などからは戻さない）。
        if (state == ItemState.Falling) state = ItemState.Stored;
    }

    // 掴まれた／離されたときに呼ばれる（BottleDraggerが呼ぶ）。
    public void MarkDragging() => state = ItemState.Dragging;
    public void ReleaseDragging()
    {
        // 離したら瓶に落ちて戻る扱い。収納済みに戻す。
        if (state == ItemState.Dragging) state = ItemState.Stored;
    }

    // こぼれ落下中に遷移（Bottleが口の外で収納済みアイテムを検知したとき）。
    public void MarkSpilling() => state = ItemState.Spilling;

    // 取り出し成功＝使用（Bottleが口の外でドラッグ中アイテムを検知したとき）。
    //   効果がnull可なので存在チェックしてからUse。
    public void UseItem()
    {
        data?.Effect?.Use();
    }
}
