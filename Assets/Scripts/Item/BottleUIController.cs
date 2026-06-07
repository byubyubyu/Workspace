// 保存先: Assets/Scripts/Item/BottleUIController.cs
// 瓶UIの制御役（ミニマップのMinimapControllerに相当）。
//   ・開閉（キー入力。新Input System直接読み）。拾った時は OpenBottle() を外（ItemPicker）から呼んで自動で開く。
//   ・開いている間だけ瓶専用カメラを有効化（撮影カメラ→RenderTexture→RawImageで表示）。
//   ・開いている間だけ BottleDragger を有効化。
//   ・開く時：BottleStorage.Load() で前回の積み方を復元。
//   ・閉じる時：見た目だけ閉じる（プレイヤーは行動再開＝無防備が延びない）。
//     裏の物理空間は静止するまで動かし続け、静止 or タイムアウトで BottleStorage.Save()（記録＋破棄）。
//     ＝「閉じても物理結果は確定する（逃げ得防止）」。
//
//   表示の配置（RawImageの大きさ・余白＝周囲から背後の世界が見える）はCanvas側で調整（実装時）。
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BottleUIController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameObject bottlePanel;   // 瓶UIのパネル（RawImageを含む）
    [SerializeField] private Camera bottleCamera;      // 瓶の2D物理空間を撮る専用カメラ
    [SerializeField] private Bottle bottle;            // 瓶（物理空間）
    [SerializeField] private BottleStorage storage;    // 記録・復元
    [SerializeField] private BottleDragger dragger;    // 漁る操作役
    [SerializeField] private InventoryHolder playerHolder; // 既定で開く対象（プレイヤーの中身）

    [Header("入力")]
    [SerializeField] private Key toggleKey = Key.I;    // 開閉キー（仮：Inventory）
    [SerializeField] private EquipmentUIController equipmentUI; // 装備画面中のIで装備を閉じて瓶を中央に開き直すため

    [Header("閉じる時の静止待ち")]
    [SerializeField] private float closeTimeout = 3f;  // 静止しなくても強制確定するまでの秒数（保険）

    private bool open;
    private bool closing; // 閉じ処理（静止待ち）中か
    private InventoryHolder current; // 今開いている対象（プレイヤー or 死体）
    public bool IsOpen => open;
    public InventoryHolder CurrentHolder => current; // 今開いている対象（Corpseが残存タイマー停止判定に使う）

    // 横断参照（Corpseが「自分が今開かれているか」を問い合わせる）。シーンに1個。
    public static BottleUIController Instance { get; private set; }

    // 瓶パネルのレイアウト切替用（I=中央＝元の配置／C=右半分）。
    private RectTransform bottleRect;
    private Vector2 defAnchorMin, defAnchorMax, defAnchoredPos, defSizeDelta;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[BottleUIController] 既に存在するため重複を破棄: {name}");
            Destroy(this);
            return;
        }
        Instance = this;

        // 瓶パネルの元の配置（中央）を保存しておく（C時は右半分へ切替、I時はここへ戻す）。
        if (bottlePanel != null)
        {
            bottleRect = bottlePanel.GetComponent<RectTransform>();
            if (bottleRect != null)
            {
                defAnchorMin = bottleRect.anchorMin;
                defAnchorMax = bottleRect.anchorMax;
                defAnchoredPos = bottleRect.anchoredPosition;
                defSizeDelta = bottleRect.sizeDelta;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (bottle != null) bottle.OnItemTakenOut += OnItemTakenOut;
    }

    private void OnDisable()
    {
        if (bottle != null) bottle.OnItemTakenOut -= OnItemTakenOut;
    }

    // 取り出し成立時：死体（他人の瓶＝current!=playerHolder）を漁っていたら、1個取り出すごとに自動で閉じる。
    //   手早い取り出しで死体↔自分の瓶を高速に行き来して物理が乱れるのを避ける（1個取ったら確定して閉じる）。
    //   自分の瓶は閉じない（漁い続けられる）。手に持つ処理は PlayerHandState 側が別途購読して行う。
    private void OnItemTakenOut(ItemData data)
    {
        if (open && current != null && current != playerHolder)
        {
            CloseBottle();
        }
    }

    private void Start()
    {
        // 初期は閉じる（見た目オフ・カメラオフ・Dragger無効）。
        ApplyVisual(false);
        open = false;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb[toggleKey].wasPressedThisFrame)
        {
            // 商人UI中のIは取引キャンセル（途中の払いは戻す）→ 瓶を中央で単独で開き直す。
            if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen)
            {
                MerchantUIController.Instance.Close(); // 取引キャンセル＋瓶も一旦閉じる
                SetRightHalf(false);
                OpenBottle();
                return;
            }
            // ミニマップ（M画面）中のIはMを閉じて、そのまま瓶を中央で開く（画面の切り替え）。
            if (MinimapController.Instance != null && MinimapController.Instance.IsOpen)
            {
                MinimapController.Instance.Close();
                SetRightHalf(false);
                OpenBottle();
                return;
            }
            // 装備画面が開いている時のI：装備UI＋瓶を閉じてから、瓶を中央で単独で開き直す。
            if (equipmentUI != null && equipmentUI.IsOpen)
            {
                equipmentUI.Close(); // 装備UIと瓶を閉じる
                SetRightHalf(false);
                OpenBottle();        // 瓶を中央で開く
            }
            else if (open) CloseBottle();
            else { SetRightHalf(false); OpenBottle(); } // Iキー単独＝中央（いつも通り）
        }
    }

    // 既定（プレイヤーの瓶）を開く。ItemPicker・I キーから呼ばれる。
    public void OpenBottle()
    {
        OpenBottle(playerHolder);
    }

    // 対象を指定して開く（段階4で死体の瓶もこれで開く）。キー or 拾った時に外から呼ぶ。
    public void OpenBottle(InventoryHolder holder)
    {
        if (open) return;
        if (holder == null)
        {
            Debug.LogError($"[BottleUIController] 開く対象(holder)が null です: {name}");
            return;
        }

        // 閉じ処理（静止待ち）の途中だった場合：コルーチンを止める前に、その対象へSaveを完了させる。
        //   静止待ち中はまだSaveが走っておらず瓶に実体が残っている。ここでSaveせずにLoadすると
        //   「残った実体＋復元分」で二重になる。なので開く直前に旧対象へ強制Save（記録・全破棄）する。
        if (closing)
        {
            StopAllCoroutines();
            closing = false;
            if (storage != null && current != null)
            {
                storage.Save(current);        // 旧対象へ即記録・破棄（瓶を空にする）
                NotifyCorpseClosed(current);  // 旧対象が空の死体なら消す（開き直しで前の死体を即閉じる経路）
            }
        }

        current = holder;
        open = true;
        ApplyVisual(true);

        // 対象の積み方を復元。
        if (storage != null) storage.Load(current);
    }

    // 閉じる（見た目だけ閉じ、物理は静止まで回してから記録）。
    public void CloseBottle()
    {
        if (!open) return;
        open = false;

        // 見た目を閉じる（プレイヤーは行動再開）。物理空間(Bottle)は動かし続ける。
        ApplyVisual(false);

        // 静止 or タイムアウトを待ってから記録・破棄。
        if (storage != null && bottle != null)
        {
            StopAllCoroutines();
            StartCoroutine(WaitRestThenSave());
        }
    }

    private IEnumerator WaitRestThenSave()
    {
        closing = true;
        float t = 0f;
        // 全アイテムが静止するか、タイムアウトまで待つ。
        while (t < closeTimeout && !bottle.IsAllAtRest())
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (current != null) storage.Save(current); // 記録＋破棄（共有方式：閉じたら物理は消す）
        closing = false;
        NotifyCorpseClosed(current); // 死体を閉じたとき、中身が空なら消す
    }

    // 閉じた対象が死体で中身が空なら消す（Corpseが自分で判定）。Save直後に呼ぶ。
    private void NotifyCorpseClosed(InventoryHolder holder)
    {
        if (holder == null) return;
        var corpse = holder.GetComponent<Corpse>();
        if (corpse != null) corpse.OnClosed();
    }

    // 見た目・カメラ・DraggerのまとめてON/OFF。
    private void ApplyVisual(bool value)
    {
        if (bottlePanel != null) bottlePanel.SetActive(value);
        if (bottleCamera != null) bottleCamera.enabled = value;
        if (dragger != null) dragger.enabled = value;
    }

    // 瓶パネルの配置を切り替える（true=右半分＝装備画面と並べる／false=元の配置＝中央・I単独）。
    public void SetRightHalf(bool right)
    {
        if (bottleRect == null) return;
        if (right)
        {
            bottleRect.anchorMin = new Vector2(0.5f, 0f);
            bottleRect.anchorMax = new Vector2(1f, 1f);
            bottleRect.offsetMin = Vector2.zero;
            bottleRect.offsetMax = Vector2.zero;
        }
        else
        {
            bottleRect.anchorMin = defAnchorMin;
            bottleRect.anchorMax = defAnchorMax;
            bottleRect.anchoredPosition = defAnchoredPos;
            bottleRect.sizeDelta = defSizeDelta;
        }
    }
}
