public sealed class PlayerAttackState : PlayerState
{
    public PlayerAttackState(CharacterController controller) : base(controller)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Attack;

    public override void Enter()
    {
        Controller.StartAttack();
    }

    public override void Tick()
    {
        if (Controller.TryEnterDeadState())
            return;

        if (Controller.TryEnterHurtState())
            return;

        if (!Controller.IsAttackInProgress)
        {
            Controller.ChangeState(Controller.HasMoveInput ? PlayerStateId.Run : PlayerStateId.Idle);
        }
    }

    public override void FixedTick()
    {
        Controller.TickMotor(CharacterController.HorizontalControlMode.LockToZero);
    }
}
