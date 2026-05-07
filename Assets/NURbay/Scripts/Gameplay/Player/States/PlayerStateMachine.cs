public class PlayerStateMachine
{
    public IPlayerState CurrentState { get; private set; }

    public void Initialize(IPlayerState initialState)
    {
        CurrentState = initialState;
        CurrentState?.Enter();
    }

    public void ChangeState(IPlayerState nextState)
    {
        if (nextState == null || CurrentState == nextState)
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }

    public void Tick()
    {
        CurrentState?.Tick();
    }

    public void FixedTick()
    {
        CurrentState?.FixedTick();
    }
}
