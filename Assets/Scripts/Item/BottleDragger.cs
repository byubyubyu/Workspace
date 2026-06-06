// 保存先: Assets/Scripts/Item/BottleDragger.cs
// 瓶の中を漁る操作役。瓶UIが開いている間だけ働く（BottleUIControllerがenabledを制御）。
//   ・新Input Systemで直接マウスを読む（Mouse.current）。
//   ・マウス位置→瓶の2D物理空間の座標に変換（瓶カメラのScreenToWorldPoint。基本形）。
//     ※ 瓶UI(RawImage)が画面の一部に表示される場合のRawImage内座標の厳密対応は⑥で調整する。
//   ・押下時：その座標に Physics2D.OverlapPoint で掴むBottleItemCoreを特定。
//   ・掴んだら TargetJoint2D を動的追加し、マウス位置へバネで引っ張る（重さ・摩擦が手応えになる）。
//   ・離したら TargetJoint2D を外す（アイテムは瓶に落ちて戻る＝ReleaseDragging）。
//   ・掴んでいる間はそのアイテムを「ドラッグ中」状態にする（口から出た後の分岐の判定材料）。
//     口から出た後の使用/こぼれ分岐自体は Bottle 側（外側ゾーン＋状態）が行う。
//
//   バネ強度（frequency/dampingRatio）は仮値。実際の手応えは実装後にチューニングする。
using UnityEngine;
using UnityEngine.InputSystem;

public class BottleDragger : MonoBehaviour
{
    [SerializeField] private Camera bottleCamera; // 瓶の2D物理空間を撮る専用カメラ
    [SerializeField] private RectTransform bottleViewRect; // 瓶映像を表示するRawImageのRectTransform（座標変換用）

    [Header("バネ（TargetJoint2D）の仮設定・実装後に調整")]
    [SerializeField] private float jointFrequency = 5f;
    [SerializeField] private float jointDampingRatio = 1f;

    private BottleItemCore grabbed;     // 今掴んでいるアイテム
    private TargetJoint2D activeJoint;  // 掴んでいるアイテムに付けたジョイント

    private void OnDisable()
    {
        // UIが閉じる等で無効化されたら、掴みを安全に解除する。
        Release();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // マウス位置を瓶の2D物理空間に変換。
        //   worldPos：RawImage外でも縁にクランプした座標（ドラッグ中のtarget用。吹っ飛び防止）。
        //   inside  ：RawImage内かどうか（掴み開始の判定用。外では掴ませない）。
        Vector2 worldPos;
        bool inside = ScreenToBottleWorld(mouse.position.ReadValue(), out worldPos);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (inside) TryGrab(worldPos);
        }
        else if (mouse.leftButton.isPressed && grabbed != null && activeJoint != null)
        {
            // ドラッグ中：目標をマウス位置（クランプ済み）に更新。
            //   RawImage外に出ても、縁にクランプされた座標へ引っ張る。
            //   RawImage内の上端＝瓶の口の外に対応するので、上へ引けば取り出しできる。
            activeJoint.target = worldPos;
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            Release();
        }
    }

    // マウス画面座標 → 瓶の2D物理空間のワールド座標。
    //   RawImage(BottleView)内の割合（viewport）を求め、瓶カメラの ViewportToWorldPoint で変換する。
    //   Canvasが Screen Space - Overlay のため、ScreenPointToLocalPointInRectangle のcameraはnull。
    //   ★worldPos は viewport を 0〜1 にクランプしてから変換する（RawImage外でも縁で止まる＝吹っ飛び防止）。
    //   戻り値 inside：クランプ前の生の割合が 0〜1 の内側だったか（掴み開始の可否に使う）。
    private bool ScreenToBottleWorld(Vector2 screenPos, out Vector2 worldPos)
    {
        worldPos = Vector2.zero;
        if (bottleCamera == null || bottleViewRect == null) return false;

        // RawImage内のローカル座標を求める（Overlayなのでcamera=null）。
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                bottleViewRect, screenPos, null, out local))
        {
            // 変換自体に失敗した場合も、最低限 worldPos は瓶中心付近を返しておく（吹っ飛ばない）。
            return false;
        }

        // ローカル座標 → 0〜1の割合（viewport）。
        Rect rect = bottleViewRect.rect;
        float rawVx = (local.x - rect.x) / rect.width;
        float rawVy = (local.y - rect.y) / rect.height;

        bool inside = (rawVx >= 0f && rawVx <= 1f && rawVy >= 0f && rawVy <= 1f);

        // ★target用：0〜1にクランプ（はみ出した分は縁で止める）。
        float vx = Mathf.Clamp01(rawVx);
        float vy = Mathf.Clamp01(rawVy);

        Vector3 w = bottleCamera.ViewportToWorldPoint(new Vector3(vx, vy, bottleCamera.nearClipPlane));
        worldPos = new Vector2(w.x, w.y);

        return inside;
    }

    private void TryGrab(Vector2 worldPos)
    {
        // その点に重なる全Colliderを取得する。
        //   OverlapPoint（1つだけ）だと、壁・ゾーン・背景・他アイテムのうちどれが返るか不定で、
        //   アイテム以外を拾うと掴めない（密に重なった所で再現）。全部取ってアイテムだけ選ぶ。
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        if (hits == null || hits.Length == 0) return;

        BottleItemCore best = null;
        float bestY = float.MinValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;
            var core = col.GetComponentInParent<BottleItemCore>();
            if (core == null) continue; // 壁・ゾーン・背景などは無視

            // 複数アイテムが重なっている場合は、一番上（Y座標が高い）を優先（漁る＝上から取る感覚）。
            float y = core.transform.position.y;
            if (y > bestY)
            {
                bestY = y;
                best = core;
            }
        }

        if (best == null) return; // アイテムが無ければ掴まない

        grabbed = best;
        grabbed.MarkDragging();

        var rb = grabbed.GetComponent<Rigidbody2D>();
        if (rb == null) { grabbed = null; return; }

        activeJoint = grabbed.gameObject.AddComponent<TargetJoint2D>();
        activeJoint.autoConfigureTarget = false;
        activeJoint.target = worldPos;
        activeJoint.frequency = jointFrequency;
        activeJoint.dampingRatio = jointDampingRatio;
        // anchor は掴んだローカル点にしてもよいが、まずは中心(0,0)で素直に引っ張る。
        activeJoint.anchor = Vector2.zero;
    }

    private void Release()
    {
        if (activeJoint != null)
        {
            Destroy(activeJoint);
            activeJoint = null;
        }
        if (grabbed != null)
        {
            grabbed.ReleaseDragging(); // 瓶に落ちて戻る（収納済みへ）
            grabbed = null;
        }
    }
}
