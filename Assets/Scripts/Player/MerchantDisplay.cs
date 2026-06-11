// 保存先: Assets/Scripts/Player/MerchantDisplay.cs
// 商人UIの3Dモデル表示用の裏空間（装備UIのEquipmentDisplay相当・物理なし）。
//   MerchantUIControllerが各スロット枠のワールド位置を計算して渡し、そこに在庫品の3Dモデル(ItemData.Prefab)を生成する。
//   専用カメラがここを撮ってRenderTexture→商人パネルのRawImageに映す（瓶・装備と同じ流儀）。
//   見た目サイズは ItemViewScaler で揃える。レイヤーはこのMerchantDisplayに合わせる（商人カメラのCulling Mask用）。
using System.Collections.Generic;
using UnityEngine;

public class MerchantDisplay : MonoBehaviour
{
    [SerializeField] private float viewSize = 1f; // 表示モデルの目標サイズ（最大辺・カメラ画角に合わせて調整）

    // ListView / DetailView で大きさを切り替えるため、外から設定可能にする。
    public float ViewSize { get => viewSize; set => viewSize = value; }

    // 各スロット位置に今出ている見た目モデル（作り直し用に保持）。indexはStockのindexに対応。
    private readonly List<GameObject> models = new List<GameObject>();

    // 在庫の品＋各スロットのワールド位置に合わせてモデルを生成し直す。
    //   itemsとworldPositionsは同じ長さ・同じindexで対応する（呼び出し側が揃える）。
    public void UpdateDisplay(IReadOnlyList<ItemData> items, IReadOnlyList<Vector3> worldPositions)
    {
        UpdateDisplay(items, worldPositions, null);
    }

    // sizes 指定版：indexごとに表示サイズを変えられる（例：商品は大きく・支払いアイテムは小さく）。
    //   sizes が null または要素不足のindexは viewSize を使う。
    public void UpdateDisplay(IReadOnlyList<ItemData> items, IReadOnlyList<Vector3> worldPositions, IReadOnlyList<float> sizes)
    {
        if (items == null || worldPositions == null) return;

        // 既存モデルを一旦すべて消す（在庫変動・スロット数変動を確実に反映）。
        for (int i = 0; i < models.Count; i++)
            if (models[i] != null) Destroy(models[i]);
        models.Clear();

        int n = Mathf.Min(items.Count, worldPositions.Count);
        for (int i = 0; i < n; i++)
        {
            var item = items[i];
            if (item == null || item.Prefab == null) { models.Add(null); continue; }

            var model = Instantiate(item.Prefab, transform);
            model.transform.position = worldPositions[i];   // UI枠から逆算したワールド位置
            model.transform.localRotation = Quaternion.identity;
            float size = (sizes != null && i < sizes.Count) ? sizes[i] : viewSize;
            ItemViewScaler.FitToSize(model, size);
            SetLayerRecursively(model, gameObject.layer);   // 商人カメラに写るようレイヤーを合わせる
            models.Add(model);
        }
    }

    // 表示を全部消す（パネルを閉じる時に呼ぶ）。
    public void Clear()
    {
        for (int i = 0; i < models.Count; i++)
            if (models[i] != null) Destroy(models[i]);
        models.Clear();
    }

    // 生成したモデルとその子を、指定レイヤーに設定する（商人カメラのCulling Maskに写すため）。
    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
