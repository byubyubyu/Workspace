// 保存先: Assets/Scripts/Citizen/CitizenSkills.cs
// 市民のスキル個体値（GDDセクション15・婚活の「相手の値」の実体）。
//   生成時（CitizenCore.Initialize経由）にCitizenSkillDataの範囲でランダムにロールして保持する。
//   遺伝式 (父実効＋母個体値)÷2×減衰率 の母側として Family が読む。
//   将来：職業との連動（鍛冶屋の家系は鍛冶が高い等）・婚活UIでの表示。
using UnityEngine;

public class CitizenSkills : MonoBehaviour
{
    private float[] values; // スキルIDごとの個体値（カタログと同じ並び）

    public int SkillCount => values != null ? values.Length : 0;
    public float GetValue(int skillId) =>
        values != null && skillId >= 0 && skillId < values.Length ? values[skillId] : 0f;

    // CitizenCore.Initializeから押し込まれる（市民が自分で設定を読みにいかない）。
    public void Initialize(CitizenSkillData data)
    {
        if (data == null || data.catalog == null)
        {
            values = new float[0];
            return;
        }
        int count = data.catalog.Skills.Count;
        values = new float[count];
        if (count == 0) return;

        // ランダムに1〜2個（SO設定範囲）のスキルへ個体値を振る。
        int rolls = Random.Range(data.skillCountMin, data.skillCountMax + 1);
        for (int r = 0; r < rolls; r++)
        {
            int skillId = Random.Range(0, count);
            values[skillId] = Mathf.Max(values[skillId], Random.Range(data.valueMin, data.valueMax));
        }
    }
}
