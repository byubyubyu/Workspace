// 保存先: Assets/Scripts/Citizen/MerchantData.cs
// 商人の種類SO。CitizenData（見た目prefab・徘徊速度）を継承し、売り物リスト（品・売値・在庫）を足す。
//   兵士のMinionDataに対する商人版。このSOは「設定値」。実行時の在庫変動はMerchantが別途コピーして持つ
//   （SO直接編集はアセットを永続的に汚すため避ける）。
using System.Collections.Generic;
using UnityEngine;

// 売り物1件＝品(item)・支払い物(priceItem×priceCount)・初期在庫数(stock)。Inspectorで商人ごとに並べる。
//   支払いは「お金専用」を作らず物々交換と一本化：priceItemにコイン(ItemData_Coin)を入れれば「コインN枚で買う」、
//   priceItemに木材を入れれば「木材N個で買う」を同じ仕組みで表せる（段階3）。
[System.Serializable]
public class MerchantStockEntry
{
    public ItemData item;        // 売る品
    public ItemData priceItem;   // 支払いに要求するアイテム（例：ItemData_Coin）
    public int priceCount = 1;   // priceItemの必要個数
    public int stock = 1;        // 初期在庫数（買うと減る）
}

[CreateAssetMenu(fileName = "MerchantData", menuName = "Project/Citizen/MerchantData")]
public class MerchantData : CitizenData
{
    [Header("品揃え")]
    [SerializeField] private MerchantStockEntry[] goods;

    public IReadOnlyList<MerchantStockEntry> Goods => goods;
}
