// 保存先: Assets/Scripts/Item/ItemPicker.cs
// プレイヤーに付ける「拾い手」。マップ⇔瓶の双方向の橋渡しを集約する。
//   ・毎フレーム周囲を3D範囲検知（Physics.OverlapSphere、タグ"Item"）し、最寄り1個を把握（Nearestで公開）。
//     毎フレーム把握は将来「拾える物を光らせる」等の演出に備えるため。
//   ・拾うキー押下：最寄りMapItemCoreのItemDataを取得 → 瓶UIを開く → 瓶に生成して入れる → マップの実体を破棄。
//   ・こぼれ（Bottle.OnSpilled）を購読：こぼれたItemDataをプレイヤー周囲にMapItemCoreとして生成（再取得可能）。
//   入力は新Input Systemで直接読み（既存方針：各コンポーネントが自分の入力を読む）。
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPicker : MonoBehaviour
{
    [Header("参照（DI・Inspector割り当て。⑨での一元化は後判断）")]
    [SerializeField] private Bottle bottle;
    [SerializeField] private BottleUIController bottleUI;
    [SerializeField] private BottleItemFactory bottleItemFactory;
    [SerializeField] private MapItemFactory mapItemFactory;

    [Header("拾う検知")]
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private Key pickupKey = Key.E;

    [Header("こぼれをマップに戻すときの散らばり")]
    [SerializeField] private float spillScatterRadius = 1f;
    [SerializeField] private float spillSpawnHeight = 1f; // プレイヤー頭上から落とす高さ

    private MapItemCore nearest;
    private static readonly Collider[] hits = new Collider[16];

    // 将来の演出（拾える物を光らせる等）のために最寄りを公開。
    public MapItemCore Nearest => nearest;

    private void OnEnable()
    {
        if (bottle != null) bottle.OnSpilled += OnSpilled;
    }

    private void OnDisable()
    {
        if (bottle != null) bottle.OnSpilled -= OnSpilled;
    }

    private void Update()
    {
        RefreshNearest();

        var kb = Keyboard.current;
        if (kb != null && kb[pickupKey].wasPressedThisFrame)
        {
            TryPickup();
        }
    }

    // 周囲を3D検知し、タグ"Item"の最寄りMapItemCoreを把握する。
    private void RefreshNearest()
    {
        nearest = null;
        float best = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(transform.position, pickupRange, hits);
        for (int i = 0; i < count; i++)
        {
            var col = hits[i];
            if (col == null) continue;
            if (!col.CompareTag("Item")) continue;

            var item = col.GetComponentInParent<MapItemCore>();
            if (item == null) continue;

            float d = (item.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = item;
            }
        }
    }

    // 最寄りを拾う：瓶を開く→瓶に生成して入れる→マップの実体を破棄。
    private void TryPickup()
    {
        if (nearest == null) return;

        ItemData data = nearest.Data;
        if (data == null) return;

        // 瓶UIを自動で開く（収納の様子を見せる）。
        if (bottleUI != null) bottleUI.OpenBottle();

        // 瓶に入れる（共通処理）。
        PutIntoBottle(data);

        // マップの実体を破棄（橋渡しは拾い手が主導）。
        Destroy(nearest.gameObject);
        nearest = null;
    }

    // アイテムを瓶に入れる（拾った時・手持ちアイテムを瓶に戻す時の共通処理）。
    //   PlayerHandStateがインベントリを開いた際、手持ちアイテムを瓶に戻すのにも使う（重複回避）。
    public void PutIntoBottle(ItemData data)
    {
        if (data == null) return;
        if (bottle == null || bottleItemFactory == null)
        {
            Debug.LogError($"[ItemPicker] 参照が未設定です（bottle/bottleItemFactory）: {name}");
            return;
        }

        // 瓶の口の上の落下開始位置から生成して入れる。
        Vector3 dropPos = bottle.GetDropPosition();
        BottleItemCore core = bottleItemFactory.Create(data, dropPos, Quaternion.identity, bottle.transform);
        if (core != null)
        {
            core.Initialize(data);
            bottle.Register(core);
        }
    }

    // こぼれたアイテムをプレイヤー周囲にMapItemCoreとして戻す（再取得可能）。
    private void OnSpilled(ItemData data)
    {
        if (data == null || mapItemFactory == null) return;

        Vector2 r = Random.insideUnitCircle * spillScatterRadius;
        Vector3 pos = transform.position + new Vector3(r.x, spillSpawnHeight, r.y);

        MapItemCore core = mapItemFactory.Create(data, pos);
        if (core != null) core.Initialize(data);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
