// 保存先: Assets/Scripts/Player/EquipmentUIController.cs
// 装備UIの制御役（段階3）。Cキーで開閉し、各スロットの装備中アイテム名を表示する。
//   スロットのボタンをクリックすると、その装備を外して自分の瓶へ戻す（EquipmentHolder.Unequip）。
//   装備する操作は持たない：取り出した装備品はPlayerHandStateが自動装備する（手持ち＝装備の方針）。
//   表示は当面アイテム名テキスト（最小）。3Dモデルのアイコン表示は将来（瓶と同じRenderTexture方式）。
//   ※ Canvas・スロットのButton/TextはInspectorで割り当てる（瓶UI・ミニマップと同じ流儀）。
using System.Collections;
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

    [Header("左カラム＝メインカメラの寄り表示")]
    [SerializeField] private Camera mainCamera;             // 通常プレイのメインカメラ（開いている間だけ左1/3に絞る。未設定ならCamera.main）
    [SerializeField] private TPSCamera tpsCamera;           // 視点のクローズアップ切替（正面寄り）
    [SerializeField] private float closeUpDistance = 2.5f;  // 寄りの距離
    [SerializeField] private float closeUpPitch = 5f;       // 寄りの俯角
    [SerializeField] private float closeUpHeight = 1.2f;    // 注視点の高さ（低くするほどカメラ・画面中央が下がる）
    [SerializeField] private float closeUpFarClip = 8f;     // クローズアップ中の描画距離（キャラ周辺だけ描画＝それ以遠は虚空）

    private bool open;
    public bool IsOpen => open;

    private Rect savedCamRect = new Rect(0f, 0f, 1f, 1f);   // 開く前のメインカメラ描画範囲（閉じたら戻す）
    private float savedFarClip;                              // 開く前の描画距離
    private CameraClearFlags savedClearFlags;                // 開く前のクリア方法
    private Color savedBgColor;                              // 開く前の背景色
    private int savedCullingMask;                            // 開く前の描画レイヤー（自分以外を消すため絞る）
    private bool cullingMaskSaved;                           // ※-1(Everything)が正規値のため、保存済みかはboolで判定
    private readonly Dictionary<EquipmentSlot, ItemData> shown = new Dictionary<EquipmentSlot, ItemData>(); // 前回表示の装備（フラッシュ差分検知用）
    private readonly HashSet<Graphic> flashing = new HashSet<Graphic>(); // 多重フラッシュ防止

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
        // 開いている間は毎フレーム見た目レイヤーを当て直す（取り出し→自動装備で手元の見た目が
        //   作り直されると、新Rendererは元レイヤーのまま＝映らなくなるため。対象は自分の数個のGOで軽い）。
        if (open) CloseUpIsolator.Refresh();

        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
        {
            // 魔族を操作中のCは進化画面（EvolutionUIController）が担当する。人間の装備画面は開かない。
            if (ActivePlayer.Exists && ActivePlayer.Go.GetComponent<DemonCore>() != null) return;
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

    // 装備UI＋自分の瓶（右1/3）を開く。
    public void Open()
    {
        if (open) return;
        open = true;
        if (panel != null) panel.SetActive(true);
        if (equipmentCamera != null) equipmentCamera.enabled = true; // 開いている間だけ装備カメラを有効化

        // 左1/3にメインカメラを絞り、自キャラ正面のクローズアップへ（閉じたら元に戻す）。
        //   描画距離もキャラ周辺まで絞る＝それ以遠は黒い虚空（演出兼・描画コスト削減）。
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam != null)
        {
            savedCamRect = cam.rect;
            savedFarClip = cam.farClipPlane;
            savedClearFlags = cam.clearFlags;
            savedBgColor = cam.backgroundColor;
            cam.rect = new Rect(0f, 0f, 1f / 3f, 1f);
            cam.farClipPlane = closeUpFarClip;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            // 自分以外を消す：自分の見た目をCloseUpViewレイヤーへ移し、カメラはそれだけ映す（真っ黒＋自分）。
            savedCullingMask = cam.cullingMask;
            cullingMaskSaved = true;
            cam.cullingMask = CloseUpIsolator.Mask;
        }
        if (tpsCamera != null) tpsCamera.BeginCloseUp(closeUpDistance, closeUpPitch, closeUpHeight);
        CloseUpIsolator.Isolate(ActivePlayer.Exists ? ActivePlayer.Go : (equipmentHolder != null ? equipmentHolder.gameObject : null));

        SyncShown(); // 開いた瞬間の一斉フラッシュ防止（現状を記録してからRefresh）
        Refresh();   // 最新の装備内容を反映
        if (bottleUI != null)
        {
            bottleUI.SetRightHalf(true); // 装備画面と並べるため瓶を右1/3に
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

        // メインカメラを全画面・元の視点・元の描画距離に戻す。
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam != null)
        {
            cam.rect = savedCamRect;
            cam.farClipPlane = savedFarClip;
            cam.clearFlags = savedClearFlags;
            cam.backgroundColor = savedBgColor;
            if (cullingMaskSaved) { cam.cullingMask = savedCullingMask; cullingMaskSaved = false; }
        }
        if (tpsCamera != null) tpsCamera.EndCloseUp();
        CloseUpIsolator.Restore(); // 自分の見た目レイヤーを元に戻す

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

        // 名前テキスト更新＋装備が変わったスロットはフラッシュ（「取り出す→自動装備」を視覚で伝える）。
        foreach (var sv in slotViews)
        {
            var item = equipmentHolder.Get(sv.slot);
            shown.TryGetValue(sv.slot, out var prev);
            if (!ReferenceEquals(prev, item))
            {
                shown[sv.slot] = item;
                if (open && item != null && sv.button != null && sv.button.image != null)
                    StartCoroutine(FlashSlot(sv.button.image));
            }
            if (sv.label == null) continue;
            sv.label.text = item != null ? item.ItemName : "(空)";
        }
    }

    // 現状の装備を記録だけする（開いた瞬間に全スロットがフラッシュするのを防ぐ）。
    private void SyncShown()
    {
        if (equipmentHolder == null) return;
        foreach (var sv in slotViews) shown[sv.slot] = equipmentHolder.Get(sv.slot);
    }

    // スロット枠を一瞬ハイライトして「装備された」ことを伝える。
    private IEnumerator FlashSlot(Graphic g)
    {
        if (g == null || !flashing.Add(g)) yield break;
        Color baseColor = g.color;
        var highlight = new Color(1f, 0.92f, 0.4f, baseColor.a);
        const float DUR = 0.45f;
        float t = 0f;
        while (t < DUR && g != null)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Abs(2f * (t / DUR) - 1f); // 0→1→0の山なり
            g.color = Color.Lerp(baseColor, highlight, k);
            yield return null;
        }
        if (g != null) g.color = baseColor;
        flashing.Remove(g);
    }

    // スロット枠(UI)の中心位置 → 装備カメラ前のワールド位置に変換する（共通ヘルパー委譲）。
    private Vector3 SlotWorldPosition(RectTransform frame)
    {
        return UIModelProjection.FrameToWorld(frame, rawImageRect, uiCamera, equipmentCamera, depth);
    }
}
