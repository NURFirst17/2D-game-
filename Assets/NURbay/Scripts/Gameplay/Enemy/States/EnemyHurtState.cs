public sealed class EnemyHurtState : EnemyState
{
    public EnemyHurtState(EnemyChase enemy) : base(enemy)
    {
    }

    public override EnemyStateId Id => EnemyStateId.Hurt;

    public override void Enter()
    {
        Enemy.StopMove();
        Enemy.ConsumeHurtRequest();
    }

    public override void Tick()
    {
        if (Enemy.TryEnterDeadState())
            return;

        if (Enemy.IsHurtLocked)
            return;

        Enemy.ChangeState(Enemy.CanChaseTarget ? EnemyStateId.Chase : EnemyStateId.Idle);
    }
}
