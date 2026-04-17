using UnityEngine;

public class CharackterView : MonoBehaviour
{
    private readonly int VelocityXKey = Animator.StringToHash("VelocityX");
    private readonly int VelocityYKey = Animator.StringToHash("VelocityY");
    private readonly int IsGroundedKey = Animator.StringToHash("IsGrounded");

    [SerializeField] private CharacterController _character;
    [SerializeField] private Animator _animator;

    private void Update()
    {
        _animator.SetFloat(VelocityXKey, Mathf.Abs(_character.Velocity.x));
        _animator.SetFloat(VelocityYKey, _character.Velocity.y);
        _animator.SetBool(IsGroundedKey, _character.IsGrounded());
    }
}
