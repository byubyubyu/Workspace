using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 装備UI（C画面）のホットスポット表示。
//   キャラのボーン位置をメインカメラで画面投影し、部位マーカー（○）を毎フレーム追従させる。
//   タグ（装備名）は画面の固定位置に置き、マーカーとタグを引き出し線で結ぶ。
//   マーカークリック＝外す（EquipmentHolder.Unequip）。武器スロットも表示だけは扱う。
//   GaugeAttacher（頭上ゲージ）と同じ「ワールド→スクリーン投影」方式＝キャラが回転しても追従する。
public class EquipmentHotspotView : MonoBehaviour
{
    [Serializable]
    public class SlotMarker
    {
        public EquipmentSlot slot;
        public HumanBodyBones bone = HumanBodyBones.Head;
        public Vector3 worldOffset;        // ボーンからのずらし（ワールド・体の中心寄せ等）
        public RectTransform marker;       // 部位マーカー（○・Button付き）
        public RectTransform tagRect;      // 固定タグ（装備名）
        public RectTransform line;         // 引き出し線（pivot=(0,0.5)のImage）
        public Text label;                 // タグ内のテキスト
        public Button button;              // マーカーのクリック（外す）
        public Button tagButton;           // タグのクリックでも外せる（任意）
    }

    [SerializeField] private EquipmentUIController controller;  // 開閉状態の参照
    [SerializeField] private EquipmentHolder equipmentHolder;
    [SerializeField] private Camera mainCamera;                 // 左カラムのクローズアップを映しているカメラ（未設定ならCamera.main）
    [SerializeField] private Animator bodyAnimator;             // ボーン位置の供給元（SoldierVisual）
    [SerializeField] private RectTransform canvasRect;          // 親Canvas（座標変換用）
    [SerializeField] private GameObject layerRoot;              // マーカー一式の親（開いている間だけ表示）
    [SerializeField] private List<SlotMarker> markers = new List<SlotMarker>();
    [SerializeField] private float behindAlpha = 0.25f;         // カメラから見て裏側の部位のマーカー透明度

    private bool shownLastFrame;

    private void Start()
    {
        if (layerRoot != null) layerRoot.SetActive(false);
        // クリック＝外す（マーカーは押した瞬間の表示物なのでクロージャでslotを束縛）
        foreach (var m in markers)
        {
            var slot = m.slot;
            if (m.button != null) m.button.onClick.AddListener(() => equipmentHolder.Unequip(slot));
            if (m.tagButton != null) m.tagButton.onClick.AddListener(() => equipmentHolder.Unequip(slot));
        }
    }

    private void LateUpdate()
    {
        bool open = controller != null && controller.IsOpen;
        if (layerRoot != null && open != shownLastFrame) layerRoot.SetActive(open);
        shownLastFrame = open;
        if (!open) return;

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null || bodyAnimator == null) return;

        foreach (var m in markers)
        {
            Transform bone = bodyAnimator.GetBoneTransform(m.bone);
            if (bone == null || m.marker == null) continue;

            Vector3 world = bone.position + bodyAnimator.transform.TransformDirection(m.worldOffset);
            Vector3 screen = cam.WorldToScreenPoint(world);
            bool visible = screen.z > 0f;
            m.marker.gameObject.SetActive(visible);
            if (m.line != null) m.line.gameObject.SetActive(visible);
            if (!visible) continue;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
            m.marker.anchoredPosition = local;

            // 裏側の部位（体の中心より奥）は薄く
            float bodyZ = cam.WorldToScreenPoint(bodyAnimator.transform.position + Vector3.up).z;
            float alpha = screen.z > bodyZ + 0.05f ? behindAlpha : 1f;
            var img = m.marker.GetComponent<Image>();
            if (img != null) { var c = img.color; c.a = alpha; img.color = c; }

            // タグの内容
            if (m.label != null)
            {
                ItemData item = equipmentHolder != null ? equipmentHolder.Get(m.slot) : null;
                m.label.text = item != null ? item.ItemName : "（空）";
                m.label.color = item != null ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }

            // 引き出し線：タグの端（マーカー側の縁）→マーカー
            if (m.line != null && m.tagRect != null)
            {
                Vector2 from = m.tagRect.anchoredPosition;
                Vector2 to = local;
                from.x += Mathf.Sign(to.x - from.x) * m.tagRect.sizeDelta.x * 0.5f; // 中心ではなく縁から出す
                Vector2 diff = to - from;
                m.line.anchoredPosition = from;
                m.line.sizeDelta = new Vector2(diff.magnitude, m.line.sizeDelta.y);
                m.line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
            }
        }
    }
}
