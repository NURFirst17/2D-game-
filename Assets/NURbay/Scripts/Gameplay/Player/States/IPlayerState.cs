public interface IPlayerState
{
    PlayerStateId Id { get; }

    void Enter();
    void Exit();
    void Tick();
    void FixedTick();
}
