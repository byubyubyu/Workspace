// 保存先: Assets/Scripts/Player/MerchantStockSlot.cs
// 商人パネルの「在庫1件ぶんのスロット」。MinimapQuotaRow流儀の動的生成行。
//   ・itemFrame：売る品の3Dモデル枠。MerchantUIControllerがこの位置からMerchantDisplayへ
//     ItemData.Prefabを生成して重ねる。
//   ・priceFrame：支払いアイテムの3Dモデル枠。同様に裏空間にpriceItemのモデルを並べる。
//   ・priceCountLabel：支払い必要個数（「x5」など）。在庫Textと同様、数値の添え物。
//   ・stockLabel：残在庫数。
//   ・buyButton：買う（段階3-1は表示のみ・支払い実装は3-2）。
//   品も支払いも見た目は3Dモデルで見せる方針（UIグラフィカル方針）。
using UnityEngine;
using UnityEngine.UI;

public class MerchantStockSlot : MonoBehaviour
{
    [SerializeField] private RectTransform itemFrame;    // 売る品の3Dモデル表示枠
    [SerializeField] private RectTransform priceFrame;   // 支払いアイテムの3Dモデル表示枠
    [SerializeField] private Text priceCountLabel;       // 支払い必要個数（「x5」）
    [SerializeField] private Text stockLabel;            // 残在庫数（「x3」）
    [SerializeField] private Button buyButton;           // 買う（段階3-1は表示のみ）

    public RectTransform ItemFrame => itemFrame;
    public RectTransform PriceFrame => priceFrame;
    public Button BuyButton => buyButton;
    public MerchantStockEntry Entry { get; private set; }

    // MerchantUIControllerが生成時に呼ぶ。個数・在庫を表示し、ボタンの押下時コールバックを差し込む。
    public void Setup(MerchantStockEntry entry, System.Action<MerchantStockEntry> onBuy)
    {
        Entry = entry;
        Refresh();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            if (onBuy != null && entry != null)
                buyButton.onClick.AddListener(() => onBuy(entry));
        }
    }

    // 在庫数や個数を最新値で表示し直す（購入後の在庫減などで呼ばれる想定）。
    public void Refresh()
    {
        if (Entry == null) return;
        if (priceCountLabel != null) priceCountLabel.text = $"x{Entry.priceCount}";
        if (stockLabel != null) stockLabel.text = $"x{Entry.stock}";
        if (buyButton != null) buyButton.interactable = Entry.stock > 0;
    }

    // 受付中フラグと累計支払い個数の表示切替（MerchantUIControllerが呼ぶ）。
    //   受付中：priceCountLabelを「2/5」表示に。
    //   非受付：priceCountLabelを通常表示（Refreshに任せる）。
    public void SetPayingState(bool isPaying, int paidCount)
    {
        if (Entry == null || priceCountLabel == null) return;
        if (isPaying) priceCountLabel.text = $"{paidCount}/{Entry.priceCount}";
        // 非受付時はRefreshで通常表示が反映済み（MerchantUIController側でRefresh→SetPayingStateの順に呼ぶ）。
    }
}
