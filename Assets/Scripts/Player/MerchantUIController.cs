// 保存先: Assets/Scripts/Player/MerchantUIController.cs
// 商人の売買UIのハブ（開閉・リスト/詳細ビュー・受け皿の返金）。
//   ItemPickerが商人に近づいてEで Open(merchant) する。開いている間だけパネル表示。
//
//   コード整理（2026-06-12）で4コンポーネント＋計算ヘルパーに分割した。各役割：
//     ・MerchantUIController（このファイル）＝開閉・足止め・距離自動クローズ・スロット生成・
//       3Dモデル配置・受け皿⇔瓶の資産移動（返金3経路・瓶投入）・確定/キャンセル受付
//     ・MerchantCeremonyDirector＝取引の儀式（購入/売却の演出コルーチン・手・中断精算）。
//       Begin*/AbortAndSettleで起動・中断し、Started/Finishedイベントでこちらが追随する
//     ・MerchantEmotionView＝顔イラスト＋セリフ（Bind/Show/Unbind）
//     ・MerchantWalletView＝所持金の集計表示（瓶のOnItemsChanged購読）
//     ・MerchantTrade（静的）＝支払いカウント・査定・買い取り可否の純ロジック
//
//   取引フロー（段階3-4/3-5）：
//     ・Open直後＝ListView：在庫スロットを動的生成し、行クリックで payingEntry を確定し DetailView へ。
//     ・受け皿の中身変化（OnItemsChanged）で支払い/査定を再計算し、確定ボタンの interactable を更新。
//     ・確定ボタン → MerchantCeremonyDirector の儀式へ。成立後はListViewへ復帰。
//     ・売却：ListView中に瓶から買い取り対象を取り出すと受け皿に入り売却モード（査定表示）。
//     ・キャンセル：受け皿の Items を瓶へ即時投入し直して破棄、ListViewへ復帰。
//     ・Close（取引キャンセル）：受け皿の中身は PendingItems 経由で返金。
//       ※ リアルタイム復活は直後の bottleUI.CloseBottle() の物理停止と競合する事故があるため使わない。
//
//   ※ 横断参照（PlayerHandStateが呼ぶ）のため Instance を持つ（BottleUIControllerと同じ流儀）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MerchantUIController : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;
    [SerializeField] private BottleUIController bottleUI;       // 商人UIと一緒に開く瓶（右半分で漁れる）

    [Header("分離コンポーネント（同じMerchantUISystem内の兄弟）")]
    [SerializeField] private MerchantCeremonyDirector ceremony; // 取引の儀式（演出・中断精算）
    [SerializeField] private MerchantEmotionView emotionView;   // 顔＋セリフ
    [SerializeField] private MerchantWalletView walletView;     // 所持金表示

    [Header("在庫スロット（動的生成）")]
    [SerializeField] private MerchantStockSlot slotPrefab;     // 在庫1件ぶんのスロットprefab
    [SerializeField] private RectTransform slotContainer;      // スロットを並べる親（VerticalLayoutGroup想定）

    [Header("3Dモデル表示（装備UIと同じ方式）")]
    [SerializeField] private MerchantDisplay merchantDisplay;  // 裏空間（在庫品の3Dモデルを並べる）
    [SerializeField] private Camera merchantCamera;            // 裏空間を撮る専用カメラ（開いている間だけ有効）
    [SerializeField] private RectTransform rawImageRect;       // モデルを映すRawImageの矩形（枠位置→モデル位置の変換基準）
    [SerializeField] private Camera uiCamera;                  // Canvasのカメラ（Screen Space Overlayならnullのまま）
    [SerializeField] private float depth = 10f;                // 商人カメラからモデルを置く奥行き
    [SerializeField] private float listViewSize = 3f;          // ListView 時の商品モデルの最大辺
    [SerializeField] private float priceViewSize = 1.5f;       // ListView 時の支払いアイテムモデルの最大辺（商品と別管理）
    [SerializeField] private float detailViewSize = 10f;       // DetailView 時の大表示モデルの最大辺

    [Header("リアルタイム瓶投入（購入成立時に即落下させる）")]
    [SerializeField] private Bottle playerBottle;              // 購入成立時の生成先（瓶の口の上から落とす）
    [SerializeField] private BottleItemFactory bottleItemFactory; // 瓶内アイテム生成役（受け皿への投入にも流用）
    [SerializeField] private InventoryHolder playerHolder;     // PendingItems返金先のフォールバック（ActivePlayer未設定時）

    [Header("物理受け皿（GDD準拠の物々交換UI・段階3-3）")]
    [SerializeField] private Bottle tray;                      // 商人の受け皿（Bottleの汎用物理空間を流用）
    [SerializeField] private Camera trayCamera;                // 受け皿を撮る専用カメラ（開いている間だけ有効）

    [Header("リスト/詳細 切替（段階3-4）")]
    [SerializeField] private GameObject listGroup;             // ListView 親（SlotContainer等を含む）
    [SerializeField] private GameObject detailGroup;           // DetailView 親（商品名・大表示・受け皿・購入/キャンセル）
    [SerializeField] private RectTransform detailProductFrame; // DetailView の商品大表示枠
    [SerializeField] private Text detailProductLabel;          // DetailView の商品名表示
    [SerializeField] private Button confirmButton;             // 「購入」ボタン (priceCount 達成で interactable)
    [SerializeField] private Button cancelButton;              // 「キャンセル」ボタン (受け皿の中身を瓶へ戻して ListView へ)

    [Header("自動クローズ")]
    [SerializeField] private Transform player;                  // プレイヤー本体（距離計測用）
    [SerializeField] private float autoCloseRange = 5f;         // 商人からこの距離を超えたら自動で閉じる（取引キャンセル）

    private bool open;
    private Wander heldWander; // 売買中に足止めしている商人（Close時に必ず解除する）
    private readonly List<MerchantStockSlot> slots = new List<MerchantStockSlot>();
    private MerchantStockEntry payingEntry; // 「買」を押して受付中のスロット
    private int paidCount;                  // 受付中スロットの受け皿内 priceItem 個数（再計算結果のキャッシュ）

    // 売却モード（買い取り・段階3-5）。ListView中に瓶から買い取り対象を取り出すと受け皿に入り selling=true。
    private bool selling;
    private Text confirmLabel;              // 確定ボタンの文字（購入/売る を切り替える）
    private string confirmDefaultText;      // prefab上の元の文字（＝購入）

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
        if (ceremony != null)
        {
            ceremony.Started -= OnCeremonyStarted;
            ceremony.Finished -= OnCeremonyFinished;
        }
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (merchantCamera != null) merchantCamera.enabled = false;
        if (trayCamera != null) trayCamera.enabled = false;
        // 受け皿の物理壁・ゾーンを組む（data は tray の SerializeField から読む。InventorySystem流儀）。
        if (tray != null) tray.Build(null);
        // 確定/キャンセル のハンドラ登録（重複防止のため一度だけ）。
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        // 確定ボタンの文字（売却モードで「売る」に変えるためキャッシュ）。
        if (confirmButton != null) confirmLabel = confirmButton.GetComponentInChildren<Text>(true);
        if (confirmLabel != null) confirmDefaultText = confirmLabel.text;
        // 儀式の開始/完走に追随（ボタンロック・ListView復帰）。
        if (ceremony != null)
        {
            ceremony.Started += OnCeremonyStarted;
            ceremony.Finished += OnCeremonyFinished;
        }
        open = false;
    }

    // 商人UI中の状態監視：商人から離れたら自動でClose（取引キャンセル）。
    //   I/M/Cの開閉キーは各UI側（BottleUI/TabMenu）が押された時に
    //   こちらをCloseしてから自分を開く流儀にする（既存の相互閉じ流儀と同じ）。
    // 操作中プレイヤー（陣営選択後はActivePlayer＝人間/魔族。未設定時は従来のシーン参照にフォールバック）。
    private Transform PlayerTransform => ActivePlayer.Exists ? ActivePlayer.Transform : player;
    private InventoryHolder OwnHolder => ActivePlayer.Holder != null ? ActivePlayer.Holder : playerHolder;

    private void Update()
    {
        if (!open || Current == null) return;
        var pt = PlayerTransform;
        if (pt == null) return;
        float distSq = (Current.transform.position - pt.position).sqrMagnitude;
        if (distSq > autoCloseRange * autoCloseRange) Close();
    }

    // 商人に話しかけて売買UIを開く（ItemPickerから）。瓶も一緒に開く（プレイヤーが漁ってpriceItemを商人に渡せるよう）。
    public void Open(Merchant merchant)
    {
        if (merchant == null) return;
        // 統合メニュー（装備/進化タブ等）が開いていたら閉じてから（既存の相互閉じ流儀）。
        if (TabMenuController.Instance != null && TabMenuController.Instance.IsOpen)
            TabMenuController.Instance.CloseMenu();
        Current = merchant;
        open = true;
        payingEntry = null;
        paidCount = 0;
        // 売買の間、商人の徘徊を足止め（こちらを向いてもらう）。Closeで必ず解除する。
        heldWander = merchant.GetComponent<Wander>();
        if (heldWander != null) heldWander.SetHold(true, PlayerTransform);
        if (panel != null) panel.SetActive(true);
        if (merchantCamera != null) merchantCamera.enabled = true;
        if (trayCamera != null) trayCamera.enabled = true;

        // 顔＋挨拶。
        if (emotionView != null) emotionView.Bind(merchant);

        // 受け皿の中身変化を購読（priceItem 個数・査定を再計算するため）。
        if (tray != null) tray.OnItemsChanged += OnTrayChanged;
        // ウォレット表示を起動（プレイヤー瓶の中身変化を購読してリアルタイム更新）。
        if (walletView != null) walletView.SetActive(true);

        // 瓶UIも一緒に開く（装備UIと同じ流儀。右半分で漁れる＝取り出したpriceItemを受け皿へ流す）。
        if (bottleUI != null)
        {
            bottleUI.SetRightHalf(true);
            bottleUI.OpenBottle();
        }

        BuildSlots();
        ShowListView();
    }

    public void Close()
    {
        // 取引キャンセル：受け皿に残った Items を ItemData ごとに OwnHolder.PendingItems へ戻す。
        //   ※ 受け皿に直接生成（リアルタイム）すると、直後の bottleUI.CloseBottle() で物理が止まる前に
        //     生成したコインが口から飛び出してこぼれ判定（→マップへ）に流れる事故が起きる。
        //     PendingItems経由なら次回瓶を開いた時に確実に口から落ちる＝インベントリ管理下に残る。
        RefundTrayToPending();

        if (tray != null) tray.OnItemsChanged -= OnTrayChanged;
        if (walletView != null) walletView.SetActive(false);

        open = false;
        if (emotionView != null) emotionView.Unbind();

        // 取引の儀式の途中で閉じた場合の中断精算（返金/補填の方針はMerchantCeremonyDirector参照）。
        if (ceremony != null && ceremony.AbortAndSettle(OwnHolder))
            SetTradeButtons(true);
        selling = false;

        Current = null;
        payingEntry = null;
        paidCount = 0;
        if (heldWander != null) { heldWander.SetHold(false); heldWander = null; } // 足止め解除（自動Close経路も通る）
        if (panel != null) panel.SetActive(false);
        if (merchantCamera != null) merchantCamera.enabled = false;
        if (trayCamera != null) trayCamera.enabled = false;
        ClearSlots();
        if (merchantDisplay != null) merchantDisplay.Clear();
        if (bottleUI != null) bottleUI.CloseBottle(); // 一緒に閉じる
    }

    // ============ 受け皿⇔瓶の資産移動（返金・投入） ============

    // 受け皿の Items を ItemData ごとに PendingItems に戻して、物理実体を破棄する。
    //   Close() 専用：直後の bottleUI.CloseBottle() で物理が止まる前にコインが口から飛び出して
    //   こぼれ判定（→マップへ）に流れる事故を避けるため、リアルタイム復活は使わず PendingItems へ積む。
    private void RefundTrayToPending()
    {
        if (tray == null) return;
        var items = tray.Items;
        if (items == null || items.Count == 0) return;
        if (OwnHolder != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var core = items[i];
                if (core == null || core.Data == null) continue;
                OwnHolder.PendingItems.Add(core.Data);
            }
        }
        ClearTrayItems();
    }

    // 受け皿の Items を「瓶へ即時投入」で復元する。Cancelボタン／商品切替時に使う。
    //   瓶UIが開きっぱなしの想定なので DropItemToBottle の hot path で物理復活する。
    //   万一閉じていれば DropItemToBottle 内で PendingItems にフォールバックされる。
    private void RefundTrayToBottleLive()
    {
        if (tray == null) return;
        var snapshot = new List<BottleItemCore>(tray.Items);
        if (snapshot.Count == 0) return;
        tray.OnItemsChanged -= OnTrayChanged; // 一括巻き戻し中の余計な再計算を抑止
        for (int i = 0; i < snapshot.Count; i++)
        {
            var core = snapshot[i];
            if (core == null) continue;
            var data = core.Data;
            tray.Unregister(core);
            Destroy(core.gameObject);
            if (data != null) DropItemToBottle(data);
        }
        if (open) tray.OnItemsChanged += OnTrayChanged;
    }

    // 受け皿の物理実体をすべて破棄する。Bottle.Unregister は OnItemsChanged を発火するので、
    // 一旦購読を外してから一括破棄→再購読、で無駄な再計算を避ける。
    private void ClearTrayItems()
    {
        if (tray == null) return;
        var snapshot = new List<BottleItemCore>(tray.Items);
        tray.OnItemsChanged -= OnTrayChanged;
        for (int i = 0; i < snapshot.Count; i++)
        {
            var core = snapshot[i];
            if (core == null) continue;
            tray.Unregister(core);
            Destroy(core.gameObject);
        }
        if (open) tray.OnItemsChanged += OnTrayChanged;
    }

    // ItemDataを瓶に入れる共通処理（儀式＝MerchantCeremonyDirectorからも使う）。
    //   瓶UIが開いている：BottleItemFactoryで物理実体を口の上に生成→Register（リアルタイム落下・即見える）。
    //   開いていない：OwnHolder.PendingItemsに積む（次回開いた時に落ちる）。
    public void DropItemToBottle(ItemData data)
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
        if (OwnHolder != null) OwnHolder.PendingItems.Add(data);
    }

    // ============ スロット生成・3Dモデル配置 ============

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
            slot.Setup(entry, OnSlotClicked);
            slots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null) Destroy(slots[i].gameObject);
        slots.Clear();
    }

    // ListView の3Dモデル配置：在庫スロットの「品の枠」「価格の枠」両方にモデルを並べる。
    //   VerticalLayoutGroup配下にスロットを追加した直後は frame.position が未確定なので、
    //   ForceRebuildLayoutImmediateでレイアウトを即時確定させてから位置を読む。
    private void RefreshListDisplay()
    {
        if (merchantDisplay == null || merchantCamera == null || rawImageRect == null) return;

        merchantDisplay.ViewSize = listViewSize;
        if (slotContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer);

        var items = new List<ItemData>(slots.Count * 2);
        var positions = new List<Vector3>(slots.Count * 2);
        var sizes = new List<float>(slots.Count * 2); // 商品と支払いでサイズを分ける
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.Entry == null) continue;

            if (slot.ItemFrame != null && slot.Entry.item != null)
            {
                items.Add(slot.Entry.item);
                positions.Add(SlotWorldPosition(slot.ItemFrame));
                sizes.Add(listViewSize);
            }
            if (slot.PriceFrame != null && slot.Entry.priceItem != null)
            {
                items.Add(slot.Entry.priceItem);
                positions.Add(SlotWorldPosition(slot.PriceFrame));
                sizes.Add(priceViewSize);
            }
        }
        merchantDisplay.UpdateDisplay(items, positions, sizes);
    }

    // DetailView の3Dモデル配置：選んだ品 1個を detailProductFrame の位置に大表示する。
    //   DetailView では「枠の位置をカメラViewportにマップ」せず、カメラ中央 (vp 0.5, 0.5) にモデルを置く。
    //   DetailProductFrame は MerchantRT 全体を映すので、カメラ中央の品が枠の中央に大きく見える。
    private void RefreshDetailDisplay(MerchantStockEntry entry)
    {
        if (merchantDisplay == null || merchantCamera == null || rawImageRect == null) return;
        if (entry == null) { merchantDisplay.Clear(); return; }
        if (detailProductFrame == null || entry.item == null) { merchantDisplay.Clear(); return; }

        merchantDisplay.ViewSize = detailViewSize;
        var pos = merchantCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
        var items = new List<ItemData> { entry.item };
        var positions = new List<Vector3> { pos };
        merchantDisplay.UpdateDisplay(items, positions);
    }

    // スロット枠(UI)の中心位置 → 商人カメラ前のワールド位置に変換する（共通ヘルパー委譲）。
    private Vector3 SlotWorldPosition(RectTransform frame)
    {
        return UIModelProjection.FrameToWorld(frame, rawImageRect, uiCamera, merchantCamera, depth);
    }

    // ============ リスト/詳細/売却ビュー切替 ============

    // 行クリック：そのスロットを購入予定にして DetailView へ切替。
    //   在庫切れ／不正設定は無視。priceCount==0 でも DetailView に入る（「購入」ボタンが即押下可能）。
    //   既に別スロットが選ばれていた場合は受け皿の中身を巻き戻してから切り替える（誤クリック保護）。
    private void OnSlotClicked(MerchantStockEntry entry)
    {
        if (entry == null || entry.stock <= 0) return;
        if (entry.priceCount < 0) return;
        if (entry.priceCount > 0 && entry.priceItem == null) return;

        // 別の商品を選び直す場合は、受け皿の中身（前の支払い物）を瓶へ戻してから切り替え。
        //   瓶UIは開きっぱなしなのでリアルタイム復活でOK。
        if (payingEntry != null && payingEntry != entry)
        {
            RefundTrayToBottleLive();
        }

        payingEntry = entry;
        paidCount = 0;
        ShowDetailView(entry);
        RecalculatePayment();
    }

    private void ShowListView()
    {
        selling = false;
        if (confirmLabel != null) confirmLabel.text = confirmDefaultText; // 「購入」へ戻す
        if (listGroup != null) listGroup.SetActive(true);
        if (detailGroup != null) detailGroup.SetActive(false);
        // 在庫数表示を最新化（購入成立で減った Entry.stock を反映）。
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null) slots[i].Refresh();
        RefreshListDisplay();
    }

    private void ShowDetailView(MerchantStockEntry entry)
    {
        if (listGroup != null) listGroup.SetActive(false);
        if (detailGroup != null) detailGroup.SetActive(true);
        if (detailProductLabel != null) detailProductLabel.text = entry.item != null ? entry.item.ItemName : "";
        RefreshDetailDisplay(entry);
        UpdateConfirmButton();
    }

    // 売却モードに入る：DetailViewの枠を「査定」表示として使う（商品リストは隠す）。
    //   大表示モデルは出さない（売却品の実体が受け皿に見えているため）。
    private void EnterSellView()
    {
        selling = true;
        payingEntry = null;
        if (listGroup != null) listGroup.SetActive(false);
        if (detailGroup != null) detailGroup.SetActive(true);
        if (merchantDisplay != null) merchantDisplay.Clear();
        if (confirmLabel != null) confirmLabel.text = "売る";
        RecalculateSell();
    }

    // ============ 受け皿の受付・再計算 ============

    // PlayerHandState.OnItemTakenOut の冒頭から呼ばれる。
    //   購入受付中（payingEntry あり）：priceItem と一致したら受け皿へ（支払い）。
    //   ListView中（payingEntry なし）：商人の買い取り対象なら受け皿へ＝売却モードに入る（段階3-5）。
    //   戻り値：受け皿に投入したなら true（PlayerHandStateは以降の処理をスキップ）。
    public bool TryDropIntoTray(ItemData data)
    {
        if (!open || data == null) return false;
        if (tray == null || bottleItemFactory == null) return false;
        if (ceremony != null && ceremony.IsRunning) return false; // 儀式中は受け取らない

        if (payingEntry != null)
        {
            // 購入の支払い受付。
            if (payingEntry.priceItem != data) return false; // 別アイテムは受け取らない（手に渡す）
        }
        else
        {
            // 売却の受付（査定）。買い取れない品は受け皿に乗せず、即瓶へ返して断る（可否はMerchantTrade）。
            //   手に持たせず瓶へ直接戻す（瓶UIは開きっぱなしなので口から落ちて見える）。
            if (!MerchantTrade.CanSell(tray, Current, data))
            {
                DropItemToBottle(data);
                if (Current != null && emotionView != null)
                    emotionView.Show(Current.PortraitSad, Current.LineRefuse); // 困り顔（それは買い取れません！）
                return true; // 返却済み＝PlayerHandState側の通常経路（手に持つ）はスキップ
            }
        }

        Vector3 dropPos = tray.GetRandomDropPosition(); // 中央積み上げ回避＝横位置を散らして落とす
        BottleItemCore core = bottleItemFactory.Create(data, dropPos, Quaternion.identity, tray.transform);
        if (core == null) return false;
        core.Initialize(data);
        if (payingEntry == null && !selling) EnterSellView(); // 最初の1個で売却モードへ（先に切替＝Registerの再計算が売却側を通る）
        tray.Register(core); // OnItemsChanged → OnTrayChanged → 再計算（購入/売却で分岐）
        return true;
    }

    private void OnTrayChanged()
    {
        // 儀式中の受け皿変動（コイン回収・対価の設置等）では再計算しない（ボタンを再点灯させない）。
        if (ceremony != null && ceremony.IsRunning) return;
        if (selling) RecalculateSell();
        else RecalculatePayment();
    }

    // 受け皿の priceItem 個数を数えて paidCount に反映（計算はMerchantTrade）。
    //   段階3-4：自動成立はやめ、確定ボタンの interactable を更新するだけ。
    private void RecalculatePayment()
    {
        if (payingEntry == null || tray == null) return;
        paidCount = MerchantTrade.CountInTray(tray, payingEntry.priceItem);
        UpdateConfirmButton();
        if (walletView != null) walletView.Refresh();
    }

    // 売却の査定：受け皿の中身を買い取りリストで評価し、対価合計をラベルに出す（計算はMerchantTrade）。
    private void RecalculateSell()
    {
        if (!selling) return;
        var appraisal = MerchantTrade.Appraise(tray, Current);

        if (detailProductLabel != null)
        {
            if (appraisal.sellableCount <= 0) detailProductLabel.text = "買い取れる物がありません";
            else
            {
                var parts = new List<string>();
                foreach (var pair in appraisal.payoutTotal)
                    parts.Add($"{pair.Key.ItemName}×{pair.Value}");
                detailProductLabel.text = $"買い取り {appraisal.sellableCount}個 → {string.Join("＋", parts)}";
            }
        }
        if (confirmButton != null) confirmButton.interactable = appraisal.sellableCount > 0;
        if (walletView != null) walletView.Refresh();
    }

    // 確定ボタンの interactable は「priceCount==0（無料）」 または「paidCount >= priceCount」 で true。
    private void UpdateConfirmButton()
    {
        if (confirmButton == null) return;
        bool canConfirm = payingEntry != null
            && (payingEntry.priceCount == 0 || paidCount >= payingEntry.priceCount);
        confirmButton.interactable = canConfirm;
    }

    // ============ 確定・キャンセル（儀式はMerchantCeremonyDirectorへ委譲） ============

    // 確定ボタン押下＝取引開始。購入なら「手がコイン回収→商品を置く」、売却なら「手が売却品を回収→対価を置く」。
    private void OnConfirmClicked()
    {
        if (ceremony == null || ceremony.IsRunning) return; // 儀式中の多重起動防止
        if (selling)
        {
            ceremony.BeginSell(Current);
            return;
        }
        if (payingEntry == null) return;
        if (payingEntry.priceCount > 0 && paidCount < payingEntry.priceCount) return; // 念のため二重ガード
        var entry = payingEntry;
        payingEntry = null; // 以降の再計算・別商品クリックの巻き戻し対象から外す
        ceremony.BeginPurchase(entry, Current);
    }

    // 「キャンセル」ボタン押下：受け皿の中身を瓶へリアルタイム復元して ListView へ復帰。
    //   Close()と違って瓶UIは開きっぱなしなので、コインが口から物理復活して見える（UX的に自然）。
    //   売却モード中も同じ（査定中の品が瓶へ戻る）。
    private void OnCancelClicked()
    {
        RefundTrayToBottleLive();
        payingEntry = null;
        paidCount = 0;
        if (Current != null && emotionView != null)
            emotionView.Show(Current.PortraitSad, Current.LineCancel); // 悲しい顔（売れなかった…）
        ShowListView();
    }

    // 儀式開始：購入/キャンセルを押せなくする（多重実行・途中キャンセルの事故防止）。
    private void OnCeremonyStarted() => SetTradeButtons(false);

    // 儀式完走：ボタンを戻してListViewへ（閉じていればパネル復帰はしない）。
    private void OnCeremonyFinished()
    {
        SetTradeButtons(true);
        paidCount = 0;
        if (open) ShowListView();
    }

    private void SetTradeButtons(bool value)
    {
        if (confirmButton != null) confirmButton.interactable = value;
        if (cancelButton != null) cancelButton.interactable = value;
    }
}
