public abstract class PlayerState : IPlayerState
{
    protected PlayerState(CharacterController controller)
    {
        Controller = controller;
    }

    protected CharacterController Controller { get; }
    public abstract PlayerStateId Id { get; }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void Tick()
    {
    }

    public virtual void FixedTick()
    {
    }
}
