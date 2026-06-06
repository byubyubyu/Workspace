// 保存先: Assets/Scripts/Item/IItemData.cs
// アイテムの種類データの窓口。マップ用(MapItemCore)・瓶用(BottleItemCore)の両方がこれを参照する。
//   物理パラメータ（形・サイズ・質量・摩擦）と見た目(prefab)を直接持ち、効果だけ差込口(ItemEffect)。
//   ※ アイテムは値が少ないため分割せず1枚(ItemData)に集約する。将来肥大化すれば分割に育てる。
using UnityEngine;

public interface IItemData
{
    ItemShape Shape { get; }      // 形（丸／四角／細長）
    Vector2 Size { get; }         // 当たり判定の寸法。Circleはx=直径として扱う / Box・Longは(幅,高さ)
    float Mass { get; }           // 質量（重いほど崩れ・扱いにくさに影響）
    float Friction { get; }       // 摩擦（低い=ツルツル滑る／高い=ゴツゴツ引っかかる）
    GameObject Prefab { get; }    // 見た目の3Dモデル（マップ用・瓶用で共通使用）
    float MapViewSize { get; }    // マップでの見た目の目標サイズ（最大辺・視認性基準）
    float BottleViewSize { get; } // 瓶での見た目の目標サイズ（Sizeの最大辺＝当たり判定に合わせる）
    ItemEffect Effect { get; }    // 使用時の効果（差込口・null可＝効果なし）
}
