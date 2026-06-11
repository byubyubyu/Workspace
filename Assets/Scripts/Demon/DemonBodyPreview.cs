// 保存先: Assets/Scripts/Demon/DemonBodyPreview.cs
// 素体の表示専用プレビューを組み立てる静的ヘルパー（転生画面が使う）。
//   素体prefab（DemonSkeleton）はアンカーだけの骨格なので、そのまま表示しても何も見えない。
//   DemonCore.AssembleParts と同じ手順で「骨格＋各スロットの初期部位prefab」を組み立てて返す
//   （表示専用＝PartHurtboxへのデータ押し込みはしない。挙動の無効化は表示側のstripBehavioursに任せる）。
//   返したGOの所有権は呼び出し側（MerchantDisplay.UpdateDisplayInstances＝親付け替え・破棄を行う）。
using UnityEngine;

public static class DemonBodyPreview
{
    public static GameObject Build(BodyData body)
    {
        if (body == null || body.BodyPrefab == null) return null;
        var root = Object.Instantiate(body.BodyPrefab);
        root.name = $"BodyPreview_{body.BodyName}";

        foreach (var slot in body.Slots)
        {
            if (slot == null || slot.initialPart == null || slot.initialPart.partPrefab == null) continue;
            if (slot.partObjectNames == null) continue;
            foreach (var anchorName in slot.partObjectNames)
            {
                var anchor = FindDeep(root.transform, anchorName);
                if (anchor == null)
                {
                    Debug.LogWarning($"[DemonBodyPreview] アンカーが見つからない: {anchorName}（素体: {body.BodyName}）");
                    continue;
                }
                Object.Instantiate(slot.initialPart.partPrefab, anchor, false);
            }
        }
        return root;
    }

    // 名前で子孫Transformを探す（DemonCore.FindDeepと同じ。表示専用側に閉じた小さな複製）。
    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
