// 保存先: Assets/Scripts/Citizen/MerchantTrade.cs
// 商人取引の計算ルール（純ロジック・MonoBehaviourではない。DamageCalculator/NearestFinderと同じ流儀）。
//   ・受け皿（Bottleの物理空間）の中身と商人の在庫/買い取りリストから、
//     「支払い個数」「売却の査定」「買い取り可否」を計算する。
//   ・状態を持たない（すべて引数で受けて結果を返す）＝UI側（MerchantUIController/CeremonyDirector）から
//     呼ばれるだけで、こちらからUIへの参照はない。
using System.Collections.Generic;

public static class MerchantTrade
{
    // 売却の査定結果。RecalculateSell（表示）と SellCeremony（成立処理）の両方が同じ計算を使う。
    public class Appraisal
    {
        public int sellableCount;                                                            // 売れる品の総数
        public readonly Dictionary<ItemData, int> payoutTotal = new Dictionary<ItemData, int>();          // 対価アイテムごとの合計個数
        public readonly List<BottleItemCore> soldCores = new List<BottleItemCore>();                      // 売る実体（受け皿の中の物理アイテム）
        public readonly Dictionary<MerchantStockEntry, int> soldPerEntry = new Dictionary<MerchantStockEntry, int>(); // 買い取りエントリごとの個数
    }

    // 受け皿にある data と同じアイテムの個数（購入の支払いカウントにも使う）。
    public static int CountInTray(Bottle tray, ItemData data)
    {
        if (tray == null || tray.Items == null || data == null) return 0;
        int count = 0;
        for (int i = 0; i < tray.Items.Count; i++)
        {
            var core = tray.Items[i];
            if (core != null && core.Data == data) count++;
        }
        return count;
    }

    // このアイテムを受け皿に乗せられるか（売却の受付可否）。
    //   不可：買い取り対象外（コイン等の通貨・フォールバック無効）／対価が0（priceItem未設定・priceCount<=0）／
    //         受け皿の同種アイテムが既に残り買い取り枠に達している。
    public static bool CanSell(Bottle tray, Merchant merchant, ItemData data)
    {
        if (merchant == null || data == null) return false;
        var entry = merchant.FindBuyEntry(data);
        if (entry == null) return false;
        if (entry.priceItem == null || entry.priceCount <= 0) return false;
        return CountInTray(tray, data) < entry.stock;
    }

    // 受け皿の中身を査定する：買い取り枠はエントリごとに entry.stock 個まで、超過分は査定外。
    //   priceItem 未設定のエントリは対価なし（個数のみ加算）。
    public static Appraisal Appraise(Bottle tray, Merchant merchant)
    {
        var result = new Appraisal();
        if (tray == null || tray.Items == null || merchant == null) return result;

        for (int i = 0; i < tray.Items.Count; i++)
        {
            var core = tray.Items[i];
            if (core == null || core.Data == null) continue;
            var entry = merchant.FindBuyEntry(core.Data);
            if (entry == null) continue;
            result.soldPerEntry.TryGetValue(entry, out int used);
            if (used >= entry.stock) continue; // 枠超過分は査定外
            result.soldPerEntry[entry] = used + 1;
            result.sellableCount++;
            result.soldCores.Add(core);
            if (entry.priceItem != null && entry.priceCount > 0)
            {
                result.payoutTotal.TryGetValue(entry.priceItem, out int total);
                result.payoutTotal[entry.priceItem] = total + entry.priceCount;
            }
        }
        return result;
    }
}
