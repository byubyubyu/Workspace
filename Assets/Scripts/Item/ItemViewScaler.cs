// 保存先: Assets/Scripts/Item/ItemViewScaler.cs
// 見た目モデル（3D）の大きさを、目標サイズに「等比（形を保ったまま）」で合わせる共通ヘルパー。
//   元モデルの大きさはまちまち（丸太でも栗でも可）。boundsを測って、最大辺が目標サイズになるよう拡縮する。
//   マップ用・瓶用の両Coreから呼ぶ（重複回避。DamageCalculatorと同じstaticヘルパーの流儀）。
//   ※ 形の比率は保つ（最大辺基準）。当たり判定（四角/丸）と見た目の形は完全一致しないが、
//     リアルを求めない方針なので「だいたい収まる」で許容。おかしければ目標サイズ側で微調整する。
using UnityEngine;

public static class ItemViewScaler
{
    // view（生成済みの見た目モデル）の最大辺が targetSize になるよう、等比スケールを適用する。
    //   targetSize：合わせたい大きさ（ワールド単位）。瓶用はItemData.Sizeの最大辺、マップ用はmapViewSize。
    public static void FitToSize(GameObject view, float targetSize)
    {
        if (view == null || targetSize <= 0f) return;

        // 子も含めた全Rendererのboundsを合算して、モデルの実寸を測る。
        var renderers = view.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // 最大辺を求める。
        Vector3 size = bounds.size;
        float maxEdge = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        if (maxEdge <= 0.0001f) return; // 測れない（極小）なら何もしない

        // 等比スケール：最大辺が targetSize になる倍率を、現在のスケールに掛ける。
        float factor = targetSize / maxEdge;
        view.transform.localScale *= factor;
    }
}
