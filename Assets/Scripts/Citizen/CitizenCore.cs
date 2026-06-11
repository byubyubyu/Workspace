// 保存先: Assets/Scripts/Citizen/CitizenCore.cs
// 市民のハブ（兵士のMinionCoreに相当する非戦闘版）。Teamを持ち、Wander等のコンポーネントへデータを配る。
//   戦闘には一切関与しない（IBattleInfo非実装・Hurtboxなし・タグ"Citizen"で兵士の視界対象外）。
using UnityEngine;

public class CitizenCore : MonoBehaviour
{
    // 生存中の全市民（自己申告レジストリ）。検索側（ItemPicker等）はNearestFinder.Findで選ぶ。
    public static readonly System.Collections.Generic.List<CitizenCore> All = new System.Collections.Generic.List<CitizenCore>();

    public Team Team { get; private set; }

    private void OnEnable() { All.Add(this); }
    private void OnDisable() { All.Remove(this); } // 破棄時も呼ばれる＝解除漏れなし

    // 生成後に呼ぶ。データ・Team・徘徊範囲(homeBase)を各コンポーネントへ配る。
    public void Initialize(CitizenData data, Team team, Base homeBase)
    {
        Team = team;

        var wander = GetComponent<Wander>();
        if (wander != null) wander.Initialize(data, homeBase);

        // スキル個体値（婚活・遺伝用）。データに生成ルールがあれば押し込む（無ければ個体値なし市民）。
        var skills = GetComponent<CitizenSkills>();
        if (skills != null) skills.Initialize(data.SkillData);

        // 商人なら品揃えを配る（Merchantコンポーネントが付いていて、データが商人種別(MerchantData)のとき）。
        var merchant = GetComponent<Merchant>();
        if (merchant != null && data is MerchantData md) merchant.Initialize(md);
    }
}
