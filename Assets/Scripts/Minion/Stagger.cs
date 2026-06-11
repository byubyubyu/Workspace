// 保存先: Assets/Scripts/Minion/Stagger.cs
// ひるみの実体。状態の実体は remainingTime のみ。IsStaggered は計算プロパティ（boolを別に持たない）。
//   自分のUpdateで時間を減らす（Attack/Visionと同じ「実体が自律、Stateは問い合わせ」パターン）。
//   StaggerState がこれを問い合わせて高優先度で割り込む。今回は「その場でのけぞる」（吹っ飛びは将来）。
using UnityEngine;

public class Stagger : MonoBehaviour
{
    protected float remainingTime; // 派生クラス（AccumulatedStagger等）が発動条件を差し替えられるようprotected

    public bool IsStaggered => remainingTime > 0f; // 計算プロパティ（実体はremainingTimeのみ）

    // ひるみを発動する。再呼び出し時はmax（短い攻撃でひるみが縮まず、加算で無限に伸びもしない）。
    // virtual: 派生クラスが「即ひるみ」以外の発動条件（蓄積式など）に差し替えられる。
    public virtual void Apply(float duration)
    {
        if (duration <= 0f) return;
        remainingTime = Mathf.Max(remainingTime, duration);
    }

    private void Update()
    {
        if (remainingTime > 0f)
            remainingTime -= Time.deltaTime;
    }
}
