using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterGravity2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private ObstacleChecker groundChecker;
    [SerializeField] private ObstacleChecker ceilChecker;

    [Header("Gravity")]
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float maxFallSpeed = 20f;
    [SerializeField] private float groundedVerticalVelocity = -1f;

    private Vector2 _velocity;
    private float _targetHorizontalVelocity;
    private bool _jumpRequested;
    private float _requestedJumpForce;

    public bool IsGrounded => groundChecker != null && groundChecker.IsTouches();
    public bool IsCeilingTouched => ceilChecker != null && ceilChecker.IsTouches();
    public float VerticalVelocity => _velocity.y;
    public Vector2 Velocity => _velocity;
    public ObstacleChecker GroundChecker => groundChecker;

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        SyncFromBody();
    }

    private void FixedUpdate()
    {
        SyncFromBody();
        _velocity.x = _targetHorizontalVelocity;
        HandleJumpRequest();
        HandleVerticalVelocity();
        body.linearVelocity = _velocity;
    }

    public void SetHorizontalVelocity(float horizontalVelocity)
    {
        _targetHorizontalVelocity = horizontalVelocity;
    }

    public void Jump(float jumpForce)
    {
        if (!IsGrounded)
        {
            return;
        }

        _jumpRequested = true;
        _requestedJumpForce = jumpForce;
    }

    private void SyncFromBody()
    {
        if (body == null)
        {
            return;
        }

        _velocity = body.linearVelocity;
    }

    private void HandleVerticalVelocity()
    {
        if (IsCeilingTouched && _velocity.y > 0f)
        {
            _velocity.y = 0f;
        }

        if (IsGrounded && _velocity.y <= 0f)
        {
            _velocity.y = groundedVerticalVelocity;
            return;
        }

        _velocity.y -= gravity * Time.fixedDeltaTime;
        if (_velocity.y < -maxFallSpeed)
        {
            _velocity.y = -maxFallSpeed;
        }
    }

    private void HandleJumpRequest()
    {
        if (!_jumpRequested)
        {
            return;
        }

        _velocity.y = _requestedJumpForce;
        _jumpRequested = false;
        _requestedJumpForce = 0f;
    }
}
