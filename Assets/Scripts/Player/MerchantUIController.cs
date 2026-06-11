// 保存先: Assets/Scripts/Player/MerchantUIController.cs
// 商人の売買UIの制御役。
//   ItemPickerが商人に近づいてEで Open(merchant) する。開いている間だけパネル表示。
//
//   段階3-4：ListView / DetailView 切替 + 確定ボタン方式
//     ・Open直後＝ListView：在庫スロットを動的生成し、行クリック（旧Buyボタン領域）で商品を選択。
//     ・行クリックで payingEntry を確定し DetailView に切替：
//         - 商品リストを非表示にし、選んだ商品の3Dモデルを大表示（MerchantDisplayをUpdateで切替）。
//         - その下に「購入」ボタン（priceCount 達成までは disabled）と「キャンセル」ボタン、受け皿を出す。
//         - priceCount==0 は無料配布なので「購入」直後の確定で即成立。
//     ・受け皿の中身（priceItem の個数）を OnItemsChanged 購読で再計算し、確定ボタンの interactable を更新。
//       ※ 段階3-3まで「達成で自動成立」だったが、3-4から「確定ボタン押下まで成立しない」に変更。
//     ・「キャンセル」：受け皿の Items を瓶へ即時投入し直して破棄、ListViewへ復帰。瓶UIが開きっぱなしなので
//       コインは口から物理復活する（リアルタイム）。瓶が閉じていれば PendingItems 経由にフォールバック。
//       ※ Close()は直後にbottleUI.CloseBottle()で物理停止と競合する事故があるため、別経路(PendingItemsのみ)を使う。
//     ・購入成立後：受け皿から priceCount 個の priceItem を破棄、在庫減、買った品を瓶へ投入、ListViewへ復帰。
//
//   ※ 横断参照（PlayerHandStateが呼ぶ）のため Instance を持つ（BottleUIControllerと同じ流儀）。
using System.Collections;
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
    [SerializeField] private float listViewSize = 3f;          // ListView 時の商品モデルの最大辺
    [SerializeField] private float priceViewSize = 1.5f;       // ListView 時の支払いアイテムモデルの最大辺（商品と別管理）
    [SerializeField] private float detailViewSize = 10f;       // DetailView 時の大表示モデルの最大辺

    [Header("商人の顔（左カラム）")]
    [SerializeField] private RawImage merchantPortrait;       // MerchantData の顔を表示（未設定の商人なら枠ごと非表示）
    [SerializeField] private Text speechLabel;                // 左下のセリフ（いらっしゃいませ等。未設定可）
    [SerializeField] private float emotionDuration = 2f;      // 喜び/悲しみ顔・セリフを見せる秒数（その後通常へ）

    [Header("所持金表示")]
    [SerializeField] private Text walletLabel;                 // プレイヤーの所持金（瓶内コインのcurrencyValue合計）
    [SerializeField] private InventoryHolder playerHolder;     // 所持金集計用（瓶の中身記録Recordsから走査・閉じている時も読める）

    [Header("リアルタイム瓶投入（購入成立時に即落下させる）")]
    [SerializeField] private Bottle playerBottle;              // 購入成立時の生成先（瓶の口の上から落とす）
    [SerializeField] private BottleItemFactory bottleItemFactory; // 瓶内アイテム生成役（受け皿への投入にも流用）

    [Header("物理受け皿（GDD準拠の物々交換UI・段階3-3）")]
    [SerializeField] private Bottle tray;                      // 商人の受け皿（Bottleの汎用物理空間を流用）
    [SerializeField] private Camera trayCamera;                // 受け皿を撮る専用カメラ（開いている間だけ有効）

    [Header("取引の儀式（購入→手がコイン回収→商品を置く→クリックで瓶へ）")]
    [SerializeField] private MerchantTrayPicker trayPicker;   // 受け皿に置かれた商品のクリック判定役
    [SerializeField] private Transform handVisual;            // 商人の手（プレースホルダ可。未設定なら手の演出を飛ばす）
    [SerializeField] private float ceremonyStepInterval = 0.25f; // コイン1枚回収ごとの間（秒）
    [SerializeField] private float handMoveDuration = 0.25f;     // 手の移動1回あたりの秒数
    [SerializeField] private float payoutScatter = 1.2f;         // 売却対価を置く時の中央からの散らし幅（受け皿の横方向）
    [SerializeField] private float payoutDropInterval = 0.06f;   // 対価1個ごとの間（小刻み＝バッとまとめて置く感じ）

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
    private readonly List<MerchantStockSlot> slots = new List<MerchantStockSlot>();
    private MerchantStockEntry payingEntry; // 「買」を押して受付中のスロット
    private int paidCount;                  // 受付中スロットの受け皿内 priceItem 個数（再計算結果のキャッシュ）
    private Coroutine emotionRoutine;       // 表情の一時切替（喜び/悲しみ→通常へ戻す）
    private Coroutine ceremonyRoutine;      // 取引の儀式（実行中は購入/キャンセル不可）
    private MerchantStockEntry ceremonyEntry; // 儀式中のエントリ（中断補填用）
    private bool ceremonyPaid;              // コイン回収完了～商品設置前の区間か（中断時は商品を補填＝購入成立扱い）
    private readonly List<ItemData> ceremonyConsumedCoins = new List<ItemData>(); // 回収済みコイン（回収途中の中断＝キャンセル扱いで全額返金）

    // 売却モード（買い取り・段階3-5）。ListView中に瓶から買い取り対象を取り出すと受け皿に入り selling=true。
    //   確定で逆向きの儀式（手が売却品を回収→対価を受け皿に置く→クリックで瓶へ）。
    private bool selling;
    private readonly List<ItemData> ceremonyPayout = new List<ItemData>(); // 売却の未払い対価（支払い設置前の中断＝成立扱いで補填）
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
        open = false;
    }

    // 商人UI中の状態監視：商人から離れたら自動でClose（取引キャンセル）。
    //   I/M/Cの開閉キーは各UI側（BottleUI/Minimap/EquipmentUI）が押された時に
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
        // 進化画面（魔族のC画面）が開いていたら閉じてから（既存の相互閉じ流儀）。
        if (EvolutionUIController.Instance != null && EvolutionUIController.Instance.IsOpen)
            EvolutionUIController.Instance.Close();
        Current = merchant;
        open = true;
        payingEntry = null;
        paidCount = 0;
        if (panel != null) panel.SetActive(true);
        if (merchantCamera != null) merchantCamera.enabled = true;
        if (trayCamera != null) trayCamera.enabled = true;

        // 商人の顔（SOで商人ごとに設定。未設定なら枠ごと出さない）。
        if (merchantPortrait != null)
        {
            merchantPortrait.texture = merchant.PortraitNormal;
            merchantPortrait.gameObject.SetActive(merchant.PortraitNormal != null);
        }
        // セリフ（挨拶）。
        if (speechLabel != null)
        {
            speechLabel.text = merchant.LineGreeting;
            speechLabel.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(merchant.LineGreeting));
        }

        // 受け皿の中身変化を購読（priceItem 個数を再計算するため）。
        if (tray != null) tray.OnItemsChanged += OnTrayChanged;
        // プレイヤー瓶の中身変化も購読（ウォレット表示をリアルタイム更新するため）。
        if (playerBottle != null) playerBottle.OnItemsChanged += OnPlayerBottleChanged;

        // 瓶UIも一緒に開く（装備UIと同じ流儀。右半分で漁れる＝取り出したpriceItemを受け皿へ流す）。
        if (bottleUI != null)
        {
            bottleUI.SetRightHalf(true);
            bottleUI.OpenBottle();
        }

        BuildSlots();
        RefreshWallet();
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
        if (playerBottle != null) playerBottle.OnItemsChanged -= OnPlayerBottleChanged;

        open = false;
        if (emotionRoutine != null) { StopCoroutine(emotionRoutine); emotionRoutine = null; }

        // 取引の儀式の途中で閉じた場合の後始末。
        //   ・コイン回収の途中で閉じた → キャンセル扱い：回収済みコインを全額PendingItemsへ返金（在庫はまだ減っていない）。
        //   ・全額回収後〜商品設置前に閉じた → 購入成立扱い：商品をPendingItemsで補填。
        //   ・商品設置後なら受け皿に実体があるので、直後の RefundTrayToPending が商品ごと瓶へ戻す（=入手済み扱い）。
        if (ceremonyRoutine != null)
        {
            StopCoroutine(ceremonyRoutine);
            ceremonyRoutine = null;
            if (trayPicker != null) trayPicker.StopWatch();
            HideHand();
            SetTradeButtons(true);
            if (OwnHolder != null)
            {
                if (ceremonyConsumedCoins.Count > 0)
                {
                    // 回収途中の中断＝不成立扱い：回収済みの支払いコイン（購入）／売却品（売却）を全て返す。
                    foreach (var coin in ceremonyConsumedCoins)
                        if (coin != null) OwnHolder.PendingItems.Add(coin);
                }
                else if (ceremonyPaid)
                {
                    // 回収完了〜対価設置前の中断＝成立扱い：購入なら商品、売却なら未設置の対価を補填。
                    if (ceremonyPayout.Count > 0)
                    {
                        foreach (var pay in ceremonyPayout)
                            if (pay != null) OwnHolder.PendingItems.Add(pay);
                    }
                    else if (ceremonyEntry != null && ceremonyEntry.item != null)
                    {
                        OwnHolder.PendingItems.Add(ceremonyEntry.item);
                    }
                }
            }
            ceremonyConsumedCoins.Clear();
            ceremonyPayout.Clear();
            ceremonyEntry = null;
            ceremonyPaid = false;
        }
        selling = false;

        Current = null;
        payingEntry = null;
        paidCount = 0;
        if (panel != null) panel.SetActive(false);
        if (merchantCamera != null) merchantCamera.enabled = false;
        if (trayCamera != null) trayCamera.enabled = false;
        ClearSlots();
        if (merchantDisplay != null) merchantDisplay.Clear();
        if (bottleUI != null) bottleUI.CloseBottle(); // 一緒に閉じる
    }

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
        if (slotContainer != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer);

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
    //   SetActive(true) 直後はまだ Canvas が update されておらず frame.position が決まらないため、
    //   ForceUpdateCanvases で全 Canvas の layout を即時 rebuild してから position を読む。
    private void RefreshDetailDisplay(MerchantStockEntry entry)
    {
        if (merchantDisplay == null || merchantCamera == null || rawImageRect == null) return;
        if (entry == null) { merchantDisplay.Clear(); return; }
        if (detailProductFrame == null || entry.item == null) { merchantDisplay.Clear(); return; }

        merchantDisplay.ViewSize = detailViewSize;
        // DetailView では「枠の位置をカメラViewportにマップ」せず、カメラ中央 (vp 0.5, 0.5) にモデルを置く。
        // DetailProductFrame は MerchantRT 全体を映すので、カメラ中央の品が枠の中央に大きく見える。
        var pos = merchantCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
        var items = new List<ItemData> { entry.item };
        var positions = new List<Vector3> { pos };
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

    // 所持金＝プレイヤーのインベントリ全体のcurrencyValue合計（コインは1個ずつ1Gなど）。
    //   瓶UIが開いてる時はBottle.Items（物理実体）が真の中身、閉じてる時はRecordsがスナップショット。
    //   PendingItemsは「まだ瓶に積まれてない初期分」なので常に加算。
    //   商人UI中は瓶も開きっぱなしの設計なので通常はBottle.Itemsから集計する経路を通る。
    private void RefreshWallet()
    {
        if (walletLabel == null) return;
        int gold = 0;
        bool isLiveBottle = playerBottle != null && playerBottle.Items != null && playerBottle.Items.Count > 0;
        if (isLiveBottle)
        {
            var items = playerBottle.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i]?.Data;
                if (d != null && d.CurrencyValue > 0) gold += d.CurrencyValue;
            }
        }
        else if (OwnHolder != null)
        {
            var records = OwnHolder.Records;
            for (int i = 0; i < records.Count; i++)
            {
                var d = records[i].data;
                if (d != null && d.CurrencyValue > 0) gold += d.CurrencyValue;
            }
        }
        if (OwnHolder != null)
        {
            var pending = OwnHolder.PendingItems;
            for (int i = 0; i < pending.Count; i++)
            {
                var d = pending[i];
                if (d != null && d.CurrencyValue > 0) gold += d.CurrencyValue;
            }
        }
        walletLabel.text = $"{gold}G";
    }

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

    // PlayerHandState.OnItemTakenOut の冒頭から呼ばれる。
    //   購入受付中（payingEntry あり）：priceItem と一致したら受け皿へ（支払い）。
    //   ListView中（payingEntry なし）：商人の買い取り対象なら受け皿へ＝売却モードに入る（段階3-5）。
    //   戻り値：受け皿に投入したなら true（PlayerHandStateは以降の処理をスキップ）。
    public bool TryDropIntoTray(ItemData data)
    {
        if (!open || data == null) return false;
        if (tray == null || bottleItemFactory == null) return false;
        if (ceremonyRoutine != null) return false; // 儀式中は受け取らない

        if (payingEntry != null)
        {
            // 購入の支払い受付。
            if (payingEntry.priceItem != data) return false; // 別アイテムは受け取らない（手に渡す）
        }
        else
        {
            // 売却の受付（査定）。買い取り金額が付かない品は受け皿に乗せず、即瓶へ返して断る。
            //   対象：買い取り対象外（コイン等の通貨・フォールバック無効）／対価が0（priceItem未設定・priceCount<=0）／枠切れ・枠超過。
            //   手に持たせず瓶へ直接戻す（瓶UIは開きっぱなしなので口から落ちて見える）。
            var buyEntry = Current != null ? Current.FindBuyEntry(data) : null;
            bool refuse = buyEntry == null
                || buyEntry.priceItem == null
                || buyEntry.priceCount <= 0
                || CountInTray(data) >= buyEntry.stock; // 残り買い取り枠の超過分
            if (refuse)
            {
                DropItemToBottle(data);
                if (Current != null) ShowEmotion(Current.PortraitSad, Current.LineRefuse); // 困り顔（それは買い取れません！）
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

    // 受け皿にある data と同じアイテムの個数。
    private int CountInTray(ItemData data)
    {
        if (tray == null || tray.Items == null) return 0;
        int count = 0;
        for (int i = 0; i < tray.Items.Count; i++)
        {
            var core = tray.Items[i];
            if (core != null && core.Data == data) count++;
        }
        return count;
    }

    private void OnTrayChanged()
    {
        if (selling) RecalculateSell();
        else RecalculatePayment();
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

    // 売却の査定：受け皿の中身を買い取りリストで評価し、対価合計をラベルに出す。
    //   買い取り枠(entry.stock)を超えた分は査定に含めない（TryDropIntoTrayでも弾くが二重ガード）。
    private void RecalculateSell()
    {
        if (!selling) return;
        if (ceremonyRoutine != null) return; // 儀式中の受け皿変動（対価の設置等）でボタンを再点灯させない
        var appraisal = AppraiseTray(out int sellableCount);

        if (detailProductLabel != null)
        {
            if (sellableCount <= 0) detailProductLabel.text = "買い取れる物がありません";
            else
            {
                var parts = new List<string>();
                foreach (var pair in appraisal)
                    parts.Add($"{pair.Key.ItemName}×{pair.Value}");
                detailProductLabel.text = $"買い取り {sellableCount}個 → {string.Join("＋", parts)}";
            }
        }
        if (confirmButton != null) confirmButton.interactable = sellableCount > 0;
        RefreshWallet();
    }

    // 受け皿の中身を査定する：支払いアイテムごとの合計個数と、売れる品の総数を返す。
    //   買い取り枠はエントリごとに entry.stock 個まで。priceItem 未設定のエントリは対価なし（個数のみ加算）。
    private Dictionary<ItemData, int> AppraiseTray(out int sellableCount)
    {
        var payoutTotal = new Dictionary<ItemData, int>();
        var usedPerEntry = new Dictionary<MerchantStockEntry, int>();
        sellableCount = 0;
        if (tray == null || tray.Items == null || Current == null) return payoutTotal;

        for (int i = 0; i < tray.Items.Count; i++)
        {
            var core = tray.Items[i];
            if (core == null || core.Data == null) continue;
            var entry = Current.FindBuyEntry(core.Data);
            if (entry == null) continue;
            usedPerEntry.TryGetValue(entry, out int used);
            if (used >= entry.stock) continue; // 枠超過分は査定外
            usedPerEntry[entry] = used + 1;
            sellableCount++;
            if (entry.priceItem != null && entry.priceCount > 0)
            {
                payoutTotal.TryGetValue(entry.priceItem, out int total);
                payoutTotal[entry.priceItem] = total + entry.priceCount;
            }
        }
        return payoutTotal;
    }

    // プレイヤー瓶の中身が変わった（Drag取り出し／受け皿巻き戻し／購入投入）→ウォレット再計算。
    private void OnPlayerBottleChanged()
    {
        RefreshWallet();
    }

    // 受け皿の Items を走査して payingEntry.priceItem の個数を数える → paidCount に反映。
    //   段階3-4：自動成立はやめ、確定ボタンの interactable を更新するだけ。
    private void RecalculatePayment()
    {
        if (payingEntry == null || tray == null) return;
        var items = tray.Items;
        int count = 0;
        if (items != null && payingEntry.priceItem != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var core = items[i];
                if (core != null && core.Data == payingEntry.priceItem) count++;
            }
        }
        paidCount = count;
        UpdateConfirmButton();
        RefreshWallet();
    }

    // 確定ボタンの interactable は「priceCount==0（無料）」 または「paidCount >= priceCount」 で true。
    private void UpdateConfirmButton()
    {
        if (confirmButton == null) return;
        bool canConfirm = payingEntry != null
            && (payingEntry.priceCount == 0 || paidCount >= payingEntry.priceCount);
        confirmButton.interactable = canConfirm;
    }

    // 確定ボタン押下＝取引開始。購入なら「手がコイン回収→商品を置く」、売却なら「手が売却品を回収→対価を置く」。
    private void OnConfirmClicked()
    {
        if (ceremonyRoutine != null) return; // 儀式中の多重起動防止
        if (selling)
        {
            ceremonyRoutine = StartCoroutine(SellCeremony());
            return;
        }
        if (payingEntry == null) return;
        if (payingEntry.priceCount > 0 && paidCount < payingEntry.priceCount) return; // 念のため二重ガード
        ceremonyRoutine = StartCoroutine(PurchaseCeremony());
    }

    // 「キャンセル」ボタン押下：受け皿の中身を瓶へリアルタイム復元して ListView へ復帰。
    //   Close()と違って瓶UIは開きっぱなしなので、コインが口から物理復活して見える（UX的に自然）。
    //   売却モード中も同じ（査定中の品が瓶へ戻る）。
    private void OnCancelClicked()
    {
        RefundTrayToBottleLive();
        payingEntry = null;
        paidCount = 0;
        if (Current != null) ShowEmotion(Current.PortraitSad, Current.LineCancel); // 悲しい顔（売れなかった…）
        ShowListView();
    }

    // 表情とセリフを一時的に切り替え、emotionDuration 秒後に通常（顔＝Normal・セリフ＝挨拶）へ戻す。
    private void ShowEmotion(Texture2D tex, string line)
    {
        if (emotionRoutine != null) StopCoroutine(emotionRoutine);
        emotionRoutine = StartCoroutine(EmotionRoutine(tex, line));
    }

    private IEnumerator EmotionRoutine(Texture2D tex, string line)
    {
        if (merchantPortrait != null && tex != null && merchantPortrait.gameObject.activeSelf)
            merchantPortrait.texture = tex;
        if (speechLabel != null && !string.IsNullOrEmpty(line))
            speechLabel.text = line;

        yield return new WaitForSeconds(emotionDuration);

        if (open && Current != null)
        {
            if (merchantPortrait != null) merchantPortrait.texture = Current.PortraitNormal;
            if (speechLabel != null) speechLabel.text = Current.LineGreeting;
        }
        emotionRoutine = null;
    }

    // 取引の儀式：手がコインを1枚ずつ回収 → 商品を受け皿にコトン → プレイヤーがクリックして瓶へ。
    //   旧CompletePurchase（即時消費・即時投入）を演出付きシーケンスに置き換えたもの。
    private IEnumerator PurchaseCeremony()
    {
        var entry = payingEntry;
        ceremonyEntry = entry;
        payingEntry = null; // 以降の再計算・別商品クリックの巻き戻し対象から外す
        SetTradeButtons(false);

        // 1) 手がコインを1枚ずつ回収（演出。手が無ければ間隔だけ刻む）。
        int toConsume = entry.priceCount;
        if (toConsume > 0 && entry.priceItem != null && tray != null)
        {
            var coins = new List<BottleItemCore>();
            foreach (var c in tray.Items)
                if (c != null && c.Data == entry.priceItem && coins.Count < toConsume) coins.Add(c);

            tray.OnItemsChanged -= OnTrayChanged; // 一括消費中の余計な再計算を抑止
            foreach (var coin in coins)
            {
                if (coin == null) continue;
                yield return MoveHand(coin.transform.position);
                ceremonyConsumedCoins.Add(coin.Data); // 回収途中で閉じられたらキャンセル扱いで返金する
                tray.Unregister(coin);
                Destroy(coin.gameObject);
                yield return new WaitForSeconds(ceremonyStepInterval);
            }
            if (open) tray.OnItemsChanged += OnTrayChanged;
        }
        ceremonyPaid = true;             // ここから商品設置までは「支払い完了・商品未設置」区間（中断時は商品を補填）
        ceremonyConsumedCoins.Clear();   // 全額回収完了＝もう返金しない（以降の中断は商品補填で精算）

        // 2) 在庫を減らし、商品を受け皿にコトンと置く。
        if (Current != null) Current.TryConsumeStock(entry);
        BottleItemCore product = null;
        if (entry.item != null && tray != null && bottleItemFactory != null)
        {
            Vector3 dropPos = tray.GetRandomDropPosition(); // 散らして置く（コインの山の上に積まない）
            // 見本（拡大表示）はこのタイミングで下げる＝「見本を引っ込めて実物を置く」受け渡し感。
            if (merchantDisplay != null) merchantDisplay.Clear();
            yield return MoveHand(dropPos);
            product = bottleItemFactory.Create(entry.item, dropPos, Quaternion.identity, tray.transform);
            if (product != null)
            {
                product.Initialize(entry.item);
                tray.Register(product);
                ceremonyPaid = false; // 受け皿に実体が出来た＝以降の中断はRefundTrayToPendingが面倒を見る
            }
        }
        HideHand();
        if (Current != null) ShowEmotion(Current.PortraitHappy, Current.LineThanks); // 喜び顔（毎度あり!）
        RefreshWallet();
        paidCount = 0;
        Debug.Log($"[MerchantUI] 購入成立: {entry.item?.ItemName}（残在庫 {entry.stock}）→ 受け皿の商品をクリックで受け取り");

        // 3) 受け皿の商品クリック待ち → 瓶へ。
        if (product != null && trayPicker != null)
        {
            bool picked = false;
            trayPicker.Watch(product, _ => picked = true);
            while (!picked && open) yield return null;
            trayPicker.StopWatch();
            if (product != null) // Close済みならRefund側で処理済み（Destroyされている）
            {
                var data = product.Data;
                tray.Unregister(product);
                Destroy(product.gameObject);
                DropItemToBottle(data);
            }
        }
        else if (entry.item != null)
        {
            // 受け皿に置けなかった（factory未設定等）場合のフォールバック：従来通り直接瓶へ。
            DropItemToBottle(entry.item);
            ceremonyPaid = false;
        }

        SetTradeButtons(true);
        ceremonyEntry = null;
        ceremonyRoutine = null;
        if (open) ShowListView();
    }

    // 売却の儀式：手が売却品を1個ずつ回収 → 対価（コイン等）を受け皿にコトン → クリックで全額瓶へ。
    //   PurchaseCeremony の逆向き。中断時の精算：
    //     ・売却品の回収途中 → 不成立扱い：回収済みの売却品を返却（ceremonyConsumedCoins を流用。買い取り枠は未消費）
    //     ・回収完了〜対価設置前 → 成立扱い：未設置の対価を PendingItems で補填（ceremonyPayout）
    //     ・対価設置後 → 受け皿に実体があるので RefundTrayToPending が瓶へ戻す
    private IEnumerator SellCeremony()
    {
        SetTradeButtons(false);

        // 0) 査定スナップショット：受け皿の中身から「売る実体」と「エントリごとの個数」を確定。
        var soldCores = new List<BottleItemCore>();
        var soldPerEntry = new Dictionary<MerchantStockEntry, int>();
        if (tray != null && tray.Items != null && Current != null)
        {
            foreach (var core in tray.Items)
            {
                if (core == null || core.Data == null) continue;
                var entry = Current.FindBuyEntry(core.Data);
                if (entry == null) continue;
                soldPerEntry.TryGetValue(entry, out int used);
                if (used >= entry.stock) continue; // 枠超過分は売らない（受け皿に残す→キャンセル/Closeで返却）
                soldPerEntry[entry] = used + 1;
                soldCores.Add(core);
            }
        }
        if (soldCores.Count == 0)
        {
            SetTradeButtons(true);
            ceremonyRoutine = null;
            yield break;
        }

        // 1) 手が売却品を1個ずつ回収（不成立中断に備えて回収分を記録）。
        if (tray != null) tray.OnItemsChanged -= OnTrayChanged;
        foreach (var core in soldCores)
        {
            if (core == null) continue;
            yield return MoveHand(core.transform.position);
            ceremonyConsumedCoins.Add(core.Data);
            tray.Unregister(core);
            Destroy(core.gameObject);
            yield return new WaitForSeconds(ceremonyStepInterval);
        }
        if (open && tray != null) tray.OnItemsChanged += OnTrayChanged;

        // 2) 成立：買い取り枠を消費し、対価リストを確定（以降の中断は対価補填で精算）。
        ceremonyConsumedCoins.Clear();
        ceremonyPayout.Clear();
        foreach (var pair in soldPerEntry)
        {
            for (int i = 0; i < pair.Value; i++)
            {
                if (Current != null) Current.TryConsumeBuyStock(pair.Key);
                if (pair.Key.priceItem != null)
                    for (int j = 0; j < pair.Key.priceCount; j++) ceremonyPayout.Add(pair.Key.priceItem);
            }
        }
        ceremonyPaid = true;

        // 3) 手が中央に1回来て、対価をまとめてバッと置く（中央±payoutScatter で少しだけ散らす）。
        //    置けた分は ceremonyPayout から外す＝中断補填と重複させない。
        var payoutCores = new List<BottleItemCore>();
        if (tray != null && bottleItemFactory != null && ceremonyPayout.Count > 0)
        {
            Vector3 center = tray.GetDropPosition();
            yield return MoveHand(center);
            var payoutSnapshot = new List<ItemData>(ceremonyPayout);
            foreach (var pay in payoutSnapshot)
            {
                Vector3 dropPos = center + tray.transform.right * Random.Range(-payoutScatter, payoutScatter);
                var core = bottleItemFactory.Create(pay, dropPos, Quaternion.identity, tray.transform);
                if (core != null)
                {
                    core.Initialize(pay);
                    tray.Register(core);
                    payoutCores.Add(core);
                    ceremonyPayout.Remove(pay);
                }
                if (payoutDropInterval > 0f) yield return new WaitForSeconds(payoutDropInterval);
            }
        }
        ceremonyPaid = false;
        // 置けなかった分（factory未設定等）は直接瓶へフォールバック。
        if (ceremonyPayout.Count > 0)
        {
            foreach (var pay in ceremonyPayout) DropItemToBottle(pay);
            ceremonyPayout.Clear();
        }

        HideHand();
        if (Current != null) ShowEmotion(Current.PortraitHappy, Current.LineThanks); // 喜び顔（良い物をありがとう!）
        RefreshWallet();
        Debug.Log($"[MerchantUI] 売却成立: {soldCores.Count}個 → 受け皿の対価をクリックで受け取り");

        // 4) 対価のクリック待ち（どれか1枚クリックで全額まとめて瓶へ）。
        if (payoutCores.Count > 0 && trayPicker != null)
        {
            bool picked = false;
            trayPicker.Watch(payoutCores, _ => picked = true);
            while (!picked && open) yield return null;
            trayPicker.StopWatch();
            if (open)
            {
                foreach (var core in payoutCores)
                {
                    if (core == null) continue; // Close済みならRefund側で処理済み
                    var data = core.Data;
                    tray.Unregister(core);
                    Destroy(core.gameObject);
                    DropItemToBottle(data);
                }
            }
        }

        SetTradeButtons(true);
        ceremonyRoutine = null;
        if (open) ShowListView();
    }

    // 商人の手を target へ滑らかに移動させる（未設定なら何もしない）。
    //   Zはコインより手前(カメラ寄り)に固定して、手のスプライトが上に描かれるようにする。
    private IEnumerator MoveHand(Vector3 target)
    {
        if (handVisual == null) yield break;
        target.z = -1f;
        if (!handVisual.gameObject.activeSelf)
        {
            handVisual.gameObject.SetActive(true);
            handVisual.position = (tray != null ? tray.GetDropPosition() : target) + Vector3.up * 0.5f + Vector3.forward * -1f;
        }
        Vector3 from = handVisual.position;
        float t = 0f;
        while (t < handMoveDuration)
        {
            t += Time.deltaTime;
            handVisual.position = Vector3.Lerp(from, target, Mathf.Clamp01(t / handMoveDuration));
            yield return null;
        }
        handVisual.position = target;
    }

    private void HideHand()
    {
        if (handVisual != null) handVisual.gameObject.SetActive(false);
    }

    // 儀式中は購入/キャンセルを押せなくする（多重実行・途中キャンセルの事故防止）。
    private void SetTradeButtons(bool value)
    {
        if (confirmButton != null) confirmButton.interactable = value;
        if (cancelButton != null) cancelButton.interactable = value;
    }

    // ItemDataを瓶に入れる共通処理。
    //   瓶UIが開いている：BottleItemFactoryで物理実体を口の上に生成→Register（リアルタイム落下・即見える）。
    //   開いていない：OwnHolder.PendingItemsに積む（次回開いた時に落ちる）。
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
        if (OwnHolder != null) OwnHolder.PendingItems.Add(data);
    }
}
