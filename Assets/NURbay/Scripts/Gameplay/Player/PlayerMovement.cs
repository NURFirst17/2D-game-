using UnityEngine;

[RequireComponent(typeof(CharacterGravity2D))]
public class PlayerMovement : MonoBehaviour
{
    private const string HorizontalAxisName = "Horizontal";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("References")]
    [SerializeField] private CharacterGravity2D gravity;
    [SerializeField] private Animator animator;

    private PlayerHealth _playerHealth;
    private float _moveInput;
    private bool _facingRight = true;

    public bool IsGrounded => gravity != null && gravity.IsGrounded;
    public bool IsDashing => false;
    public float VerticalVelocity => gravity != null ? gravity.VerticalVelocity : 0f;
    public float MoveInput => _moveInput;

    private void Awake()
    {
        if (gravity == null)
        {
            gravity = GetComponent<CharacterGravity2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        _playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (_playerHealth != null && _playerHealth.IsDead)
        {
            _moveInput = 0f;
            gravity.SetHorizontalVelocity(0f);
            UpdateAnimator();
            return;
        }

        _moveInput = Input.GetAxisRaw(HorizontalAxisName);
        gravity.SetHorizontalVelocity(_moveInput * moveSpeed);

        if (Input.GetKeyDown(jumpKey))
        {
            gravity.Jump(jumpForce);
        }

        HandleFlip();
        UpdateAnimator();
    }

    private void HandleFlip()
    {
        if (_moveInput > 0f && !_facingRight)
        {
            Flip();
        }
        else if (_moveInput < 0f && _facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        _facingRight = !_facingRight;

        var localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private void UpdateAnimator()
    {
        if (animator == null || gravity == null)
        {
            return;
        }

        animator.SetBool("isRunning", Mathf.Abs(_moveInput) > 0.01f);
        animator.SetBool("isGrounded", gravity.IsGrounded);
        animator.SetFloat("yVelocity", gravity.VerticalVelocity);
    }
}
