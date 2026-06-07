// 保存先: Assets/Scripts/Citizen/CitizenCore.cs
// 市民のハブ（兵士のMinionCoreに相当する非戦闘版）。Teamを持ち、Wander等のコンポーネントへデータを配る。
//   戦闘には一切関与しない（IBattleInfo非実装・Hurtboxなし・タグ"Citizen"で兵士の視界対象外）。
using UnityEngine;

public class CitizenCore : MonoBehaviour
{
    public Team Team { get; private set; }

    // 生成後に呼ぶ。データ・Team・徘徊範囲(homeBase)を各コンポーネントへ配る。
    public void Initialize(CitizenData data, Team team, Base homeBase)
    {
        Team = team;

        var wander = GetComponent<Wander>();
        if (wander != null) wander.Initialize(data, homeBase);

        // 商人なら品揃えを配る（Merchantコンポーネントが付いていて、データが商人種別(MerchantData)のとき）。
        var merchant = GetComponent<Merchant>();
        if (merchant != null && data is MerchantData md) merchant.Initialize(md);
    }
}
