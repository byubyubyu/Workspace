// 保存先: Assets/Scripts/Common/UIModelProjection.cs
// UI枠（RectTransform）の中心位置 → 裏空間カメラ前のワールド位置に変換する共通ヘルパー。
//   「枠位置にItemData.Prefab等の3Dモデルを置き、専用カメラ→RenderTexture→RawImageで映す」
//   グラフィカルUI方式（装備・商人・進化）の座標変換部。3クラスで同じ式が重複していたため抽出。
//   変換の流れ：枠の画面座標 → RawImage内ローカル → 正規化(=viewport) → 表示カメラのViewportToWorldPoint。
using UnityEngine;

public static class UIModelProjection
{
    // frame        … モデルを重ねたいUI枠
    // rawImageRect … RenderTextureを映しているRawImageの矩形（変換基準）
    // uiCamera     … Canvasのカメラ（Screen Space Overlayならnull）
    // displayCamera… 裏空間を撮っている専用カメラ
    // depth        … displayCameraからモデルを置く奥行き
    public static Vector3 FrameToWorld(RectTransform frame, RectTransform rawImageRect,
                                       Camera uiCamera, Camera displayCamera, float depth)
    {
        Vector3 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, frame.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, screen, uiCamera, out Vector2 local);
        Rect rect = rawImageRect.rect;
        float vx = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float vy = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
        return displayCamera.ViewportToWorldPoint(new Vector3(vx, vy, depth));
    }
}
