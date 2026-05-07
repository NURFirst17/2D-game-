public sealed class PlayerJumpState : PlayerState
{
    public PlayerJumpState(CharacterController controller) : base(controller)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Jump;

    public override void Enter()
    {
        Controller.Jump();
    }

    public override void Tick()
    {
        if (Controller.TryEnterDeadState())
            return;

        if (Controller.TryEnterHurtState())
            return;

        if (Controller.VerticalVelocity <= 0f)
        {
            Controller.ChangeState(PlayerStateId.Fall);
        }
    }

    public override void FixedTick()
    {
        Controller.TickMotor(CharacterController.HorizontalControlMode.FullControl);
    }
}
