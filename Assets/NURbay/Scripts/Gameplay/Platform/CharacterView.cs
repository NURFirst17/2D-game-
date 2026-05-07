using UnityEngine;

public class CharackterView : MonoBehaviour
{
    private readonly int IsRunningKey = Animator.StringToHash("isRunning");
    private readonly int VelocityYKey = Animator.StringToHash("yVelocity");
    private readonly int IsGroundedKey = Animator.StringToHash("isGrounded");

    [SerializeField] private CharacterController _character;
    [SerializeField] private Animator _animator;

    private void Update()
    {
        if (_character == null || _animator == null)
            return;

        _animator.SetBool(IsRunningKey, Mathf.Abs(_character.Velocity.x) > 0.05f);
        _animator.SetFloat(VelocityYKey, _character.Velocity.y);
        _animator.SetBool(IsGroundedKey, _character.IsGrounded());
    }
}
