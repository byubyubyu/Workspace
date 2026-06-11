// 保存先: Assets/Scripts/Minion/AccumulatedStagger.cs
// 蓄積式ひるみ（大型モンスター用）。通常のひるみ(Stagger)と違い、被弾のひるみ値を蓄積し、
//   閾値に達した時だけ「大ひるみ」を1回発動して蓄積をリセットする。
//   ・小刻みな被弾ではひるまない＝集団に殴られてもひるみハメにならない
//   ・大ひるみ中は蓄積しない（発動の連鎖を防ぐ）
//   Staggerを継承するので、MinionCore（GetComponent<Stagger>）・StaggerState は無変更で動く。
//   兵士は従来の Stagger のまま＝挙動不変。
using UnityEngine;

public class AccumulatedStagger : Stagger
{
    [SerializeField] private float threshold = 5f;           // 蓄積がこれに達したら大ひるみ（兵士の通常攻撃=0.5 × 10発）
    [SerializeField] private float bigStaggerDuration = 2f;  // 大ひるみの長さ（秒）

    private float accumulated; // 現在の蓄積値

    // 外から設定し直す（魔族プレイヤーの形態切替用。野生モンスターはプレハブのSerializeFieldのまま）。
    public void Configure(float threshold, float bigStaggerDuration)
    {
        this.threshold = threshold;
        this.bigStaggerDuration = bigStaggerDuration;
        accumulated = 0f;
    }

    public override void Apply(float duration)
    {
        if (duration <= 0f) return;
        if (IsStaggered) return; // 大ひるみ中は蓄積しない

        accumulated += duration;
        if (accumulated >= threshold)
        {
            accumulated = 0f;
            remainingTime = Mathf.Max(remainingTime, bigStaggerDuration); // 大ひるみ発動
        }
    }
}
