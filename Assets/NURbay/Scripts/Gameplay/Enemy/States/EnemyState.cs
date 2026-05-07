public abstract class EnemyState : IEnemyState
{
    protected EnemyState(EnemyChase enemy)
    {
        Enemy = enemy;
    }

    protected EnemyChase Enemy { get; }
    public abstract EnemyStateId Id { get; }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void Tick()
    {
    }
}
