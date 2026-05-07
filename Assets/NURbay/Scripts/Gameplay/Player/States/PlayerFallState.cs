public sealed class PlayerFallState : PlayerState
{
    public PlayerFallState(CharacterController controller) : base(controller)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Fall;

    public override void Tick()
    {
        if (Controller.TryEnterDeadState())
            return;

        if (Controller.TryEnterHurtState())
            return;

        if (Controller.IsGrounded())
        {
            Controller.ChangeState(Controller.HasMoveInput ? PlayerStateId.Run : PlayerStateId.Idle);
        }
    }

    public override void FixedTick()
    {
        Controller.TickMotor(CharacterController.HorizontalControlMode.FullControl);
    }
}
