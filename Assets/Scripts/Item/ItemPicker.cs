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
    [SerializeField] private MerchantUIController merchantUI; // 商人に近づいてEで開く売買UI

    [Header("拾う検知")]
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private Key pickupKey = Key.E;
    [SerializeField] private float corpseCloseRange = 3f; // 開いている死体からこれ以上離れたら自動で閉じる（pickupRangeより大きめ＝ちらつき防止）

    [Header("こぼれをマップに戻すときの散らばり")]
    [SerializeField] private float spillScatterRadius = 1f;
    [SerializeField] private float spillSpawnHeight = 1f; // プレイヤー頭上から落とす高さ

    private MapItemCore nearest;
    private Corpse nearestCorpse; // 最寄りの死体（Eで開いて漁る対象）
    private Merchant nearestMerchant; // 最寄りの商人（Eで話しかける対象）
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

        // 開いている死体から離れたら自動で閉じる（自分の瓶は対象外）。
        AutoCloseDistantCorpse();

        var kb = Keyboard.current;
        if (kb != null && kb[pickupKey].wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    // 開いている対象が死体で、一定距離(corpseCloseRange)以上離れたら瓶UIを閉じる。
    private void AutoCloseDistantCorpse()
    {
        if (bottleUI == null || !bottleUI.IsOpen) return;
        var holder = bottleUI.CurrentHolder;
        if (holder == null) return;
        if (holder.GetComponent<Corpse>() == null) return; // 死体でなければ（自分の瓶など）対象外
        float distSq = (holder.transform.position - transform.position).sqrMagnitude;
        if (distSq > corpseCloseRange * corpseCloseRange) bottleUI.CloseBottle();
    }

    // 周囲を3D検知し、タグ"Item"の最寄りMapItemCoreと、タグ"Corpse"の最寄りCorpseを把握する。
    private void RefreshNearest()
    {
        nearest = null;
        nearestCorpse = null;
        nearestMerchant = null;
        float bestItem = float.MaxValue;
        float bestCorpse = float.MaxValue;
        float bestMerchant = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(transform.position, pickupRange, hits);
        for (int i = 0; i < count; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            if (col.CompareTag("Item"))
            {
                var item = col.GetComponentInParent<MapItemCore>();
                if (item == null) continue;
                float d = (item.transform.position - transform.position).sqrMagnitude;
                if (d < bestItem) { bestItem = d; nearest = item; }
            }
            else if (col.CompareTag("Corpse"))
            {
                var corpse = col.GetComponentInParent<Corpse>();
                if (corpse == null) continue;
                float d = (corpse.transform.position - transform.position).sqrMagnitude;
                if (d < bestCorpse) { bestCorpse = d; nearestCorpse = corpse; }
            }
            else if (col.CompareTag("Citizen"))
            {
                var merchant = col.GetComponentInParent<Merchant>();
                if (merchant == null) continue; // 商人でない市民（にぎやかし）は対象外
                float d = (merchant.transform.position - transform.position).sqrMagnitude;
                if (d < bestMerchant) { bestMerchant = d; nearestMerchant = merchant; }
            }
        }
    }

    // E押下時の処理。
    //   ・瓶UIを開いていない時：最寄りが死体なら開く、アイテムなら拾う（近い方を優先）。
    //   ・死体を開いている時：移動で最寄りが別の死体に変わっていたら、その死体に開き直す。
    private void TryInteract()
    {
        // 商人UIが開いていればEで閉じる。
        if (merchantUI != null && merchantUI.IsOpen) { merchantUI.Close(); return; }

        // 既に瓶UIが開いている場合の扱い。
        if (bottleUI != null && bottleUI.IsOpen)
        {
            var openHolder = bottleUI.CurrentHolder;
            bool viewingCorpse = openHolder != null && openHolder.GetComponent<Corpse>() != null;
            if (viewingCorpse)
            {
                // 死体を漁い中：最寄りが別の死体なら開き直す（拾いはしない）。
                if (nearestCorpse != null && nearestCorpse.Holder != openHolder)
                {
                    bottleUI.CloseBottle();
                    bottleUI.OpenBottle(nearestCorpse.Holder);
                }
                return;
            }
            // 自分の瓶（装備画面など）を開いている時：マップのアイテムは拾える（右の瓶に入る）。死体は無視。
            if (nearest != null) TryPickup();
            return;
        }

        // 瓶を開いていない時：近い方を開く/拾う/話しかける。
        float itemDist = nearest != null ? (nearest.transform.position - transform.position).sqrMagnitude : float.MaxValue;
        float corpseDist = nearestCorpse != null ? (nearestCorpse.transform.position - transform.position).sqrMagnitude : float.MaxValue;
        float merchantDist = nearestMerchant != null ? (nearestMerchant.transform.position - transform.position).sqrMagnitude : float.MaxValue;

        // 最寄りが商人なら売買UIを開く。
        if (nearestMerchant != null && merchantDist <= itemDist && merchantDist <= corpseDist)
        {
            if (merchantUI != null) merchantUI.Open(nearestMerchant);
            return;
        }
        if (nearestCorpse != null && corpseDist <= itemDist)
        {
            if (bottleUI != null) bottleUI.OpenBottle(nearestCorpse.Holder);
            return;
        }
        if (nearest != null) TryPickup();
    }

    // 最寄りを拾う：瓶を開く→瓶に生成して入れる→マップの実体を破棄。
    private void TryPickup()
    {
        if (nearest == null) return;

        ItemData data = nearest.Data;
        if (data == null) return;

        // 瓶UIを自動で開く（収納の様子を見せる）。閉じていて新規に開く時は中央に（装備画面の右寄せを引きずらない）。
        if (bottleUI != null)
        {
            if (!bottleUI.IsOpen) bottleUI.SetRightHalf(false);
            bottleUI.OpenBottle();
        }

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
        DropToMap(data);
    }

    // アイテムをプレイヤー周囲のマップに落とす（こぼれ／手が埋まって取り出しを受け取れない時など）。
    public void DropToMap(ItemData data)
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
