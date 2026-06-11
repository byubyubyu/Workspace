// 保存先: Assets/Scripts/Player/PlayerHandState.cs
// プレイヤーの「手の状態」を管理する司令塔（段階3b：見た目つき）。
//   状態(HandState)：Empty（手ぶら）/ Weapon（武器構え）/ Item（アイテム所持）。
//
//   左クリックの振り分け：
//     ・インベントリ中      … 何もしない（漁りはBottleDraggerが処理）
//     ・Item               … 使う → Empty
//     ・Empty              … 抜刀（Drawingへ。攻撃はしない）
//     ・Drawing            … 何もしない（抜刀中の隙）
//     ・Weapon             … 攻撃（Weaponのまま）
//
//   抜刀：Emptyで左クリック→Drawing（drawDuration秒の隙。攻撃不可・移動が遅い）→経過後Weapon。
//     抜刀中は走り(Sheathe)・回避(CancelDraw)でキャンセルしてEmptyへ。
//
//   状態遷移：取り出し→Item / インベントリを開く→納刀（Weapon/DrawingはEmpty、Itemは瓶に戻してEmpty）。
//   見た目：状態に応じて HandPoint の子モデルを出し分ける。
//     Weapon → 武器モデル(weaponPrefab) / Item → heldItemのItemData.Prefab / Empty → なし。
//     状態が変わるたびに今のモデルを消して作り直す（シンプル方式）。
//     アイテムの手持ちサイズは mapViewSize を流用（手に持つサイズ。必要なら将来専用値を足す）。
//
//   （段階3c予定）走る(Shift+移動)で納刀、Weapon中は移動速度ダウン。
//   入力は新Input Systemで直接読む。攻撃の実体は PlayerCombatCore.TryAttack() に委譲。
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandState : MonoBehaviour
{
    [SerializeField] private PlayerCombatCore combat;        // 攻撃の実体（TryAttackを呼ぶ）
    [SerializeField] private BottleUIController bottleUI;    // インベントリ開閉状態の参照（IsOpen）
    [SerializeField] private Bottle bottle;                 // 取り出しイベントの発生源
    [SerializeField] private ItemPicker itemPicker;         // 手持ちアイテムを瓶に戻す（PutIntoBottle）
    [SerializeField] private EquipmentHolder equipmentHolder; // 装備の脱着（仮キー・段階1）

    [Header("見た目")]
    [SerializeField] private Transform handPoint;           // 武器/アイテムを出す位置（Playerの子）
    [SerializeField] private Transform backPoint;           // 納刀中の武器を背負う位置（Playerの子・任意）
    [SerializeField] private GameObject weaponPrefab;       // 武器の見た目（仮でCube等。後で本物に差し替え可）

    [Header("抜刀")]
    [SerializeField] private float drawDuration = 1f;       // 抜刀にかかる時間＝隙（秒）。仮・後調整
    [SerializeField] private float sheatheViewLinger = 0.75f; // 納刀時に武器の見た目を消すまでの時間（納刀モーション尺に合わせる）
    [SerializeField] private Vector3 drawViewEuler = Vector3.zero; // 抜刀モーション中だけ武器に足す回転（刃先を上向きに）
    [SerializeField] private GameObject drawEffect;         // 抜刀時のエフェクト（任意・null可）
    [SerializeField] private AudioClip drawSound;           // 抜刀音（任意・null可）

    private HandState state = HandState.Empty;
    private ItemData heldItem;        // Item状態のとき手に持っているアイテム
    private bool prevInventoryOpen;   // インベントリ開閉の「開いた瞬間」検知用
    private GameObject currentView;   // HandPointに今出ている見た目モデル
    private float drawTimer;          // 抜刀の残り時間（Drawing中のみ使用）

    public HandState State => state;
    public ItemData HeldItem => heldItem;

    private void OnEnable()
    {
        if (bottle != null) bottle.OnItemTakenOut += OnItemTakenOut;
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged += OnEquipmentChanged;
    }

    private void OnDisable()
    {
        if (bottle != null) bottle.OnItemTakenOut -= OnItemTakenOut;
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged -= OnEquipmentChanged;
    }

    private void Start()
    {
        RefreshView(); // 初期（Empty）＝何も持たない見た目に
    }

    private void Update()
    {
        // インベントリが「開いた瞬間」を検知して納刀処理。
        bool open = (bottleUI != null && bottleUI.IsOpen);
        if (open && !prevInventoryOpen)
        {
            OnInventoryOpened();
        }
        prevInventoryOpen = open;

        // 抜刀中はタイマーを進め、満了で武器構え(Weapon)へ移る。
        if (state == HandState.Drawing)
        {
            drawTimer -= Time.deltaTime;
            if (drawTimer <= 0f) SetState(HandState.Weapon);
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }
    }

    private void HandleLeftClick()
    {
        // 統合メニュー中は攻撃も使用もしない（タブやマーカーのクリックと衝突するため）。
        if (TabMenuController.Instance != null && TabMenuController.Instance.IsOpen) return;
        // インベントリ中は攻撃も使用もしない（漁り操作はBottleDraggerが別途処理する）。
        if (bottleUI != null && bottleUI.IsOpen) return;
        // ミニマップ（M画面）中も攻撃しない。
        if (MinimapController.Instance != null && MinimapController.Instance.IsOpen) return;
        // 商人UI中も攻撃しない（売買中の事故を防ぐ。瓶のドラッグはBottleDraggerが処理する）。
        if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen) return;
        // ステータスUI（P画面）中も攻撃しない（スキル行ボタンのクリックと衝突するため）。
        if (StatusUIController.Instance != null && StatusUIController.Instance.IsOpen) return;
        // 市民プロフィール（婚活）中も攻撃しない（求婚ボタンのクリックと衝突するため）。
        if (CitizenProfileUIController.Instance != null && CitizenProfileUIController.Instance.IsOpen) return;

        switch (state)
        {
            case HandState.Item:
                UseHeldItem();
                break;

            case HandState.Empty:
                StartDraw();                      // 抜刀のみ（攻撃はしない）
                break;

            case HandState.Drawing:
                // 抜刀中は何もしない（隙）。
                break;

            case HandState.Weapon:
                if (combat != null) combat.TryAttack();
                break;
        }
    }

    // 抜刀を開始する（Empty左クリック）。Drawingに入り、タイマーと演出を起動する。
    //   武器の見た目は抜刀モーション開始と同時に出す（RefreshViewのDrawingケース）。
    private void StartDraw()
    {
        if (!HasWeapon()) return; // 武器が無ければ抜刀できない（技がないと構えても攻撃できないため）
        SetState(HandState.Drawing);
        drawTimer = drawDuration;

        if (drawEffect != null && handPoint != null)
            Instantiate(drawEffect, handPoint.position, handPoint.rotation);
        if (drawSound != null)
            AudioSource.PlayClipAtPoint(drawSound, transform.position);
    }

    // 装備が変わったとき：武器が無くなったら納刀する（Weapon/Drawingを解除）。
    //   状態が変わらないケース（手ぶらで武器を装備した等）でも背中の表示は更新する。
    private void OnEquipmentChanged()
    {
        if (!HasWeapon() && (state == HandState.Weapon || state == HandState.Drawing))
            SetState(HandState.Empty);
        else
            RefreshBackView();
    }

    // 武器を装備しているか（右手に武器の技セットがあるか）。
    private bool HasWeapon()
    {
        return equipmentHolder != null && equipmentHolder.GetWeaponAttack() != null;
    }

    private void OnItemTakenOut(ItemData data)
    {
        if (data == null) return;

        // 商人UI受付中なら、取り出されたアイテムを商人の受け皿（物理空間）に物理生成して落とす。
        //   priceItem 一致 → 受け皿に投入し true（手に持たせない）。priceItem 不一致は false で通常経路へ。
        var merchant = MerchantUIController.Instance;
        if (merchant != null && merchant.TryDropIntoTray(data)) return;

        // 装備品は手に持たず、種別に応じて自動装備する（手持ち＝装備の方針）。外すのは装備UI（クリック）。
        if (data.Equipment != null && equipmentHolder != null)
        {
            equipmentHolder.Equip(data);
            return;
        }

        // 手が既に埋まっている（アイテム所持中）なら手に取れない＝マップに落とす（手は1個・上書き消失を防ぐ）。
        //   「手以外で口の外に出たらマップ」の仕様に沿う。死体の瓶でも同じ。
        if (state == HandState.Item || heldItem != null)
        {
            if (itemPicker != null) itemPicker.DropToMap(data);
            return;
        }

        heldItem = data;
        SetState(HandState.Item);
    }

    private void UseHeldItem()
    {
        if (heldItem != null) heldItem.Effect?.Use(); // 効果はnull可（中身は将来）
        heldItem = null;
        SetState(HandState.Empty);
    }

    private void OnInventoryOpened()
    {
        if (state == HandState.Item && heldItem != null)
        {
            if (itemPicker != null) itemPicker.PutIntoBottle(heldItem); // 手持ちを瓶に戻す
            heldItem = null;
        }
        SetState(HandState.Empty); // 納刀
    }

    // 状態を変え、見た目を更新する（状態変更は必ずここを通す）。
    private void SetState(HandState next)
    {
        if (state == next && currentView != null) return; // 同状態なら作り直さない（Weapon連打で再生成しない）
        // 納刀（構え→手ぶら）だけは武器の見た目を納刀モーションが終わるまで残す。
        bool sheathing = state == HandState.Weapon && next == HandState.Empty;
        state = next;
        RefreshView(sheathing);
    }

    // 納刀（武器をしまう）。走り出したときにPlayerMovementから呼ばれる。
    //   Weapon・Drawing のどちらも Empty にする（走ると武器をしまう＝抜刀中なら抜刀キャンセル）。
    //   Item（アイテム所持）中は持ったまま走れるので触らない。
    public void Sheathe()
    {
        if (state == HandState.Weapon || state == HandState.Drawing) SetState(HandState.Empty);
    }

    // 抜刀キャンセル（回避時にPlayerCombatCoreから呼ばれる）。
    //   抜刀中(Drawing)のみ Empty に戻す。武器構え(Weapon)中は構えたまま回避できるよう触らない。
    public void CancelDraw()
    {
        if (state == HandState.Drawing) SetState(HandState.Empty);
    }

    // 状態に応じた見た目を HandPoint に出し分ける（今のを消して作り直す）。
    private GameObject lingeringView; // 納刀モーション中だけ残している武器（遅延破棄待ち）
    private void RefreshView() => RefreshView(false);
    private void RefreshView(bool sheathing)
    {
        // 既存の見た目を消す。納刀時だけは納刀モーションが終わるまで残す（遅延破棄）。
        if (currentView != null)
        {
            if (sheathing)
            {
                lingeringView = currentView;
                // 納刀モーション中は抜刀時と同じ「刃先上向き」にする（肩越しに戻す動きと合わせる）。
                lingeringView.transform.localRotation = Quaternion.Euler(drawViewEuler);
                Destroy(currentView, sheatheViewLinger);
            }
            else
            {
                Destroy(currentView);
            }
            currentView = null;
        }

        if (handPoint == null) return;

        switch (state)
        {
            // Drawing（抜刀中）：手に持たせるが、刃先は上向き（drawViewEuler）＝背中から抜いている途中の見た目。
            // Weaponになったら通常の構え向き（identity）で作り直す。
            case HandState.Drawing:
            case HandState.Weapon:
                if (lingeringView != null)
                {
                    Destroy(lingeringView); // 納刀の残像が残っていたら即消す（連続抜刀の二重表示防止）
                    lingeringView = null;
                }
                if (weaponPrefab != null)
                {
                    currentView = Instantiate(weaponPrefab, handPoint);
                    currentView.transform.localPosition = Vector3.zero;
                    currentView.transform.localRotation =
                        state == HandState.Drawing ? Quaternion.Euler(drawViewEuler) : Quaternion.identity;
                }
                break;

            case HandState.Item:
                if (heldItem != null && heldItem.Prefab != null)
                {
                    currentView = Instantiate(heldItem.Prefab, handPoint);
                    currentView.transform.localPosition = Vector3.zero;
                    currentView.transform.localRotation = Quaternion.identity;
                    // 手に持つサイズに合わせる（mapViewSizeを流用）。
                    ItemViewScaler.FitToSize(currentView, heldItem.MapViewSize);
                }
                break;

            case HandState.Empty:
            default:
                // 手には何も出さない。
                break;
        }

        // 背中の武器表示を更新（納刀時は納刀モーションが終わってから背負う）。
        CancelInvoke(nameof(RefreshBackView));
        if (sheathing) Invoke(nameof(RefreshBackView), sheatheViewLinger);
        else RefreshBackView();
    }

    // 背中の武器：装備していて手に持っていない（Empty/Item）あいだだけ背負う。
    private GameObject backView;
    private void RefreshBackView()
    {
        if (backView != null)
        {
            Destroy(backView);
            backView = null;
        }
        bool show = HasWeapon() && (state == HandState.Empty || state == HandState.Item);
        if (show && weaponPrefab != null && backPoint != null)
        {
            backView = Instantiate(weaponPrefab, backPoint);
            backView.transform.localPosition = Vector3.zero;
            backView.transform.localRotation = Quaternion.identity;
        }
    }
}
