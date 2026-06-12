// 保存先: Assets/Scripts/Player/MerchantWalletView.cs
// 所持金表示役（MerchantUIControllerから分離したウォレットコンポーネント）。
//   所持金＝プレイヤーのインベントリ全体の currencyValue 合計（コインは1個ずつ1Gなど）。
//   ・瓶UIが開いてる時は Bottle.Items（物理実体）が真の中身、閉じてる時は Records がスナップショット。
//   ・PendingItems は「まだ瓶に積まれてない初期分」なので常に加算。
//   ・商人UI中は瓶も開きっぱなしの設計なので通常は Bottle.Items から集計する経路を通る。
//   SetActive(true) で瓶の変化（OnItemsChanged）を購読してリアルタイム更新、false で解除。
using UnityEngine;
using UnityEngine.UI;

public class MerchantWalletView : MonoBehaviour
{
    [SerializeField] private Text walletLabel;             // プレイヤーの所持金表示
    [SerializeField] private Bottle playerBottle;          // 開いている間の真の中身（物理実体）
    [SerializeField] private InventoryHolder playerHolder; // 閉じている時のスナップショット（Records）集計用

    private bool active;

    // 操作中プレイヤーのインベントリ（陣営選択後はActivePlayer。未設定時は従来のシーン参照にフォールバック）。
    private InventoryHolder OwnHolder => ActivePlayer.Holder != null ? ActivePlayer.Holder : playerHolder;

    // 売買UIの開閉に合わせて呼ぶ：true=購読開始＋即時更新／false=購読解除。
    public void SetActive(bool value)
    {
        if (active == value) return;
        active = value;
        if (playerBottle != null)
        {
            if (value) playerBottle.OnItemsChanged += Refresh;
            else playerBottle.OnItemsChanged -= Refresh;
        }
        if (value) Refresh();
    }

    public void Refresh()
    {
        if (walletLabel == null) return;
        int gold = 0;
        bool isLiveBottle = playerBottle != null && playerBottle.Items != null && playerBottle.Items.Count > 0;
        if (isLiveBottle)
        {
            var items = playerBottle.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i]?.Data;
                if (d != null && d.CurrencyValue > 0) gold += d.CurrencyValue;
            }
        }
        else if (OwnHolder != null)
        {
            var records = OwnHolder.Records;
            for (int i = 0; i < records.Count; i++)
            {
                var d = records[i].data;
                if (d != null && d.CurrencyValue > 0) gold += d.CurrencyValue;
            }
        }
        if (OwnHolder != null)
        {
            var pending = OwnHolder.PendingItems;
            for (int i = 0; i < pending.Count; i++)
            {
                var d = pending[i];
                if (d != null && d.CurrencyValue > 0) gold += d.CurrencyValue;
            }
        }
        walletLabel.text = $"{gold}G";
    }
}
