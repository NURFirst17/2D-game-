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

    [Header("One Way Platforms")]
    [SerializeField] private LayerMask _oneWayPlatformMask = 1 << 8;
    [SerializeField] private float _oneWayStandTolerance = 0.08f;
    [SerializeField] private float _oneWayPlatformProbePadding = 1.5f;
    [SerializeField] private float _oneWayPlatformRayStartOffset = 0.08f;

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
    private readonly Collider2D[] _oneWayPlatformBuffer = new Collider2D[16];
    private readonly HashSet<Collider2D> _ignoredOneWayPlatforms = new();
    private readonly HashSet<Collider2D> _visibleOneWayPlatforms = new();
    private readonly List<Collider2D> _releasedOneWayPlatforms = new();
    private readonly RaycastHit2D[] _oneWayPlatformRaycastHits = new RaycastHit2D[8];

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
        if (_groundChecker == null)
        {
            return false;
        }

        var groundCollider = _groundChecker.GetTouchingCollider();
        if (groundCollider == null)
        {
            return IsStandingOnAnyOneWayPlatform();
        }

        return !IsOneWayPlatform(groundCollider) || CanStandOnOneWayPlatform(groundCollider);
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

        RefreshOneWayPlatformCollisions();
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
        if (_ceilChecker == null)
        {
            return;
        }

        var ceilingCollider = _ceilChecker.GetTouchingCollider();
        if (ceilingCollider != null && !IsOneWayPlatform(ceilingCollider))
        {
            _velocity.y = Mathf.Min(0f, _velocity.y);
        }
    }

    private void RefreshOneWayPlatformCollisions()
    {
        if (_solidColliders == null || _solidColliders.Length == 0)
        {
            return;
        }

        _visibleOneWayPlatforms.Clear();

        foreach (var ownCollider in _solidColliders)
        {
            if (ownCollider == null || ownCollider.isTrigger)
            {
                continue;
            }

            var bounds = ownCollider.bounds;
            var probeSize = new Vector2(bounds.size.x + _oneWayPlatformProbePadding, bounds.size.y + _oneWayPlatformProbePadding);
            var platformFilter = new ContactFilter2D();
            platformFilter.SetLayerMask(_oneWayPlatformMask);
            platformFilter.useTriggers = false;
            var platformCount = Physics2D.OverlapBox(bounds.center, probeSize, 0f, platformFilter, _oneWayPlatformBuffer);

            for (var i = 0; i < platformCount; i++)
            {
                var platformCollider = _oneWayPlatformBuffer[i];
                if (platformCollider == null || platformCollider.transform.root == transform.root)
                {
                    continue;
                }

                _visibleOneWayPlatforms.Add(platformCollider);

                var shouldIgnore = ShouldIgnoreOneWayPlatform(ownCollider, platformCollider);
                Physics2D.IgnoreCollision(ownCollider, platformCollider, shouldIgnore);

                if (shouldIgnore)
                {
                    _ignoredOneWayPlatforms.Add(platformCollider);
                }
                else
                {
                    _ignoredOneWayPlatforms.Remove(platformCollider);
                }
            }
        }

        ReleaseHiddenOneWayPlatforms();
    }

    private void ReleaseHiddenOneWayPlatforms()
    {
        if (_ignoredOneWayPlatforms.Count == 0)
        {
            return;
        }

        _releasedOneWayPlatforms.Clear();
        foreach (var platformCollider in _ignoredOneWayPlatforms)
        {
            if (platformCollider == null || _visibleOneWayPlatforms.Contains(platformCollider))
            {
                continue;
            }

            SetIgnoredForAllSolidColliders(platformCollider, false);
            _releasedOneWayPlatforms.Add(platformCollider);
        }

        foreach (var platformCollider in _releasedOneWayPlatforms)
        {
            _ignoredOneWayPlatforms.Remove(platformCollider);
        }

        _releasedOneWayPlatforms.Clear();
    }

    private void SetIgnoredForAllSolidColliders(Collider2D platformCollider, bool ignored)
    {
        foreach (var ownCollider in _solidColliders)
        {
            if (ownCollider != null && !ownCollider.isTrigger && platformCollider != null)
            {
                Physics2D.IgnoreCollision(ownCollider, platformCollider, ignored);
            }
        }
    }

    private bool ShouldIgnoreOneWayPlatform(Collider2D ownCollider, Collider2D platformCollider)
    {
        return _velocity.y > 0f || !CanStandOnOneWayPlatform(ownCollider, platformCollider);
    }

    private bool CanStandOnOneWayPlatform(Collider2D platformCollider)
    {
        if (_solidColliders == null)
        {
            return false;
        }

        foreach (var ownCollider in _solidColliders)
        {
            if (CanStandOnOneWayPlatform(ownCollider, platformCollider))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanStandOnOneWayPlatform(Collider2D ownCollider, Collider2D platformCollider)
    {
        if (platformCollider == null || _velocity.y > 0.01f)
        {
            return false;
        }

        if (ownCollider == null || ownCollider.isTrigger)
        {
            return false;
        }

        var bounds = ownCollider.bounds;
        var rayDistance = _oneWayStandTolerance + _oneWayPlatformRayStartOffset + Mathf.Max(0.05f, -_velocity.y * Time.fixedDeltaTime);
        var leftX = bounds.min.x + Mathf.Min(0.05f, bounds.extents.x);
        var rightX = bounds.max.x - Mathf.Min(0.05f, bounds.extents.x);
        var centerX = bounds.center.x;
        var startY = bounds.min.y + _oneWayPlatformRayStartOffset;

        return IsPlatformBelowFoot(new Vector2(leftX, startY), rayDistance, platformCollider) ||
               IsPlatformBelowFoot(new Vector2(centerX, startY), rayDistance, platformCollider) ||
               IsPlatformBelowFoot(new Vector2(rightX, startY), rayDistance, platformCollider);
    }

    private bool IsStandingOnAnyOneWayPlatform()
    {
        if (_solidColliders == null || _velocity.y > 0.01f)
        {
            return false;
        }

        var platformFilter = new ContactFilter2D();
        platformFilter.SetLayerMask(_oneWayPlatformMask);
        platformFilter.useTriggers = false;

        foreach (var ownCollider in _solidColliders)
        {
            if (ownCollider == null || ownCollider.isTrigger)
            {
                continue;
            }

            var bounds = ownCollider.bounds;
            var rayDistance = _oneWayStandTolerance + _oneWayPlatformRayStartOffset + Mathf.Max(0.05f, -_velocity.y * Time.fixedDeltaTime);
            var origin = new Vector2(bounds.center.x, bounds.min.y + _oneWayPlatformRayStartOffset);
            var hitCount = Physics2D.Raycast(origin, Vector2.down, platformFilter, _oneWayPlatformRaycastHits, rayDistance);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _oneWayPlatformRaycastHits[i];
                if (hit.collider != null && hit.normal.y > 0.5f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsPlatformBelowFoot(Vector2 origin, float rayDistance, Collider2D platformCollider)
    {
        var platformFilter = new ContactFilter2D();
        platformFilter.SetLayerMask(_oneWayPlatformMask);
        platformFilter.useTriggers = false;

        var hitCount = Physics2D.Raycast(origin, Vector2.down, platformFilter, _oneWayPlatformRaycastHits, rayDistance);
        for (var i = 0; i < hitCount; i++)
        {
            var hit = _oneWayPlatformRaycastHits[i];
            if (hit.collider == null || hit.normal.y <= 0.5f)
            {
                continue;
            }

            if (hit.collider == platformCollider || hit.collider.transform == platformCollider.transform)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOneWayPlatform(Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return false;
        }

        return (_oneWayPlatformMask.value & (1 << targetCollider.gameObject.layer)) != 0;
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
