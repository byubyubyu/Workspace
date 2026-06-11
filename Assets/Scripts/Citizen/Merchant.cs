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

    // 実行時の買い取り枠（MerchantDataの買い取りリストを複製したもの。stock=残り買い取り可能数）。
    private readonly List<MerchantStockEntry> buyStock = new List<MerchantStockEntry>();

    // UI等が読む在庫一覧（品・売値・残数を含む）。
    public IReadOnlyList<MerchantStockEntry> Stock => stock;

    // UI等が読む買い取り枠一覧。
    public IReadOnlyList<MerchantStockEntry> BuyStock => buyStock;

    // リスト外買い取りの対価（SOから受け取る。null＝リスト外は買い取らない）。
    private ItemData unlistedPriceItem;
    private int unlistedPriceCount;

    // 売買UIに出す顔イラスト（SOから受け取る。未設定ならnull＝UI側で非表示）。
    //   Happy/Sad は SO側で Normal にフォールバック済み。
    public Texture2D PortraitNormal { get; private set; }
    public Texture2D PortraitHappy { get; private set; }
    public Texture2D PortraitSad { get; private set; }

    // セリフ（売買UIの左下に出す簡易テキスト）。
    public string LineGreeting { get; private set; }
    public string LineThanks { get; private set; }
    public string LineCancel { get; private set; }
    public string LineRefuse { get; private set; }

    // CitizenCoreから配られる。品揃えを実行時在庫として複製して持つ。
    public void Initialize(MerchantData data)
    {
        stock.Clear();
        buyStock.Clear();
        PortraitNormal = data != null ? data.PortraitNormal : null;
        PortraitHappy = data != null ? data.PortraitHappy : null;
        PortraitSad = data != null ? data.PortraitSad : null;
        LineGreeting = data != null ? data.LineGreeting : "";
        LineThanks = data != null ? data.LineThanks : "";
        LineCancel = data != null ? data.LineCancel : "";
        LineRefuse = data != null ? data.LineRefuse : "";
        unlistedPriceItem = data != null ? data.UnlistedPriceItem : null;
        unlistedPriceCount = data != null ? data.UnlistedPriceCount : 0;
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

        // 買い取りリストも同様に複製（stock=残り買い取り可能数として減らしていく）。
        var buyGoods = data.BuyGoods;
        if (buyGoods == null) return;
        for (int i = 0; i < buyGoods.Count; i++)
        {
            var g = buyGoods[i];
            if (g == null || g.item == null) continue;
            buyStock.Add(new MerchantStockEntry
            {
                item = g.item,
                priceItem = g.priceItem,
                priceCount = g.priceCount,
                stock = g.stock,
            });
        }
    }

    // このアイテムを買い取れるか（買い取りリストにあり、残り枠が1以上）。買い取れるならEntryを返す。
    //   リストに無い品は「リスト外買い取り」（unlistedPriceItem 設定時のみ）：
    //   実行時エントリ（枠は実質無制限）を合成して buyStock にキャッシュし、以降は通常エントリと同じに扱う。
    //   通貨アイテム（CurrencyValue>0）は対象外＝コインをコインで買い取る無限ループを防ぐ。
    public MerchantStockEntry FindBuyEntry(ItemData item)
    {
        if (item == null) return null;
        for (int i = 0; i < buyStock.Count; i++)
        {
            var e = buyStock[i];
            if (e != null && e.item == item && e.stock > 0) return e;
        }

        // リスト外買い取り（一律価格）。リストに載っていて枠切れの品は合成しない（上限の意味を保つ）。
        if (unlistedPriceItem == null) return null;
        if (item.CurrencyValue > 0) return null;
        for (int i = 0; i < buyStock.Count; i++)
            if (buyStock[i] != null && buyStock[i].item == item) return null; // 枠切れの既存エントリ

        var synth = new MerchantStockEntry
        {
            item = item,
            priceItem = unlistedPriceItem,
            priceCount = unlistedPriceCount,
            stock = int.MaxValue,
        };
        buyStock.Add(synth);
        return synth;
    }

    // 買い取り枠を1つ消費する（売却成立時に呼ぶ）。枠が無ければ何もせず false。
    public bool TryConsumeBuyStock(MerchantStockEntry entry)
    {
        if (entry == null || entry.stock <= 0) return false;
        entry.stock--;
        return true;
    }

    // 在庫を1つ減らす（購入確定時に段階3で呼ぶ）。残数が無ければ何もせず false。
    public bool TryConsumeStock(MerchantStockEntry entry)
    {
        if (entry == null || entry.stock <= 0) return false;
        entry.stock--;
        return true;
    }
}
