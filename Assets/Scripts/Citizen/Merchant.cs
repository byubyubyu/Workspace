// 保存先: Assets/Scripts/Citizen/Merchant.cs
// 商人（市民の一種）。固定位置に立ち、プレイヤーが話しかけて売買する相手（徘徊しない＝Wanderを付けない）。
//   段階3：MerchantDataから品揃えを受け取り、実行時在庫として保持する（買うと減る）。
//   在庫の実体はここが持つ（SOは設定値。SO直接編集はアセットを汚すので、生成時に複製してこちらで増減する）。
//   売買のお金のやり取り・UI表示は後段（このクラスは在庫の保持・公開・増減APIまで）。
using System.Collections.Generic;
using UnityEngine;

public class Merchant : MonoBehaviour
{
    // 実行時の在庫（MerchantDataの品揃えを複製したもの。残数はここで増減する）。
    private readonly List<MerchantStockEntry> stock = new List<MerchantStockEntry>();

    // UI等が読む在庫一覧（品・売値・残数を含む）。
    public IReadOnlyList<MerchantStockEntry> Stock => stock;

    // CitizenCoreから配られる。品揃えを実行時在庫として複製して持つ。
    public void Initialize(MerchantData data)
    {
        stock.Clear();
        if (data == null) return;
        var goods = data.Goods;
        if (goods == null) return;

        for (int i = 0; i < goods.Count; i++)
        {
            var g = goods[i];
            if (g == null || g.item == null) continue;
            // SOの値を複製（残数を独立に減らすため。元のSOは書き換えない）。
            stock.Add(new MerchantStockEntry
            {
                item = g.item,
                priceItem = g.priceItem,
                priceCount = g.priceCount,
                stock = g.stock,
            });
        }
    }

    // 在庫を1つ減らす（購入確定時に段階3で呼ぶ）。残数が無ければ何もせず false。
    public bool TryConsumeStock(MerchantStockEntry entry)
    {
        if (entry == null || entry.stock <= 0) return false;
        entry.stock--;
        return true;
    }
}
