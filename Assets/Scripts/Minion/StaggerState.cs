// 保存先: Assets/Scripts/Minion/StaggerState.cs
// ひるみ状態（優先度30）。ひるみ中はDead以外の何より優先される高優先度割り込み。
//   Enter()で進行中の攻撃を無条件ForceCancel（実体とStateの独立性の例外その1）＋その場で停止。
//   ひるみ中はその場でのけぞるだけ（移動も攻撃もしない）。残り時間の管理はStagger（実体）が持つ。
using UnityEngine;

[RequireComponent(typeof(Stagger))]
[RequireComponent(typeof(Movement))]
public class StaggerState : MonoBehaviour, IState
{
    private Stagger stagger;
    private Attack attack;     // 攻撃を持つ兵士のみ（無くてもよい）
    private Movement movement;
    private StateMachine stateMachine;

    public int Priority => 30;

    public void Initialize(StateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        stagger = GetComponent<Stagger>();
        attack = GetComponent<Attack>();
        movement = GetComponent<Movement>();
    }

    public bool CanEnter() => stagger.IsStaggered;

    public void Enter()
    {
        Debug.Log($"[StaggerDBG] {name} ひるみEnter。ForceCancel呼ぶ。IsAttacking={(attack != null ? attack.IsAttacking.ToString() : "no-attack")}"); // DEBUG 一時
        // 進行中の攻撃を無条件中断（ひるみの割り込み）。
        if (attack != null) attack.ForceCancel();
        // その場でのけぞる（移動停止）。
        if (movement != null) movement.StopHere();
    }

    public void Update()
    {
        // 何もしない。ひるみ時間の経過はStaggerが自分で減らす。時間切れでCanEnterがfalseになり抜ける。
    }

    public void Exit() { }
}
