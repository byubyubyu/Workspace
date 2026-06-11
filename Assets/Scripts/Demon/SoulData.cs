// 保存先: Assets/Scripts/Demon/SoulData.cs
// 魂ポイントの獲得ルールSO（GDDセクション15・転生式）。
//   換算・倍率の数値はすべてここに置く（SOはシリアライズフィールドのみ・ロジックなし）。
//   倍率をどう適用するかの判断は DemonSoul（コンポーネント）側が行う。
using UnityEngine;

[CreateAssetMenu(fileName = "SoulData", menuName = "Project/Demon/SoulData")]
public class SoulData : ScriptableObject
{
    public float pointsPerNutrition = 1f; // 捕食した肉量1あたりの魂ポイント
    public float humanWeight = 2f;        // 人間側（Team有り＝兵士等）の死体の倍率
    public float wildWeight = 1f;         // 野生（Team.None＝モンスター）の死体の倍率

    // 将来：食べた身体の「格」による倍率テーブル（大量獲得）、特殊解放条件
}
