public sealed class PlayerDeadState : PlayerState
{
    public PlayerDeadState(CharacterController controller) : base(controller)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Dead;

    public override void FixedTick()
    {
        Controller.TickMotor(CharacterController.HorizontalControlMode.LockToZero);
    }
}
