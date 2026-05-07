public sealed class EnemyDeadState : EnemyState
{
    public EnemyDeadState(EnemyChase enemy) : base(enemy)
    {
    }

    public override EnemyStateId Id => EnemyStateId.Dead;

    public override void Enter()
    {
        Enemy.StopMove();
        Enemy.DisableAttackWindow();
    }
}
