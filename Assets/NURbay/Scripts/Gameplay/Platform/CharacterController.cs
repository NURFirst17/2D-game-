using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public enum HorizontalControlMode
    {
        FullControl,
        LockToZero,
        Preserve
    }

    private const string HorizontalAxisName = "Horizontal";
    private const float GroundedVerticalVelocity = -1f;

    [Header("Physics")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Collider2D[] _solidColliders;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private ObstacleChecker _groundChecker;
    [SerializeField] private ObstacleChecker _ceilChecker;
    [SerializeField] private float _yVelocityForJump = 25f;
    [SerializeField] private float _gravity = 80f;
    [SerializeField] private float _speed = 7f;
    [SerializeField] private float _deathVisualLocalY = 0f;

    [Header("Input")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;

    [Header("Combat")]
    [SerializeField] private PlayerAttack _playerAttack;

    [Header("State")]
    [SerializeField] private float _hurtLockDuration = 0.2f;

    private readonly Dictionary<PlayerStateId, IPlayerState> _states = new();

    private PlayerHealth _playerHealth;
    private PlayerStateMachine _stateMachine;
    private Vector2 _velocity;
    private float _horizontalInput;
    private bool _jumpRequested;
    private bool _attackRequested;
    private bool _hurtRequested;
    private float _hurtLockTimer;
    private bool _isDead;
    private Vector3 _initialVisualLocalPosition;

    public Vector2 Velocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;
    public float VerticalVelocity => Velocity.y;
    public bool HasMoveInput => Mathf.Abs(_horizontalInput) > 0.01f;
    public bool IsAttackInProgress => _playerAttack != null && _playerAttack.IsAttacking;
    public bool IsHurtLocked => _hurtLockTimer > 0f;
    public PlayerStateId CurrentStateId => _stateMachine?.CurrentState?.Id ?? PlayerStateId.Idle;

    private Quaternion TurnRight => Quaternion.identity;
    private Quaternion TurnLeft => Quaternion.Euler(0f, 180f, 0f);

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        if (_playerAttack == null)
        {
            _playerAttack = GetComponent<PlayerAttack>();
        }

        if (_solidColliders == null || _solidColliders.Length == 0)
        {
            _solidColliders = GetComponents<Collider2D>();
        }

        if (_visualRoot == null)
        {
            var animatorInChildren = GetComponentInChildren<Animator>();
            if (animatorInChildren != null)
            {
                _visualRoot = animatorInChildren.transform;
            }
        }

        if (_visualRoot != null)
        {
            _initialVisualLocalPosition = _visualRoot.localPosition;
        }

        _playerHealth = GetComponent<PlayerHealth>();
        _stateMachine = new PlayerStateMachine();

        RegisterStates();
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Damaged += HandleDamaged;
            _playerHealth.Died += HandleDied;
        }
    }

    private void Start()
    {
        _stateMachine.Initialize(_states[PlayerStateId.Idle]);
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Damaged -= HandleDamaged;
            _playerHealth.Died -= HandleDied;
        }
    }

    private void Update()
    {
        ReadInput();

        if (_playerAttack != null)
        {
            _playerAttack.Tick(Time.deltaTime);
        }

        if (_hurtLockTimer > 0f)
        {
            _hurtLockTimer -= Time.deltaTime;
        }

        _stateMachine.Tick();
    }

    private void FixedUpdate()
    {
        _stateMachine.FixedTick();
    }

    public bool IsGrounded()
    {
        return _groundChecker != null && _groundChecker.IsTouches();
    }

    public void ChangeState(PlayerStateId stateId)
    {
        if (_states.TryGetValue(stateId, out var nextState))
        {
            _stateMachine.ChangeState(nextState);
        }
    }

    public bool TryEnterJumpState()
    {
        if (_isDead || !_jumpRequested || !IsGrounded())
        {
            return false;
        }

        _jumpRequested = false;
        ChangeState(PlayerStateId.Jump);
        return true;
    }

    public bool TryEnterAttackState()
    {
        if (_isDead || !_attackRequested || _playerAttack == null)
        {
            return false;
        }

        _attackRequested = false;

        if (!_playerAttack.CanStartAttack(IsGrounded()))
        {
            return false;
        }

        ChangeState(PlayerStateId.Attack);
        return true;
    }

    public bool TryEnterHurtState()
    {
        if (_isDead || !_hurtRequested)
        {
            return false;
        }

        ChangeState(PlayerStateId.Hurt);
        return true;
    }

    public bool TryEnterDeadState()
    {
        if (!_isDead)
        {
            return false;
        }

        if (CurrentStateId != PlayerStateId.Dead)
        {
            ChangeState(PlayerStateId.Dead);
        }

        return true;
    }

    public void Jump()
    {
        _velocity = _rigidbody.linearVelocity;
        _velocity.y = _yVelocityForJump;
        _rigidbody.linearVelocity = _velocity;
    }

    public void StartAttack()
    {
        _playerAttack?.TryStartAttack();
    }

    public void TickMotor(HorizontalControlMode controlMode)
    {
        if (_rigidbody == null)
        {
            return;
        }

        _velocity = _rigidbody.linearVelocity;

        switch (controlMode)
        {
            case HorizontalControlMode.FullControl:
                _velocity.x = _speed * _horizontalInput;
                break;
            case HorizontalControlMode.LockToZero:
                _velocity.x = 0f;
                break;
            case HorizontalControlMode.Preserve:
                break;
        }

        HandleCeil();
        HandleGravity();
        _rigidbody.linearVelocity = _velocity;

        UpdateRotation(controlMode);
    }

    public void ConsumeHurtRequest()
    {
        _hurtRequested = false;
    }

    private void RegisterStates()
    {
        _states[PlayerStateId.Idle] = new PlayerIdleState(this);
        _states[PlayerStateId.Run] = new PlayerRunState(this);
        _states[PlayerStateId.Jump] = new PlayerJumpState(this);
        _states[PlayerStateId.Fall] = new PlayerFallState(this);
        _states[PlayerStateId.Attack] = new PlayerAttackState(this);
        _states[PlayerStateId.Hurt] = new PlayerHurtState(this);
        _states[PlayerStateId.Dead] = new PlayerDeadState(this);
    }

    private void ReadInput()
    {
        if (_isDead)
        {
            _horizontalInput = 0f;
            _jumpRequested = false;
            _attackRequested = false;
            return;
        }

        _horizontalInput = Input.GetAxisRaw(HorizontalAxisName);

        if (Input.GetKeyDown(_jumpKey))
        {
            _jumpRequested = true;
        }

        if (_playerAttack != null && Input.GetKeyDown(_playerAttack.AttackKey))
        {
            _attackRequested = true;
        }
    }

    private void HandleGravity()
    {
        if (IsGrounded() && _velocity.y <= 0f)
        {
            _velocity.y = GroundedVerticalVelocity;
        }
        else
        {
            _velocity.y -= _gravity * Time.fixedDeltaTime;
        }
    }

    private void HandleCeil()
    {
        if (_ceilChecker != null && _ceilChecker.IsTouches())
        {
            _velocity.y = Mathf.Min(0f, _velocity.y);
        }
    }

    private void UpdateRotation(HorizontalControlMode controlMode)
    {
        if (controlMode == HorizontalControlMode.Preserve)
        {
            return;
        }

        if (_horizontalInput > 0f)
        {
            transform.rotation = TurnRight;
        }
        else if (_horizontalInput < 0f)
        {
            transform.rotation = TurnLeft;
        }
    }

    private void HandleDamaged()
    {
        if (_isDead)
        {
            return;
        }

        _jumpRequested = false;
        _attackRequested = false;
        _hurtRequested = true;
        _hurtLockTimer = _hurtLockDuration;
    }

    private void HandleDied()
    {
        _isDead = true;
        _jumpRequested = false;
        _attackRequested = false;
        _hurtRequested = false;
        IgnoreEnemyCollisions();
        GroundDeathVisual();
    }

    private void IgnoreEnemyCollisions()
    {
        if (_solidColliders == null)
        {
            return;
        }

        var enemyColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var ownCollider in _solidColliders)
        {
            if (ownCollider == null)
            {
                continue;
            }

            foreach (var otherCollider in enemyColliders)
            {
                if (otherCollider == null || otherCollider.transform.root == transform.root)
                {
                    continue;
                }

                if (otherCollider.gameObject.layer != 6)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(ownCollider, otherCollider, true);
            }
        }

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
        }
    }

    private void GroundDeathVisual()
    {
        if (_visualRoot == null)
        {
            return;
        }

        var localPosition = _visualRoot.localPosition;
        localPosition.y = _deathVisualLocalY;
        _visualRoot.localPosition = localPosition;
    }
}
