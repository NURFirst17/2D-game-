using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerHealth playerHealth;

    private float moveInput;
    private bool isGrounded;
    private bool facingRight = true;

    private bool isDashing;
    private float dashTimeLeft;
    private float dashCooldownTimer;
    private float dashDirection;

    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public float VerticalVelocity => rb.linearVelocity.y;
    public float MoveInput => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        CheckGround();
        HandleTimers();

        if (playerHealth != null && playerHealth.IsDead)
        {
            moveInput = 0f;
            UpdateAnimator();
            return;
        }

        ReadInput();
        HandleJump();
        HandleDash();
        Flip();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing)
        {
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            return;
        }

        Move();
    }

    private void ReadInput()
    {
        if (isDashing)
            return;

        moveInput = Input.GetAxisRaw("Horizontal");
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (isDashing)
            return;

        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            Jump();
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void HandleDash()
    {
        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;

            if (dashTimeLeft <= 0f)
            {
                StopDash();
            }

            return;
        }

        if (dashCooldownTimer > 0f)
            return;

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimeLeft = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (moveInput != 0)
            dashDirection = moveInput;
        else
            dashDirection = facingRight ? 1f : -1f;

        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
    }

    private void StopDash()
    {
        isDashing = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void HandleTimers()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Flip()
    {
        if (isDashing)
            return;

        if (moveInput > 0 && !facingRight)
        {
            RotatePlayer();
        }
        else if (moveInput < 0 && facingRight)
        {
            RotatePlayer();
        }
    }

    private void RotatePlayer()
    {
        facingRight = !facingRight;

        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool("isRunning", moveInput != 0 && !isDashing);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isDashing", isDashing);
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}