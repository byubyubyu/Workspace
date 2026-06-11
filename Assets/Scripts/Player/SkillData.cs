// 保存先: Assets/Scripts/Player/SkillData.cs
// 人間スキル1種ぶんのSO（GDDセクション15・UO型スキル）。
//   ・効果（ポイントあたりの補正）と成長レートの数値をすべてここに置く（SOはフィールドのみ・ロジックなし）。
//   ・成長トリガーもenumでなく数値で表現する：
//       gainOnHit     … 攻撃を当てた時に伸びる量（剣技=大・肉体=小・防御=0）
//       gainOnDamaged … 被弾した時に伸びる量（防御=大・肉体=小・剣技=0）
//     ＝どのスキルが何で伸びるかをデータだけで差し替えられる（コード分岐なし）。
using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Project/Player/SkillData")]
public class SkillData : ScriptableObject
{
    public string skillName;

    [Header("分類")]
    public bool isBodily;             // 肉体系か（true=加齢の影響を受ける。技術系=falseは老いても落ちない）

    [Header("ポイントあたりの効果（実効値×係数）")]
    public float hpPerPoint;          // 最大HP加算
    public float attackPerPoint;      // 攻撃力加算（武器装備時の実威力に乗る）
    public float damageCutPerPoint;   // 被ダメ軽減（防御計算後のダメージから引く・フラット）

    [Header("成長量（1イベントあたり。値が高いほど自然に鈍化する）")]
    public float gainOnHit;           // 攻撃命中で伸びる量
    public float gainOnDamaged;       // 被弾で伸びる量
}
