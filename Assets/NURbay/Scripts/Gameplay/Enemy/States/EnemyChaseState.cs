public sealed class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyChase enemy) : base(enemy)
    {
    }

    public override EnemyStateId Id => EnemyStateId.Chase;

    public override void Tick()
    {
        if (Enemy.TryEnterDeadState())
            return;

        if (Enemy.TryEnterHurtState())
            return;

        if (!Enemy.HasTarget || !Enemy.CanChaseTarget)
        {
            Enemy.ChangeState(EnemyStateId.Idle);
            return;
        }

        if (Enemy.CanAttackTarget)
        {
            Enemy.ChangeState(EnemyStateId.Attack);
            return;
        }

        Enemy.MoveToTarget();
    }
}
