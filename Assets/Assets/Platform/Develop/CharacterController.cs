using UnityEngine;

public class CharacterController : MonoBehaviour
{
    private const string HorizontalAxisName = "Horizontal";
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private ObstacleChecker _groundChecker;
    [SerializeField] private ObstacleChecker _ceilChecker;
    [SerializeField] private float _yVelocityForJump;
    [SerializeField] private float _gravity;
    
    [SerializeField] private float _speed;

    private Vector2 _velocity;

    private bool _jumpPressed;

    public Vector2 Velocity => _rigidbody.linearVelocity;

    private Quaternion TurnRight => Quaternion.identity;
    private Quaternion TurnLeft => Quaternion.Euler(0, 180, 0);
    
    private void Update()
    {
        float xInput = Input.GetAxisRaw(HorizontalAxisName); 
        
        _jumpPressed = Input.GetKeyDown(KeyCode.Space);
        
        float horizontalVelocity = _speed * xInput;

        _velocity = new Vector2(horizontalVelocity, _velocity.y);

        HandleGravity();

        HandleJump();

        HandleCeil();

        _rigidbody.linearVelocity = _velocity;

        transform.rotation = GetRotationFrom(_velocity);
    }


    public bool IsGrounded() => _groundChecker.IsTouches();
    
    private void HandleCeil()
    {
        if (_ceilChecker.IsTouches())
        _velocity.y = Mathf.Min(0, _velocity.y);
    }

    private void HandleJump()
    {
        if (_jumpPressed && _groundChecker.IsTouches())
            _velocity.y = _yVelocityForJump;
    }

    private void HandleGravity()
    {
        if (_groundChecker.IsTouches() && _velocity.y <= 0)
            _velocity.y = 0;
        else
            _velocity.y -= _gravity * Time.deltaTime;
    }

    private Quaternion GetRotationFrom(Vector2 velocity)
    {
        if (velocity.x > 0)
            return TurnRight;
        
        if (velocity.x < 0)
            return TurnLeft;
        
        return transform.rotation;
    }
}
