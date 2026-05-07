public class EnemyStateMachine
{
    public IEnemyState CurrentState { get; private set; }

    public void Initialize(IEnemyState initialState)
    {
        CurrentState = initialState;
        CurrentState?.Enter();
    }

    public void ChangeState(IEnemyState nextState)
    {
        if (nextState == null || CurrentState == nextState)
            return;

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }

    public void Tick()
    {
        CurrentState?.Tick();
    }
}
