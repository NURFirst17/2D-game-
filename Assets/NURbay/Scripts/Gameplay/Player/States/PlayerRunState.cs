public sealed class PlayerRunState : PlayerState
{
    public PlayerRunState(CharacterController controller) : base(controller)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Run;

    public override void Tick()
    {
        if (Controller.TryEnterDeadState())
            return;

        if (Controller.TryEnterHurtState())
            return;

        if (Controller.TryEnterAttackState())
            return;

        if (Controller.TryEnterJumpState())
            return;

        if (!Controller.IsGrounded())
        {
            Controller.ChangeState(PlayerStateId.Fall);
            return;
        }

        if (!Controller.HasMoveInput)
        {
            Controller.ChangeState(PlayerStateId.Idle);
        }
    }

    public override void FixedTick()
    {
        Controller.TickMotor(CharacterController.HorizontalControlMode.FullControl);
    }
}
