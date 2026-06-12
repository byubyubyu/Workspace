// 保存先: Assets/Scripts/Player/MerchantCeremonyDirector.cs
// 取引の儀式（演出コルーチン）役。MerchantUIControllerから分離した「演出＋中断精算」コンポーネント。
//   ・購入：手がコインを1枚ずつ回収 → 商品を受け皿にコトン → プレイヤーがクリックして瓶へ。
//   ・売却：手が売却品を1個ずつ回収 → 対価（コイン等）を受け皿にバッと置く → クリックで全額瓶へ。
//   ・途中で売買UIが閉じられた時の精算（AbortAndSettle）：
//       - 回収の途中 → 不成立扱い：回収済みの支払いコイン（購入）／売却品（売却）を全額PendingItemsへ返す
//       - 回収完了〜対価設置前 → 成立扱い：購入なら商品、売却なら未設置の対価をPendingItemsで補填
//       - 設置後 → 受け皿に実体があるので Controller 側の RefundTrayToPending が瓶へ戻す
//   ・Controllerとの境界：開始（Begin*）と中断（AbortAndSettle）だけ呼ばれ、進行はStarted/Finishedイベントで通知する。
//     瓶への投入は Controller の DropItemToBottle（hot/pending振り分け）へ委譲する。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MerchantCeremonyDirector : MonoBehaviour
{
    [Header("連携（同じMerchantUISystem内の兄弟）")]
    [SerializeField] private MerchantUIController controller;   // IsOpen参照と瓶投入（DropItemToBottle）の委譲先
    [SerializeField] private MerchantEmotionView emotionView;   // 成立時の喜び顔
    [SerializeField] private MerchantWalletView walletView;     // 成立時の所持金更新
    [SerializeField] private MerchantDisplay merchantDisplay;   // 購入時「見本を引っ込めて実物を置く」

    [Header("受け皿・生成")]
    [SerializeField] private Bottle tray;                       // 商人の受け皿
    [SerializeField] private BottleItemFactory bottleItemFactory; // 受け皿への実体生成役
    [SerializeField] private MerchantTrayPicker trayPicker;     // 受け皿に置かれた品のクリック判定役

    [Header("手の演出")]
    [SerializeField] private Transform handVisual;              // 商人の手（未設定なら手の演出を飛ばす）
    [SerializeField] private float ceremonyStepInterval = 0.25f; // コイン1枚回収ごとの間（秒）
    [SerializeField] private float handMoveDuration = 0.25f;     // 手の移動1回あたりの秒数
    [SerializeField] private float payoutScatter = 1.2f;         // 売却対価を置く時の中央からの散らし幅（受け皿の横方向）
    [SerializeField] private float payoutDropInterval = 0.06f;   // 対価1個ごとの間（小刻み＝バッとまとめて置く感じ）

    public event System.Action Started;   // 儀式開始（Controller：購入/キャンセルボタンをロック）
    public event System.Action Finished;  // 儀式完走（Controller：ボタン解除・ListViewへ復帰）

    private Coroutine routine;
    private MerchantStockEntry entry;     // 儀式中のエントリ（購入の中断補填用）
    private bool paid;                    // 回収完了〜対価設置前の区間か（中断時は成立扱いで補填）
    private readonly List<ItemData> consumed = new List<ItemData>(); // 回収済みの支払いコイン/売却品（回収途中の中断＝不成立扱いで全額返却）
    private readonly List<ItemData> payout = new List<ItemData>();   // 売却の未払い対価（設置前の中断＝成立扱いで補填）

    public bool IsRunning => routine != null;

    // 購入の儀式を開始する（確定ボタン押下から。多重起動はControllerが弾く）。
    public void BeginPurchase(MerchantStockEntry target, Merchant merchant)
    {
        if (routine != null || target == null) return;
        Started?.Invoke();
        routine = StartCoroutine(PurchaseCeremony(target, merchant));
    }

    // 売却の儀式を開始する。売れる物が無ければ何もしない（査定ビューに留まる）。
    public void BeginSell(Merchant merchant)
    {
        if (routine != null) return;
        // 査定スナップショット：受け皿の中身から「売る実体」と「エントリごとの個数」を確定。
        var appraisal = MerchantTrade.Appraise(tray, merchant);
        if (appraisal.soldCores.Count == 0) return;
        Started?.Invoke();
        routine = StartCoroutine(SellCeremony(merchant, appraisal));
    }

    // 儀式の途中で売買UIが閉じられた時の中断精算。精算したら true（Controllerがボタンを解除する）。
    public bool AbortAndSettle(InventoryHolder holder)
    {
        if (routine == null) return false;
        StopCoroutine(routine);
        routine = null;
        if (trayPicker != null) trayPicker.StopWatch();
        HideHand();
        if (holder != null)
        {
            if (consumed.Count > 0)
            {
                // 回収途中の中断＝不成立扱い：回収済みの支払いコイン（購入）／売却品（売却）を全て返す。
                foreach (var item in consumed)
                    if (item != null) holder.PendingItems.Add(item);
            }
            else if (paid)
            {
                // 回収完了〜対価設置前の中断＝成立扱い：購入なら商品、売却なら未設置の対価を補填。
                if (payout.Count > 0)
                {
                    foreach (var pay in payout)
                        if (pay != null) holder.PendingItems.Add(pay);
                }
                else if (entry != null && entry.item != null)
                {
                    holder.PendingItems.Add(entry.item);
                }
            }
        }
        consumed.Clear();
        payout.Clear();
        entry = null;
        paid = false;
        return true;
    }

    // 購入の儀式：手がコインを1枚ずつ回収 → 商品を受け皿にコトン → プレイヤーがクリックして瓶へ。
    private IEnumerator PurchaseCeremony(MerchantStockEntry target, Merchant merchant)
    {
        entry = target;

        // 1) 手がコインを1枚ずつ回収（演出。手が無ければ間隔だけ刻む）。
        //    受け皿の変動でControllerが再計算しないのはOnTrayChanged側のIsRunningガードによる。
        int toConsume = target.priceCount;
        if (toConsume > 0 && target.priceItem != null && tray != null)
        {
            var coins = new List<BottleItemCore>();
            foreach (var c in tray.Items)
                if (c != null && c.Data == target.priceItem && coins.Count < toConsume) coins.Add(c);

            foreach (var coin in coins)
            {
                if (coin == null) continue;
                yield return MoveHand(coin.transform.position);
                consumed.Add(coin.Data); // 回収途中で閉じられたらキャンセル扱いで返金する
                tray.Unregister(coin);
                Destroy(coin.gameObject);
                yield return new WaitForSeconds(ceremonyStepInterval);
            }
        }
        paid = true;        // ここから商品設置までは「支払い完了・商品未設置」区間（中断時は商品を補填）
        consumed.Clear();   // 全額回収完了＝もう返金しない（以降の中断は商品補填で精算）

        // 2) 在庫を減らし、商品を受け皿にコトンと置く。
        if (merchant != null) merchant.TryConsumeStock(target);
        BottleItemCore product = null;
        if (target.item != null && tray != null && bottleItemFactory != null)
        {
            Vector3 dropPos = tray.GetRandomDropPosition(); // 散らして置く（コインの山の上に積まない）
            // 見本（拡大表示）はこのタイミングで下げる＝「見本を引っ込めて実物を置く」受け渡し感。
            if (merchantDisplay != null) merchantDisplay.Clear();
            yield return MoveHand(dropPos);
            product = bottleItemFactory.Create(target.item, dropPos, Quaternion.identity, tray.transform);
            if (product != null)
            {
                product.Initialize(target.item);
                tray.Register(product);
                paid = false; // 受け皿に実体が出来た＝以降の中断はRefundTrayToPendingが面倒を見る
            }
        }
        HideHand();
        if (merchant != null) emotionView?.Show(merchant.PortraitHappy, merchant.LineThanks); // 喜び顔（毎度あり!）
        walletView?.Refresh();
        Debug.Log($"[MerchantUI] 購入成立: {target.item?.ItemName}（残在庫 {target.stock}）→ 受け皿の商品をクリックで受け取り");

        // 3) 受け皿の商品クリック待ち → 瓶へ。
        if (product != null && trayPicker != null)
        {
            bool picked = false;
            trayPicker.Watch(product, _ => picked = true);
            while (!picked && controller != null && controller.IsOpen) yield return null;
            trayPicker.StopWatch();
            if (product != null) // Close済みならRefund側で処理済み（Destroyされている）
            {
                var data = product.Data;
                tray.Unregister(product);
                Destroy(product.gameObject);
                if (controller != null) controller.DropItemToBottle(data);
            }
        }
        else if (target.item != null)
        {
            // 受け皿に置けなかった（factory未設定等）場合のフォールバック：従来通り直接瓶へ。
            if (controller != null) controller.DropItemToBottle(target.item);
            paid = false;
        }

        entry = null;
        routine = null;
        Finished?.Invoke();
    }

    // 売却の儀式：手が売却品を1個ずつ回収 → 対価（コイン等）を受け皿にコトン → クリックで全額瓶へ。
    //   PurchaseCeremony の逆向き。中断時の精算は AbortAndSettle 参照。
    private IEnumerator SellCeremony(Merchant merchant, MerchantTrade.Appraisal appraisal)
    {
        // 1) 手が売却品を1個ずつ回収（不成立中断に備えて回収分を記録）。
        foreach (var core in appraisal.soldCores)
        {
            if (core == null) continue;
            yield return MoveHand(core.transform.position);
            consumed.Add(core.Data);
            tray.Unregister(core);
            Destroy(core.gameObject);
            yield return new WaitForSeconds(ceremonyStepInterval);
        }

        // 2) 成立：買い取り枠を消費し、対価リストを確定（以降の中断は対価補填で精算）。
        consumed.Clear();
        payout.Clear();
        foreach (var pair in appraisal.soldPerEntry)
        {
            for (int i = 0; i < pair.Value; i++)
            {
                if (merchant != null) merchant.TryConsumeBuyStock(pair.Key);
                if (pair.Key.priceItem != null)
                    for (int j = 0; j < pair.Key.priceCount; j++) payout.Add(pair.Key.priceItem);
            }
        }
        paid = true;

        // 3) 手が中央に1回来て、対価をまとめてバッと置く（中央±payoutScatter で少しだけ散らす）。
        //    置けた分は payout から外す＝中断補填と重複させない。
        var payoutCores = new List<BottleItemCore>();
        if (tray != null && bottleItemFactory != null && payout.Count > 0)
        {
            Vector3 center = tray.GetDropPosition();
            yield return MoveHand(center);
            var payoutSnapshot = new List<ItemData>(payout);
            foreach (var pay in payoutSnapshot)
            {
                Vector3 dropPos = center + tray.transform.right * Random.Range(-payoutScatter, payoutScatter);
                var core = bottleItemFactory.Create(pay, dropPos, Quaternion.identity, tray.transform);
                if (core != null)
                {
                    core.Initialize(pay);
                    tray.Register(core);
                    payoutCores.Add(core);
                    payout.Remove(pay);
                }
                if (payoutDropInterval > 0f) yield return new WaitForSeconds(payoutDropInterval);
            }
        }
        paid = false;
        // 置けなかった分（factory未設定等）は直接瓶へフォールバック。
        if (payout.Count > 0)
        {
            foreach (var pay in payout)
                if (controller != null) controller.DropItemToBottle(pay);
            payout.Clear();
        }

        HideHand();
        if (merchant != null) emotionView?.Show(merchant.PortraitHappy, merchant.LineThanks); // 喜び顔（良い物をありがとう!）
        walletView?.Refresh();
        Debug.Log($"[MerchantUI] 売却成立: {appraisal.soldCores.Count}個 → 受け皿の対価をクリックで受け取り");

        // 4) 対価のクリック待ち（どれか1枚クリックで全額まとめて瓶へ）。
        if (payoutCores.Count > 0 && trayPicker != null)
        {
            bool picked = false;
            trayPicker.Watch(payoutCores, _ => picked = true);
            while (!picked && controller != null && controller.IsOpen) yield return null;
            trayPicker.StopWatch();
            if (controller != null && controller.IsOpen)
            {
                foreach (var core in payoutCores)
                {
                    if (core == null) continue; // Close済みならRefund側で処理済み
                    var data = core.Data;
                    tray.Unregister(core);
                    Destroy(core.gameObject);
                    controller.DropItemToBottle(data);
                }
            }
        }

        routine = null;
        Finished?.Invoke();
    }

    // 商人の手を target へ滑らかに移動させる（未設定なら何もしない）。
    //   Zはコインより手前(カメラ寄り)に固定して、手のスプライトが上に描かれるようにする。
    private IEnumerator MoveHand(Vector3 target)
    {
        if (handVisual == null) yield break;
        target.z = -1f;
        if (!handVisual.gameObject.activeSelf)
        {
            handVisual.gameObject.SetActive(true);
            handVisual.position = (tray != null ? tray.GetDropPosition() : target) + Vector3.up * 0.5f + Vector3.forward * -1f;
        }
        Vector3 from = handVisual.position;
        float t = 0f;
        while (t < handMoveDuration)
        {
            t += Time.deltaTime;
            handVisual.position = Vector3.Lerp(from, target, Mathf.Clamp01(t / handMoveDuration));
            yield return null;
        }
        handVisual.position = target;
    }

    private void HideHand()
    {
        if (handVisual != null) handVisual.gameObject.SetActive(false);
    }
}
