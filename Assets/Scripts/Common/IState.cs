// 保存先: Assets/Scripts/Common/IState.cs
public interface IState
{
    void Initialize(StateMachine stateMachine);
    bool CanEnter();        // 追加: この状態になれる条件を満たすか（毎フレーム評価に使用）
    int Priority { get; }   // 追加: 状態の優先度（大きいほど優先）
    void Enter();
    void Tick();      // 毎フレーム処理。Unityのメッセージ名Updateとの衝突を避けるためTickに改名（StateMachineが現在状態にのみ呼ぶ）
    void Exit();
}
