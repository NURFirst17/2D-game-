public sealed class EnemyIdleState : EnemyState
{
    public EnemyIdleState(EnemyChase enemy) : base(enemy)
    {
    }

    public override EnemyStateId Id => EnemyStateId.Idle;

    public override void Enter()
    {
        Enemy.StopMove();
    }

    public override void Tick()
    {
        if (Enemy.TryEnterDeadState())
            return;

        if (Enemy.TryEnterHurtState())
            return;

        if (Enemy.CanAttackTarget)
        {
            Enemy.ChangeState(EnemyStateId.Attack);
            return;
        }

        if (Enemy.CanChaseTarget)
        {
            Enemy.ChangeState(EnemyStateId.Chase);
            return;
        }

        Enemy.Patrol();
    }
}
