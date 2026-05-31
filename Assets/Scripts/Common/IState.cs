// 保存先: Assets/Scripts/Common/IState.cs
public interface IState
{
    void Initialize(StateMachine stateMachine);
    bool CanEnter();        // 追加: この状態になれる条件を満たすか（毎フレーム評価に使用）
    int Priority { get; }   // 追加: 状態の優先度（大きいほど優先）
    void Enter();
    void Update();
    void Exit();
}
