public interface IEnemyState
{
    EnemyStateId Id { get; }

    void Enter();
    void Exit();
    void Tick();
}
