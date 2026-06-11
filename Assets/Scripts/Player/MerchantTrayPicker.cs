// 保存先: Assets/Scripts/Player/MerchantTrayPicker.cs
// 受け皿(tray)に置かれた商品のクリック判定役。取引の儀式の最終段で使う。
//   ・MerchantUIControllerがWatch(商品)で起動し、クリックされたらコールバック→StopWatch。
//   ・座標変換はBottleDraggerと同じ流儀（TrayRawImage内の割合→trayCameraのViewportToWorldPoint）。
//   ・対象の商品そのものをクリックした時だけ反応する（壁・コイン残り等は無視）。
using UnityEngine;
using UnityEngine.InputSystem;

public class MerchantTrayPicker : MonoBehaviour
{
    [SerializeField] private Camera trayCamera;        // 受け皿を撮る専用カメラ
    [SerializeField] private RectTransform trayViewRect; // 受け皿映像のRawImage（座標変換用）

    private readonly System.Collections.Generic.List<BottleItemCore> targets = new System.Collections.Generic.List<BottleItemCore>(); // クリック待ちの対象（商品1個 or 売却対価の複数枚）
    private System.Action<BottleItemCore> onPicked;

    private void Awake()
    {
        enabled = false; // Watchされるまで眠る
    }

    // 商品coreのクリック監視を開始する。
    public void Watch(BottleItemCore core, System.Action<BottleItemCore> picked)
    {
        targets.Clear();
        if (core != null) targets.Add(core);
        onPicked = picked;
        enabled = true;
    }

    // 複数coreのどれか1個のクリックを待つ（売却の対価＝コイン複数枚の一括受け取り用）。
    public void Watch(System.Collections.Generic.List<BottleItemCore> cores, System.Action<BottleItemCore> picked)
    {
        targets.Clear();
        if (cores != null)
            for (int i = 0; i < cores.Count; i++)
                if (cores[i] != null) targets.Add(cores[i]);
        onPicked = picked;
        enabled = true;
    }

    public void StopWatch()
    {
        targets.Clear();
        onPicked = null;
        enabled = false;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || targets.Count == 0) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (!ScreenToTrayWorld(mouse.position.ReadValue(), out Vector2 world)) return;

        var hits = Physics2D.OverlapPointAll(world);
        for (int i = 0; i < hits.Length; i++)
        {
            var core = hits[i] != null ? hits[i].GetComponentInParent<BottleItemCore>() : null;
            if (core != null && targets.Contains(core))
            {
                onPicked?.Invoke(core);
                return;
            }
        }
    }

    // マウス画面座標 → 受け皿の2D物理空間のワールド座標（RawImage外ならfalse）。
    private bool ScreenToTrayWorld(Vector2 screenPos, out Vector2 worldPos)
    {
        worldPos = Vector2.zero;
        if (trayCamera == null || trayViewRect == null) return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(trayViewRect, screenPos, null, out Vector2 local))
            return false;

        Rect rect = trayViewRect.rect;
        float vx = (local.x - rect.x) / rect.width;
        float vy = (local.y - rect.y) / rect.height;
        if (vx < 0f || vx > 1f || vy < 0f || vy > 1f) return false;

        Vector3 w = trayCamera.ViewportToWorldPoint(new Vector3(vx, vy, trayCamera.nearClipPlane));
        worldPos = new Vector2(w.x, w.y);
        return true;
    }
}
