// 保存先: Assets/Scripts/Player/MerchantUIController.cs
// 商人の売買UIの制御役。
//   ItemPickerが商人に近づいてEで Open(merchant) する。開いている間だけパネル表示。
//   開く時：Merchant.Stockぶんスロットを動的生成（MinimapController流儀）し、各スロット枠の位置から
//   MerchantDisplayへワールド位置を逆算して渡し、品の3Dモデル(ItemData.Prefab)を裏空間に並べる。
//   表示は装備UIと同じグラフィカル方式（瓶・装備と同じカメラ→RenderTexture→RawImage）。
//
//   段階3-2：物理支払い（受け皿方式）
//     ・Open時に瓶UIも同時に開く（装備UI流儀）。プレイヤーは瓶からpriceItemを取り出して商人に渡す。
//     ・「買」を押すと、そのスロットを「受付中」に設定（payingEntry）。
//     ・PlayerHandState.OnItemTakenOut の冒頭で TryConsumePayment が呼ばれる。
//       受付中で priceItem と一致したら paidCount++（手には持たせない）。
//     ・paidCount == priceCount で購入成立：在庫減＋playerHolder.PendingItemsに買った品を追加
//       （次回瓶を開いた時に口から落ちる＝兵士の初期インベントリと同じ仕組み）。
//   ※ 横断参照（PlayerHandStateが呼ぶ）のため Instance を持つ（BottleUIControllerと同じ流儀）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MerchantUIController : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;
    [SerializeField] private BottleUIController bottleUI;       // 商人UIと一緒に開く瓶（右半分で漁れる）

    [Header("在庫スロット（動的生成）")]
    [SerializeField] private MerchantStockSlot slotPrefab;     // 在庫1件ぶんのスロットprefab
    [SerializeField] private RectTransform slotContainer;      // スロットを並べる親（VerticalLayoutGroup想定）

    [Header("3Dモデル表示（装備UIと同じ方式）")]
    [SerializeField] private MerchantDisplay merchantDisplay;  // 裏空間（在庫品の3Dモデルを並べる）
    [SerializeField] private Camera merchantCamera;            // 裏空間を撮る専用カメラ（開いている間だけ有効）
    [SerializeField] private RectTransform rawImageRect;       // モデルを映すRawImageの矩形（枠位置→モデル位置の変換基準）
    [SerializeField] private Camera uiCamera;                  // Canvasのカメラ（Screen Space Overlayならnullのまま）
    [SerializeField] private float depth = 10f;                // 商人カメラからモデルを置く奥行き

    [Header("所持金表示")]
    [SerializeField] private Text walletLabel;                 // プレイヤーの所持金（瓶内コインのcurrencyValue合計）
    [SerializeField] private InventoryHolder playerHolder;     // 所持金集計用（瓶の中身記録Recordsから走査・閉じている時も読める）

    [Header("リアルタイム瓶投入（購入成立時に即落下させる）")]
    [SerializeField] private Bottle playerBottle;              // 購入成立時の生成先（瓶の口の上から落とす）
    [SerializeField] private BottleItemFactory bottleItemFactory; // 瓶内アイテム生成役

    [Header("自動クローズ")]
    [SerializeField] private Transform player;                  // プレイヤー本体（距離計測用）
    [SerializeField] private float autoCloseRange = 5f;         // 商人からこの距離を超えたら自動で閉じる（取引キャンセル）

    private bool open;
    private readonly List<MerchantStockSlot> slots = new List<MerchantStockSlot>();
    private MerchantStockEntry payingEntry; // 「買」を押して受付中のスロット
    private int paidCount;                  // 受付中スロットへの累計支払い個数

    public bool IsOpen => open;
    public Merchant Current { get; private set; }

    // 横断参照（PlayerHandStateが「取り出されたアイテムを商人に渡す」処理のために呼ぶ）。シーンに1個。
    public static MerchantUIController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[MerchantUIController] 既に存在するため重複を破棄: {name}");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (merchantCamera != null) merchantCamera.enabled = false;
        open = false;
    }

    // 商人UI中の状態監視：商人から離れたら自動でClose（取引キャンセル）。
    //   I/M/Cの開閉キーは各UI側（BottleUI/Minimap/EquipmentUI）が押された時に
    //   こちらをCloseしてから自分を開く流儀にする（既存の相互閉じ流儀と同じ）。
    private void Update()
    {
        if (!open || Current == null) return;
        if (player == null) return;
        float distSq = (Current.transform.position - player.position).sqrMagnitude;
        if (distSq > autoCloseRange * autoCloseRange) Close();
    }

    // 商人に話しかけて売買UIを開く（ItemPickerから）。瓶も一緒に開く（プレイヤーが漁ってpriceItemを商人に渡せるよう）。
    public void Open(Merchant merchant)
    {
        if (merchant == null) return;
        Current = merchant;
        open = true;
        payingEntry = null;
        paidCount = 0;
        if (panel != null) panel.SetActive(true);
        if (merchantCamera != null) merchantCamera.enabled = true;

        // 瓶UIも一緒に開く（装備UIと同じ流儀。右半分で漁れる＝取り出したpriceItemを商人に渡す）。
        if (bottleUI != null)
        {
            bottleUI.SetRightHalf(true);
            bottleUI.OpenBottle();
        }

        BuildSlots();
        RefreshWallet();
        RefreshDisplay();
    }

    public void Close()
    {
        // 取引キャンセル：途中まで払ったpriceItemを瓶へ戻す（リアルタイム or PendingItems）。
        //   bottleUIをCloseするとBottleDraggerが無効化されて掴み中のアイテムは自動Releaseされ瓶に戻る。
        CancelTransactionRefund();

        open = false;
        Current = null;
        payingEntry = null;
        paidCount = 0;
        if (panel != null) panel.SetActive(false);
        if (merchantCamera != null) merchantCamera.enabled = false;
        ClearSlots();
        if (merchantDisplay != null) merchantDisplay.Clear();
        if (bottleUI != null) bottleUI.CloseBottle(); // 一緒に閉じる
    }

    // 取引キャンセル時の払い戻し：受付中なら paidCount 個のpriceItemをPendingItemsへ戻す。
    //   ※ ここで瓶に直接生成（リアルタイム）すると、直後の bottleUI.CloseBottle() で物理が止まる前に
    //     生成したコインが口から飛び出してこぼれ判定（→マップへ）に流れる事故が起きる。
    //     PendingItems経由なら次回瓶を開いた時に確実に口から落ちる＝インベントリ管理下に残る。
    private void CancelTransactionRefund()
    {
        if (payingEntry == null || paidCount <= 0 || payingEntry.priceItem == null) return;
        if (playerHolder == null) return;
        for (int i = 0; i < paidCount; i++)
        {
            playerHolder.PendingItems.Add(payingEntry.priceItem);
        }
    }

    // 在庫数ぶんスロットを生成し、それぞれにEntryを差し込む（MinimapControllerと同じ動的生成パターン）。
    private void BuildSlots()
    {
        ClearSlots();
        if (Current == null || slotPrefab == null || slotContainer == null) return;
        var stock = Current.Stock;
        if (stock == null) return;

        for (int i = 0; i < stock.Count; i++)
        {
            var entry = stock[i];
            if (entry == null) continue;
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.Setup(entry, OnBuyClicked);
            slots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null) Destroy(slots[i].gameObject);
        slots.Clear();
    }

    // 各スロットの「品の枠」「価格アイテムの枠」両方から、商人カメラ前のワールド位置を逆算して
    // モデルを並べる。在庫品と支払いアイテムを同じ仕組み（MerchantDisplay）で3D表示する。
    //   VerticalLayoutGroup配下にスロットを追加した直後は frame.position が未確定なので、
    //   ForceRebuildLayoutImmediateでレイアウトを即時確定させてから位置を読む（これをしないと
    //   モデルがスロットと全く違う位置に表示される）。
    private void RefreshDisplay()
    {
        if (merchantDisplay == null || merchantCamera == null || rawImageRect == null) return;

        // スロットを並べる親のレイアウトを強制再計算（frame.position確定のため）。
        if (slotContainer != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer);

        var items = new List<ItemData>(slots.Count * 2);
        var positions = new List<Vector3>(slots.Count * 2);
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.Entry == null) continue;

            // 売る品の枠にItemDataのモデル。
            if (slot.ItemFrame != null && slot.Entry.item != null)
            {
                items.Add(slot.Entry.item);
                positions.Add(SlotWorldPosition(slot.ItemFrame));
            }
            // 支払いアイテムの枠にpriceItemのモデル。
            if (slot.PriceFrame != null && slot.Entry.priceItem != null)
            {
                items.Add(slot.Entry.priceItem);
                positions.Add(SlotWorldPosition(slot.PriceFrame));
            }
        }
        merchantDisplay.UpdateDisplay(items, positions);
    }

    // スロット枠(UI)の中心位置 → 商人カメラ前のワールド位置に変換する（EquipmentUIControllerと同じ）。
    private Vector3 SlotWorldPosition(RectTransform frame)
    {
        Vector3 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, frame.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, screen, uiCamera, out Vector2 local);
        Rect rect = rawImageRect.rect;
        float vx = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float vy = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
        return merchantCamera.ViewportToWorldPoint(new Vector3(vx, vy, depth));
    }

    // 所持金＝瓶内アイテムのcurrencyValue合計（コインは1個ずつ1Gなど）。
    //   InventoryHolderのRecords（閉じている時の中身記録）＋PendingItems（まだ積んでない初期アイテム）から集計。
    //   Bottle.Itemsは「開いている時の物理実体」だけなので、商人UI（普通は瓶を閉じた状態で開く）には使えない。
    private void RefreshWallet()
    {
        if (walletLabel == null) return;
        int gold = 0;
        if (playerHolder != null)
        {
            var records = playerHolder.Records;
            for (int i = 0; i < records.Count; i++)
            {
                var d = records[i].data;
                if (d != null && d.CurrencyValue > 0) gold += d.CurrencyValue;
            }
            var pending = playerHolder.PendingItems;
            for (int i = 0; i < pending.Count; i++)
            {
                var d = pending[i];
                if (d != null && d.CurrencyValue > 0) gold += d.CurrencyValue;
            }
        }
        walletLabel.text = $"{gold}G";
    }

    // 「買」押下：そのスロットを受付中にする。在庫切れは無視。
    //   priceCount==0 は無料配布として即購入成立（priceItemは参照しない）。
    //   priceCount>0 のときは priceItem 必須（未設定なら無視）。
    //   既に別スロットが受付中なら切り替え（途中までの累計はリセット）。
    private void OnBuyClicked(MerchantStockEntry entry)
    {
        if (entry == null || entry.stock <= 0) return;
        if (entry.priceCount < 0) return;
        if (entry.priceCount > 0 && entry.priceItem == null) return;

        payingEntry = entry;
        paidCount = 0;

        if (entry.priceCount == 0)
        {
            CompletePurchase(); // 無料：即成立
            return;
        }
        RefreshSlotsView();
    }

    // PlayerHandState.OnItemTakenOut の冒頭から呼ばれる。
    //   受付中で priceItem と一致したら、商人に渡された扱い（手に持たせない）。
    //   累計が priceCount に達したら購入成立。
    //   戻り値：消費した（手に持たせない）なら true。
    public bool TryConsumePayment(ItemData data)
    {
        if (!open || payingEntry == null || data == null) return false;
        if (payingEntry.priceItem != data) return false; // 別のアイテムは横取りしない

        paidCount++;
        if (paidCount >= payingEntry.priceCount)
        {
            CompletePurchase();
        }
        else
        {
            RefreshSlotsView();
            RefreshWallet();
        }
        return true;
    }

    // 累計が priceCount に達したとき：在庫減・買った品を即座に瓶へ投入・状態リセット。
    //   瓶が開いていれば口の上から落とす（リアルタイム）、開いていなければPendingItems経由（次回落下）。
    private void CompletePurchase()
    {
        var entry = payingEntry;
        if (Current != null) Current.TryConsumeStock(entry);
        if (entry.item != null) DropItemToBottle(entry.item);

        payingEntry = null;
        paidCount = 0;

        RefreshSlotsView();
        RefreshWallet();
        Debug.Log($"[MerchantUI] 購入成立: {entry.item?.ItemName}（残在庫 {entry.stock}）");
    }

    // ItemDataを瓶に入れる共通処理。
    //   瓶UIが開いている：BottleItemFactoryで物理実体を口の上に生成→Register（リアルタイム落下・即見える）。
    //   開いていない：playerHolder.PendingItemsに積む（次回開いた時に落ちる）。
    //   商人UI中は瓶も開きっぱなしの設計だが、片付け中の中間状態にも対応するため両対応。
    private void DropItemToBottle(ItemData data)
    {
        if (data == null) return;
        bool live = playerBottle != null && bottleItemFactory != null && bottleUI != null && bottleUI.IsOpen;
        if (live)
        {
            Vector3 dropPos = playerBottle.GetDropPosition();
            BottleItemCore core = bottleItemFactory.Create(data, dropPos, Quaternion.identity, playerBottle.transform);
            if (core != null)
            {
                core.Initialize(data);
                playerBottle.Register(core);
                return;
            }
        }
        // 投入失敗 or 瓶閉じている：次回開いた時に落ちる仕組みに乗せる。
        if (playerHolder != null) playerHolder.PendingItems.Add(data);
    }

    // 受付中スロットだけ「累計/必要」表示にし、他は通常表示に戻す。在庫数も最新化。
    private void RefreshSlotsView()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;
            slot.Refresh();
            slot.SetPayingState(slot.Entry == payingEntry, paidCount);
        }
    }
}
