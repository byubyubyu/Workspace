// 保存先: Assets/Scripts/Item/ItemData.cs
// アイテムの種類SO（1枚集約）。物理パラメータ・見た目・効果差込口を持つ。
//   マップ用(MapItemCore)・瓶用(BottleItemCore)の両方がこの同じItemDataを参照する。
//   形(Shape)はItemShape、サイズ(Size)は当たり判定の寸法。生成時にCoreがこれを見て物理を組む。
//   効果(effect)はnull可（効果なしのアイテムも作れる）。中身は将来。
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Project/Item/ItemData")]
public class ItemData : ScriptableObject, IItemData
{
    [Header("識別")]
    [SerializeField] private string itemName;

    [Header("物理の姿")]
    [SerializeField] private ItemShape shape = ItemShape.Box;
    [SerializeField] private Vector2 size = Vector2.one; // Circle:x=直径 / Box・Long:(幅,高さ)
    [SerializeField] private float mass = 1f;
    [SerializeField] private float friction = 0.4f;

    [Header("見た目（マップ・瓶で共通）")]
    [SerializeField] private GameObject prefab;

    [Header("見た目の大きさ合わせ")]
    // マップでの見た目の目標サイズ（最大辺・ワールド単位）。視認性基準。
    //   物によらず一定の大きさに揃えるため。全アイテム同じ値にすれば揃う。個別調整も可。
    [SerializeField] private float mapViewSize = 1f;
    // 瓶での見た目の目標サイズは ItemData.Size の最大辺から自動導出する（当たり判定に合わせる）。
    //   別途微調整したくなったら、ここに bottleViewSize を足す（今は不要）。

    [Header("効果（差込口・null可）")]
    [SerializeField] private ItemEffect effect;

    public string ItemName => itemName;
    public ItemShape Shape => shape;
    public Vector2 Size => size;
    public float Mass => mass;
    public float Friction => friction;
    public GameObject Prefab => prefab;
    public float MapViewSize => mapViewSize;
    // 瓶での見た目目標サイズ＝Sizeの最大辺（当たり判定に合わせる）。
    public float BottleViewSize => Mathf.Max(size.x, size.y);
    public ItemEffect Effect => effect;
}
