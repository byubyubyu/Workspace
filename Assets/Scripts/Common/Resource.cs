// 保存先: Assets/Scripts/Common/Resource.cs
// 「数値の器」。現在値・最大値と、増減・判定だけを持つ純粋C#クラス。
// 時間回復・ダメージ計算・完成イベント等の固有の振る舞いは持たない（各用途ラッパーが担う）。
using UnityEngine;

public class Resource
{
    private float current;
    private float max;

    public float Current => current;
    public float Max => max;

    // max: 器の上限。startCurrent: 開始時の現在値（0〜maxにクランプ）。
    public Resource(float max, float startCurrent)
    {
        this.max = max;
        current = Mathf.Clamp(startCurrent, 0f, max);
    }

    // 増やす。結果を0〜maxにクランプする（負値を渡した場合もクランプ結果を許容）。
    public void Add(float amount)
    {
        current = Mathf.Clamp(current + amount, 0f, max);
    }

    // 払えるなら消費してtrue、払えなければ消費せずfalse（アトミック）。
    public bool Consume(float amount)
    {
        if (current < amount) return false;
        current -= amount;
        return true;
    }

    public bool CanAfford(float amount) => current >= amount;
    public bool IsFull => current >= max;
    public bool IsEmpty => current <= 0f;
}
