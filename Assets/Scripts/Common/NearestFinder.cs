// 保存先: Assets/Scripts/Common/NearestFinder.cs
// 「リストから最寄りの1件を選ぶ」共通ユーティリティ（Resource/ModifiableStatと並ぶ純粋C#の道具）。
//   自己申告レジストリ（MapItemCore.All / Corpse.All / CitizenCore.All 等）と組で使う：
//   物理のOverlapSphereを使わないので、自分の体のコライダーや地形がリストに混ざる問題が構造的に起きない。
//   破棄済み（偽null）・range超・filter不一致は除外する。
using System;
using System.Collections.Generic;
using UnityEngine;

public static class NearestFinder
{
    public static T Find<T>(IReadOnlyList<T> list, Vector3 from, float range, Func<T, bool> filter = null) where T : Component
    {
        T best = null;
        float bestSq = range * range;
        for (int i = 0; i < list.Count; i++)
        {
            var candidate = list[i];
            if (candidate == null) continue; // 破棄済み（偽null）ガード
            if (filter != null && !filter(candidate)) continue;
            float d = (candidate.transform.position - from).sqrMagnitude;
            if (d <= bestSq)
            {
                bestSq = d;
                best = candidate;
            }
        }
        return best;
    }
}
