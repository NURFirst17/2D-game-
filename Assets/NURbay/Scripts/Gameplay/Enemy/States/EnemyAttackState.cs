public sealed class EnemyAttackState : EnemyState
{
    public EnemyAttackState(EnemyChase enemy) : base(enemy)
    {
    }

    public override EnemyStateId Id => EnemyStateId.Attack;

    public override void Enter()
    {
        Enemy.StopMove();
        Enemy.StartAttack();
    }

    public override void Tick()
    {
        if (Enemy.TryEnterDeadState())
            return;

        if (Enemy.TryEnterHurtState())
            return;

        if (Enemy.IsAttackInProgress)
            return;

        if (Enemy.CanAttackTarget)
        {
            Enemy.StartAttack();
            return;
        }

        Enemy.ChangeState(Enemy.CanChaseTarget ? EnemyStateId.Chase : EnemyStateId.Idle);
    }
}
