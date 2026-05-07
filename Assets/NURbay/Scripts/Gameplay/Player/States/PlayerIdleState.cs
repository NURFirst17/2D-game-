public sealed class PlayerIdleState : PlayerState
{
    public PlayerIdleState(CharacterController controller) : base(controller)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Idle;

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

        if (Controller.HasMoveInput)
        {
            Controller.ChangeState(PlayerStateId.Run);
        }
    }

    public override void FixedTick()
    {
        Controller.TickMotor(CharacterController.HorizontalControlMode.FullControl);
    }
}
