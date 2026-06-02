// 保存先: Assets/Scripts/Common/DamageCalculator.cs
// ダメージ計算を一元化するstaticクラス。受け手(Core)が自分のdefenseを渡して呼ぶ。
//   式: Max(0, attackPower - defense)。負値にならないようガードする。
//   将来: クリティカル・防御貫通・属性などをここに集約して差し替えやすくする
//        （必要ならStrategy化の余地。今は単純な減算式）。
using UnityEngine;

public static class DamageCalculator
{
    public static float Calc(float attackPower, float defense)
    {
        return Mathf.Max(0f, attackPower - defense);
    }
}
