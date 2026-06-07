// 保存先: Assets/Scripts/Player/EquipmentUIController.cs
// 装備UIの制御役（段階3）。Cキーで開閉し、各スロットの装備中アイテム名を表示する。
//   スロットのボタンをクリックすると、その装備を外して自分の瓶へ戻す（EquipmentHolder.Unequip）。
//   装備する操作は持たない：取り出した装備品はPlayerHandStateが自動装備する（手持ち＝装備の方針）。
//   表示は当面アイテム名テキスト（最小）。3Dモデルのアイコン表示は将来（瓶と同じRenderTexture方式）。
//   ※ Canvas・スロットのButton/TextはInspectorで割り当てる（瓶UI・ミニマップと同じ流儀）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EquipmentUIController : MonoBehaviour
{
    // 1スロットの表示（どのスロットを、どのTextに名前表示し、どのButtonで外すか）。
    [System.Serializable]
    public struct SlotView
    {
        public EquipmentSlot slot;
        public Text label;    // 装備中アイテム名（空なら "(空)"）
        public Button button; // クリックで外す
    }

    [SerializeField] private GameObject panel;               // 装備UIのパネル（開いている間だけ表示・左側に置く）
    [SerializeField] private EquipmentHolder equipmentHolder;
    [SerializeField] private BottleUIController bottleUI;    // 装備画面と一緒に開く自分の瓶（右側に置く・いつも通り漁れる）
    [SerializeField] private EquipmentDisplay equipmentDisplay; // 裏空間に装備品3Dモデルを並べる（RenderTextureで左半分に表示）
    [SerializeField] private Camera equipmentCamera;        // 装備表示エリアを撮る専用カメラ（開いている間だけ有効）
    [SerializeField] private RectTransform rawImageRect;    // モデルを映すRawImageの矩形（枠位置→モデル位置の変換基準）
    [SerializeField] private Camera uiCamera;               // Canvasのカメラ（Screen Space Overlayならnullのまま）
    [SerializeField] private float depth = 10f;             // 装備カメラからモデルを置く奥行き（near〜far内で調整）
    [SerializeField] private Key toggleKey = Key.C;          // 開閉キー
    [SerializeField] private List<SlotView> slotViews = new List<SlotView>(); // 5スロット分（右手/左手/頭/鎧/靴）

    private bool open;
    public bool IsOpen => open;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (equipmentCamera != null) equipmentCamera.enabled = false; // 初期は装備カメラOFF
        open = false;

        // 各スロットのボタンに「外す」を登録。
        foreach (var sv in slotViews)
        {
            if (sv.button == null) continue;
            var slot = sv.slot; // クロージャ用にローカルへ退避
            sv.button.onClick.AddListener(() => OnSlotClicked(slot));
        }

        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged -= Refresh;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
        {
            // 商人UI中のCは取引キャンセル→装備画面を開く（画面の切り替え）。
            if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen)
            {
                MerchantUIController.Instance.Close();
                Open();
                return;
            }
            // ミニマップ（M画面）中のCはMを閉じて、そのまま装備画面を開く（画面の切り替え）。
            if (MinimapController.Instance != null && MinimapController.Instance.IsOpen)
            {
                MinimapController.Instance.Close();
                Open();
                return;
            }
            Toggle();
        }
    }

    private void Toggle()
    {
        if (open) Close();
        else Open();
    }

    // 装備UI＋自分の瓶（右半分）を開く。
    public void Open()
    {
        if (open) return;
        open = true;
        if (panel != null) panel.SetActive(true);
        if (equipmentCamera != null) equipmentCamera.enabled = true; // 開いている間だけ装備カメラを有効化
        Refresh(); // 最新の装備内容を反映
        if (bottleUI != null)
        {
            bottleUI.SetRightHalf(true); // 装備画面と並べるため瓶を右半分に
            bottleUI.OpenBottle();       // 自分の瓶も一緒に開く（右側で漁れる＝取り出して自動装備）
        }
    }

    // 装備UI＋瓶を閉じる（Iキーで瓶を中央に開き直す時などに外からも呼ばれる）。
    public void Close()
    {
        if (!open) return;
        open = false;
        if (panel != null) panel.SetActive(false);
        if (equipmentCamera != null) equipmentCamera.enabled = false;
        if (bottleUI != null) bottleUI.CloseBottle(); // 一緒に閉じる
    }

    // スロットのボタンが押された：その装備を外す（瓶へ戻る。表示はOnEquipmentChanged→Refreshで更新）。
    private void OnSlotClicked(EquipmentSlot slot)
    {
        if (equipmentHolder != null) equipmentHolder.Unequip(slot);
    }

    // 各スロットの表示を、今の装備内容に合わせて更新する。
    private void Refresh()
    {
        if (equipmentHolder == null) return;

        // 各スロット枠(UI)の位置から、装備カメラ前のワールド位置を逆算してモデルを並べる。
        //   枠を自由に配置しても、その枠の中にモデルが映る（アンカー手動配置不要）。
        if (equipmentDisplay != null && equipmentCamera != null && rawImageRect != null)
        {
            var positions = new Dictionary<EquipmentSlot, Vector3>();
            foreach (var sv in slotViews)
            {
                if (sv.button == null) continue;
                var frame = sv.button.transform as RectTransform;
                if (frame == null) continue;
                positions[sv.slot] = SlotWorldPosition(frame);
            }
            equipmentDisplay.UpdateDisplay(equipmentHolder, positions);
        }

        // 名前テキストも併用する場合のみ更新（labelが無ければ無視＝3Dモデル表示だけでもよい）。
        foreach (var sv in slotViews)
        {
            if (sv.label == null) continue;
            var item = equipmentHolder.Get(sv.slot);
            sv.label.text = item != null ? item.ItemName : "(空)";
        }
    }

    // スロット枠(UI)の中心位置 → 装備カメラ前のワールド位置に変換する。
    //   枠の画面座標 → RawImage内ローカル → 正規化(=viewport) → 装備カメラのViewportToWorldPoint。
    //   （瓶のBottleDraggerと同じ座標変換）。
    private Vector3 SlotWorldPosition(RectTransform frame)
    {
        Vector3 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, frame.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, screen, uiCamera, out Vector2 local);
        Rect rect = rawImageRect.rect;
        float vx = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float vy = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
        return equipmentCamera.ViewportToWorldPoint(new Vector3(vx, vy, depth));
    }
}
