// 保存先: Assets/Scripts/Minion/StateMachine.cs
public class StateMachine
{
    private IState[] states;
    private IState currentState;

    public StateMachine(IState[] states)
    {
        this.states = states;
    }

    public void Update()
    {
        // 毎フレーム、CanEnter を満たす状態のうち最も優先度が高いものを選ぶ。
        IState best = null;
        foreach (var state in states)
        {
            if (!state.CanEnter()) continue;
            if (best == null || state.Priority > best.Priority)
                best = state;
        }

        // 選ばれた状態が今と違うなら切り替える（同じなら維持＝ちらつき防止）
        if (best != currentState)
        {
            currentState?.Exit();
            currentState = best;
            currentState?.Enter();
        }

        currentState?.Update();
    }
}
