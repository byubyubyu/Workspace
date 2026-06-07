// 保存先: Assets/Scripts/Player/EquipmentDisplay.cs
// 装備UIの3Dモデル表示用の裏空間（瓶のBottleに相当する装備版・物理なし）。
//   EquipmentUIControllerが各スロットの「ワールド位置」を計算して渡し、そこに装備品の3Dモデル(ItemData.Prefab)を生成する。
//   （UI枠を自由配置→枠位置から逆算したワールド位置にモデルを置く方式。アンカーの手動配置・等間隔は不要）
//   専用カメラ(EquipmentUIControllerが制御)でここを撮ってRenderTexture→左半分のRawImageに映す。
//   見た目サイズは ItemViewScaler で揃える。レイヤーはこのEquipmentDisplayに合わせる（装備カメラのCulling Mask用）。
using System.Collections.Generic;
using UnityEngine;

public class EquipmentDisplay : MonoBehaviour
{
    [SerializeField] private float viewSize = 1f; // 表示モデルの目標サイズ（最大辺・カメラ画角に合わせて調整）

    // 各スロットに今出ている見た目モデル（作り直し用に保持）。
    private readonly Dictionary<EquipmentSlot, GameObject> models = new Dictionary<EquipmentSlot, GameObject>();

    // 装備内容＋各スロットのワールド位置に合わせてモデルを生成し直す。
    //   worldPositions：スロット→このカメラ前のワールド位置（EquipmentUIControllerがUI枠から逆算して渡す）。
    public void UpdateDisplay(EquipmentHolder holder, Dictionary<EquipmentSlot, Vector3> worldPositions)
    {
        if (holder == null || worldPositions == null) return;

        // 既存モデルを一旦すべて消す（装備変更・位置変更を確実に反映）。
        foreach (var kv in models)
            if (kv.Value != null) Destroy(kv.Value);
        models.Clear();

        foreach (var pair in worldPositions)
        {
            var slot = pair.Key;
            var item = holder.Get(slot);
            if (item == null || item.Prefab == null) continue;

            var model = Instantiate(item.Prefab, transform);
            model.transform.position = pair.Value;        // UI枠から逆算したワールド位置
            model.transform.localRotation = Quaternion.identity;
            ItemViewScaler.FitToSize(model, viewSize);
            SetLayerRecursively(model, gameObject.layer); // 装備カメラに写るようレイヤーを合わせる
            models[slot] = model;
        }
    }

    // 生成したモデルとその子を、指定レイヤーに設定する（装備カメラのCulling Maskに写すため）。
    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
