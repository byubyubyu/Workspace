// 保存先: Assets/Scripts/Citizen/CitizenSkillData.cs
// 市民のスキル個体値の生成ルールSO（GDDセクション15・婚活の土台）。
//   市民は生成時にここの範囲でランダムにスキル個体値を持つ（＝配偶者として遺伝式の母側に効く）。
//   CitizenData（種類SO）がこれを束ねる（マスターSO＋サブSOの規約）。
using UnityEngine;

[CreateAssetMenu(fileName = "CitizenSkillData", menuName = "Project/Citizen/CitizenSkillData")]
public class CitizenSkillData : ScriptableObject
{
    public SkillCatalog catalog;   // スキル一覧（人間プレイヤーと同じカタログ＝スキルIDが揃う）
    public int skillCountMin = 1;  // 個体値を持つスキルの数（ランダム範囲）
    public int skillCountMax = 2;
    public float valueMin = 10f;   // 個体値の値（ランダム範囲）
    public float valueMax = 50f;
}
