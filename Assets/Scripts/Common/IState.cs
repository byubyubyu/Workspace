public interface IState
{
    void Initialize(StateMachine stateMachine);
    void Enter();
    void Update();
    void Exit();
}
