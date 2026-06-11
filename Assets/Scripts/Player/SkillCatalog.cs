// 保存先: Assets/Scripts/Player/SkillCatalog.cs
// スキルカタログ（マスターSO）。全スキル（SkillData）の一覧と合計キャップを束ねる。
//   スキルID＝このリストの番号（マルチ方針：全クライアント同梱の静的データを番号で参照。BodyCatalogと同型）。
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillCatalog", menuName = "Project/Player/SkillCatalog")]
public class SkillCatalog : ScriptableObject
{
    [SerializeField] private List<SkillData> skills = new List<SkillData>();
    [SerializeField] private float totalTrainCap = 100f; // 鍛えた分の合計キャップ（遺伝値はカウントしない・UO型）
    [SerializeField] private float maxSkillValue = 100f; // 1スキルの実効値上限

    public IReadOnlyList<SkillData> Skills => skills;
    public float TotalTrainCap => totalTrainCap;
    public float MaxSkillValue => maxSkillValue;
}
