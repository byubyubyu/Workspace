// 保存先: Assets/Scripts/Player/AgingData.cs
// 加齢の設定SO（GDDセクション15・人間側）。レート・倍率カーブ・段階ラベルの数値をすべてここに置く。
//   時間源の換算（1歳=何秒）もここ＝将来昼夜サイクル等に差し替える時はこのSOとAgeだけ見ればよい。
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AgingData", menuName = "Project/Player/AgingData")]
public class AgingData : ScriptableObject
{
    // 年齢の段階ラベル1件（fromAge歳から先はこのラベル。表示・ログ用）。
    [System.Serializable]
    public class Stage
    {
        public float fromAge;
        public string label;
    }

    public float startAge = 18f;        // 開始年齢
    public float secondsPerYear = 120f; // 1歳に何秒かかるか（実時間。仮：2分）
    public AnimationCurve multiplierByAge = AnimationCurve.Linear(18f, 1f, 80f, 0.6f); // 年齢→能力倍率（カーブで自由に調整）
    public List<Stage> stages = new List<Stage>(); // 段階ラベル（壮年/中年/老年。fromAge昇順）
}
