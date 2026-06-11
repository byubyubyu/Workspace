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

    [Header("買い取りリスト（item=買い取る品 / priceItem×priceCount=1個あたりの支払い / stock=買い取り上限数）")]
    [SerializeField] private MerchantStockEntry[] buyGoods;

    [Header("リスト外買い取り（buyGoodsに無い品の一律買い取り。priceItem未設定ならリスト外は買い取らない。通貨(CurrencyValue>0)は常に対象外）")]
    [SerializeField] private ItemData unlistedPriceItem;   // リスト外買い取りの対価（例：ItemData_Coin）
    [SerializeField] private int unlistedPriceCount = 1;   // 1個あたりの支払い個数（例：1＝一律1G）

    [Header("見た目（顔イラスト3点セット。Normalのみでも可＝他はNormalにフォールバック）")]
    [SerializeField] private Texture2D portraitNormal; // 通常時
    [SerializeField] private Texture2D portraitHappy;  // 購入成立時（喜び）
    [SerializeField] private Texture2D portraitSad;    // キャンセル時（悲しみ）

    [Header("セリフ（売買UIの左下に出す簡易テキスト）")]
    [SerializeField] private string lineGreeting = "いらっしゃいませ";
    [SerializeField] private string lineThanks = "ありがとう！";
    [SerializeField] private string lineCancel = "やめちゃうの；；";
    [SerializeField] private string lineRefuse = "それは買い取れません！"; // 買い取れない品を出された時

    public IReadOnlyList<MerchantStockEntry> Goods => goods;
    public IReadOnlyList<MerchantStockEntry> BuyGoods => buyGoods;
    public ItemData UnlistedPriceItem => unlistedPriceItem;
    public int UnlistedPriceCount => unlistedPriceCount;
    public Texture2D PortraitNormal => portraitNormal;
    public Texture2D PortraitHappy => portraitHappy != null ? portraitHappy : portraitNormal;
    public Texture2D PortraitSad => portraitSad != null ? portraitSad : portraitNormal;
    public string LineGreeting => lineGreeting;
    public string LineThanks => lineThanks;
    public string LineCancel => lineCancel;
    public string LineRefuse => lineRefuse;
}
