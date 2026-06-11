// 保存先: Assets/Scripts/Minion/PartData.cs
// 部位の設定SO。部位ごとの被ダメ倍率・部位HP・破壊時のひるみボーナス・再生時間に加え、
//   ステータス補正（魔族の「装備」相当）・付与ワザ・部位ごとの進化ツリーを持つ。
//   同じ部位（例：脚×4）は1つのSOを共有する（実行時の部位HPはPartHurtbox側が個別に持つ）。
//   野生モンスターは被ダメ系のみ使う（補正・ワザ・進化はDemonCoreだけが集計する。MinionData由来のステータスが正）。
using System.Collections.Generic;
using UnityEngine;

// 部位の進化先候補1件＝行き先の部位＋必要な捕食ポイント。Inspectorで部位ごとに並べる（分岐ツリー）。
[System.Serializable]
public class PartEvolutionOption
{
    public PartData target; // 進化先の部位
    public float cost = 30f; // 必要な捕食ポイント
}

[CreateAssetMenu(fileName = "PartData", menuName = "Project/Minion/PartData")]
public class PartData : ScriptableObject
{
    public string partName;              // 表示名（頭・脚など。進化UIで使う）
    public int tier = 1;                 // 進化段階。素体（BodyData.maxPartTier）がこれの上限を決める

    [Header("見た目（部位prefab。PartHurtbox＋必要ならHitbox・Motionを同梱）")]
    public GameObject partPrefab;        // 素体骨格のアンカーに生成される。魔族の組み立て用（野生モンスターは固定体のため未使用）

    [Header("被ダメ（PartHurtboxが使う）")]
    public float damageMultiplier = 1f;  // 被ダメ倍率（頭2.0／尻尾0.5など。防御計算の前に攻撃力へ乗算）
    public float partHp = 0f;            // 部位HP（0=破壊不可。倍率適用後の攻撃力ぶん減る）
    public float breakStaggerBonus = 0f; // 破壊時に蓄積ひるみへ加算する値（閾値以上なら即大ひるみ）
    public float regenTime = 60f;        // 破壊から再生までの秒数（将来：睡眠で短縮）

    [Header("ステータス補正（装備相当。素体の基礎値に加算。魔族のみ集計）")]
    public float hpBonus;
    public float defenseBonus;
    public float attackPowerBonus;
    public float moveSpeedBonus;

    [Header("付与ワザ（この部位が解放するワザ。スロット順に連結され技番号になる）")]
    public List<AttackMove> grantedMoves = new List<AttackMove>();

    [Header("進化先候補（部位ごとの分岐ツリー。空＝これ以上進化しない）")]
    public List<PartEvolutionOption> evolutions = new List<PartEvolutionOption>();
}
