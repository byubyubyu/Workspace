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
    [SerializeField] private bool stripBehaviours = false; // 表示専用化：生成モデルのCollider/Rigidbody/MonoBehaviourを無効化
                                                           //   （部位prefabはPartHurtbox・Hitbox・Motionを同梱するため、進化画面の飾り表示ではtrueにする）
    [SerializeField] private bool centerBounds = false;    // モデルの見た目の中心（Rendererバウンズ）を枠中心に合わせる
                                                           //   （部位prefabは「アンカーからぶら下がる」作りで原点が偏っているため、進化画面ではtrueにする）

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
        if (items == null) return;
        var prefabs = new List<GameObject>(items.Count);
        for (int i = 0; i < items.Count; i++)
            prefabs.Add(items[i] != null ? items[i].Prefab : null);
        UpdateDisplayPrefabs(prefabs, worldPositions, sizes);
    }

    // prefab直接指定版：ItemDataを持たないモデル（魔族の部位prefab等）も同じ流儀で並べられる汎用口。
    //   進化画面（EvolutionUIController）がPartData.partPrefabの表示に使う。
    public void UpdateDisplayPrefabs(IReadOnlyList<GameObject> prefabs, IReadOnlyList<Vector3> worldPositions, IReadOnlyList<float> sizes)
    {
        if (prefabs == null || worldPositions == null) return;
        var instances = new List<GameObject>(prefabs.Count);
        for (int i = 0; i < prefabs.Count; i++)
            instances.Add(prefabs[i] != null ? Instantiate(prefabs[i]) : null);
        UpdateDisplayInstances(instances, worldPositions, sizes);
    }

    // 生成済みインスタンス版：実行時に組み立てたモデル（素体プレビュー等）を渡す汎用口。
    //   渡したGOの所有権はこちらに移る（親付け替え・破棄を行う）。転生画面（DemonBodyPreview）が使う。
    public void UpdateDisplayInstances(IReadOnlyList<GameObject> instances, IReadOnlyList<Vector3> worldPositions, IReadOnlyList<float> sizes)
    {
        if (instances == null || worldPositions == null) return;

        // 既存モデルを一旦すべて消す（在庫変動・スロット数変動を確実に反映）。
        for (int i = 0; i < models.Count; i++)
            if (models[i] != null) Destroy(models[i]);
        models.Clear();

        int n = Mathf.Min(instances.Count, worldPositions.Count);
        for (int i = 0; i < n; i++)
        {
            var model = instances[i];
            if (model == null) { models.Add(null); continue; }

            model.transform.SetParent(transform, false);
            model.transform.position = worldPositions[i];   // UI枠から逆算したワールド位置
            model.transform.localRotation = Quaternion.identity;
            float size = (sizes != null && i < sizes.Count) ? sizes[i] : viewSize;
            ItemViewScaler.FitToSize(model, size);
            SetLayerRecursively(model, gameObject.layer);   // 商人カメラに写るようレイヤーを合わせる
            if (stripBehaviours) StripBehaviours(model);    // 飾り表示：当たり判定・挙動を殺して見た目だけ残す
            if (centerBounds) CenterToBounds(model, worldPositions[i]); // 見た目の中心を枠中心へ
            models.Add(model);
        }
        // 位置数を超えたぶんの渡されたインスタンスは破棄（所有権はこちら）。
        for (int i = n; i < instances.Count; i++)
            if (instances[i] != null) Destroy(instances[i]);
    }

    // 表示を全部消す（パネルを閉じる時に呼ぶ）。
    public void Clear()
    {
        for (int i = 0; i < models.Count; i++)
            if (models[i] != null) Destroy(models[i]);
        models.Clear();
    }

    // モデルの見た目の中心（全Rendererの合成バウンズ中心）が target に来るよう平行移動する。
    private static void CenterToBounds(GameObject go, Vector3 target)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        go.transform.position += target - bounds.center;
    }

    // 表示専用化：Collider無効・Rigidbodyを物理停止・MonoBehaviour無効（Motion等の自走を止める）。
    private static void StripBehaviours(GameObject go)
    {
        foreach (var col in go.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
    }

    // 生成したモデルとその子を、指定レイヤーに設定する（商人カメラのCulling Maskに写すため）。
    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
