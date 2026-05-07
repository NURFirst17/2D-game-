public sealed class PlayerHurtState : PlayerState
{
    public PlayerHurtState(CharacterController controller) : base(controller)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Hurt;

    public override void Enter()
    {
        Controller.ConsumeHurtRequest();
    }

    public override void Tick()
    {
        if (Controller.TryEnterDeadState())
            return;

        if (!Controller.IsHurtLocked)
        {
            if (!Controller.IsGrounded())
            {
                Controller.ChangeState(PlayerStateId.Fall);
                return;
            }

            Controller.ChangeState(Controller.HasMoveInput ? PlayerStateId.Run : PlayerStateId.Idle);
        }
    }

    public override void FixedTick()
    {
        Controller.TickMotor(CharacterController.HorizontalControlMode.Preserve);
    }
}
