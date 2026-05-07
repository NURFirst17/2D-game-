using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterGravity2D))]
public class EnemyChase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float chaseMoveSpeed = 3.2f;
    [SerializeField] private float chaseDistance = 8f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private bool requireFacingTargetForAggro = true;

    [Header("Obstacle Check")]
    [SerializeField] private ObstacleChecker wallChecker;
    [SerializeField] private CharacterGravity2D gravity;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.4f;
    [SerializeField] private float attackCooldown = 1.1f;
    [SerializeField] private DamageDealer damageDealer;
    [SerializeField] private float fallbackDamageWindow = 0.2f;
    [SerializeField] private float attackStateDuration = 0.6f;

    [Header("State")]
    [SerializeField] private float hurtLockDuration = 0.45f;

    [Header("Patrol")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private float patrolRadius = 2.5f;
    [SerializeField] private float patrolPauseDuration = 0.4f;
    [SerializeField] private float patrolMoveSpeed = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private readonly Dictionary<EnemyStateId, IEnemyState> _states = new();

    private EnemyHealth _enemyHealth;
    private EnemyStateMachine _stateMachine;
    private float _moveDirection;
    private float _attackCooldownTimer;
    private float _attackStateTimer;
    private float _hurtLockTimer;
    private float _patrolPauseTimer;
    private float _spawnX;
    private bool _facingRight = true;
    private bool _attackTriggeredByAnimation;
    private bool _hurtRequested;
    private bool _isDead;
    private bool _hasAttackTrigger;
    private bool _hasIsRunningParameter;
    private bool _hasIsGroundedParameter;
    private bool _hasYVelocityParameter;
    private bool _hasIsAttackingParameter;
    private bool _initializedFacing;

    public bool HasTarget => player != null;
    public bool CanChaseTarget => HasTarget && IsTargetAlive() && DistanceToPlayer <= chaseDistance && IsTargetVisibleForAggro();
    public bool CanAttackTarget => HasTarget && IsTargetAlive() && DistanceToPlayer <= attackRange && IsTargetVisibleForAggro();
    public bool IsAttackInProgress => _attackStateTimer > 0f;
    public bool IsHurtLocked => _hurtLockTimer > 0f;
    public EnemyStateId CurrentStateId => _stateMachine?.CurrentState?.Id ?? EnemyStateId.Idle;

    private float DistanceToPlayer => player == null ? float.MaxValue : Vector2.Distance(transform.position, player.position);

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

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (damageDealer == null)
        {
            damageDealer = GetComponentInChildren<DamageDealer>(true);
        }

        _enemyHealth = GetComponent<EnemyHealth>();
        _hasAttackTrigger = HasTrigger(animator, "Attack");
        _hasIsRunningParameter = HasBool(animator, "isRunning");
        _hasIsGroundedParameter = HasBool(animator, "isGrounded");
        _hasYVelocityParameter = HasFloat(animator, "yVelocity");
        _hasIsAttackingParameter = HasBool(animator, "isAttacking");
        _spawnX = transform.position.x;
        InitializeFacing();
        _stateMachine = new EnemyStateMachine();
        RegisterStates();
    }

    private void OnEnable()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.Damaged += HandleDamaged;
            _enemyHealth.Died += HandleDied;
        }
    }

    private void Start()
    {
        _stateMachine.Initialize(_states[EnemyStateId.Idle]);
    }

    private void OnDisable()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.Damaged -= HandleDamaged;
            _enemyHealth.Died -= HandleDied;
        }
    }

    private void Update()
    {
        ResolvePlayer();
        TickTimers();
        _stateMachine.Tick();
        UpdateAnimator();
    }

    public void ChangeState(EnemyStateId stateId)
    {
        if (_states.TryGetValue(stateId, out var nextState))
        {
            _stateMachine.ChangeState(nextState);
        }
    }

    public bool TryEnterDeadState()
    {
        if (!_isDead)
            return false;

        if (CurrentStateId != EnemyStateId.Dead)
        {
            ChangeState(EnemyStateId.Dead);
        }

        return true;
    }

    public bool TryEnterHurtState()
    {
        if (_isDead || !_hurtRequested)
            return false;

        ChangeState(EnemyStateId.Hurt);
        return true;
    }

    public void ConsumeHurtRequest()
    {
        _hurtRequested = false;
    }

    public void StopMove()
    {
        _moveDirection = 0f;
        gravity.SetHorizontalVelocity(0f);
    }

    public void MoveToTarget()
    {
        if (player == null)
        {
            StopMove();
            return;
        }

        var offset = player.position - transform.position;
        _moveDirection = Mathf.Sign(offset.x);

        if (Mathf.Abs(offset.x) <= stopDistance)
        {
            StopMove();
        }
        else
        {
            if (CanMoveInDirection(_moveDirection))
            {
                gravity.SetHorizontalVelocity(_moveDirection * chaseMoveSpeed);
            }
            else
            {
                StopMove();
            }
        }
        HandleFlip();
    }

    public void Patrol()
    {
        if (!enablePatrol)
        {
            StopMove();
            return;
        }

        if (_patrolPauseTimer > 0f)
        {
            StopMove();
            return;
        }

        var minX = _spawnX - patrolRadius;
        var maxX = _spawnX + patrolRadius;
        var direction = _facingRight ? 1f : -1f;

        if ((_facingRight && transform.position.x >= maxX) || (!_facingRight && transform.position.x <= minX) || !CanMoveInDirection(direction))
        {
            StopMove();
            _patrolPauseTimer = patrolPauseDuration;
            Flip();
            return;
        }

        _moveDirection = direction;
        gravity.SetHorizontalVelocity(_moveDirection * patrolMoveSpeed);
    }

    public void StartAttack()
    {
        if (_attackCooldownTimer > 0f || _isDead)
            return;

        FaceTarget();
        StopMove();
        _attackCooldownTimer = attackCooldown;
        _attackStateTimer = attackStateDuration;
        _attackTriggeredByAnimation = false;

        if (animator != null && _hasAttackTrigger)
        {
            animator.SetTrigger("Attack");
            return;
        }

        OpenAttackWindow();
    }

    public void DisableAttackWindow()
    {
        if (damageDealer != null)
        {
            damageDealer.DisableDamageWindow();
        }
    }

    public void AnimationEvent_EnableAttackDamage()
    {
        _attackTriggeredByAnimation = true;
        OpenAttackWindow();
    }

    public void AnimationEvent_DisableAttackDamage()
    {
        DisableAttackWindow();
    }

    public void OpenAttackWindow()
    {
        if (damageDealer == null)
            return;

        damageDealer.EnableDamageWindow();
        CancelInvoke(nameof(CloseAttackWindowFallback));
        Invoke(nameof(CloseAttackWindowFallback), fallbackDamageWindow);
    }

    private void RegisterStates()
    {
        _states[EnemyStateId.Idle] = new EnemyIdleState(this);
        _states[EnemyStateId.Chase] = new EnemyChaseState(this);
        _states[EnemyStateId.Attack] = new EnemyAttackState(this);
        _states[EnemyStateId.Hurt] = new EnemyHurtState(this);
        _states[EnemyStateId.Dead] = new EnemyDeadState(this);
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        var playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void TickTimers()
    {
        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= Time.deltaTime;
        }

        if (_attackStateTimer > 0f)
        {
            _attackStateTimer -= Time.deltaTime;
            if (_attackStateTimer <= 0f)
            {
                DisableAttackWindow();
            }
        }

        if (_hurtLockTimer > 0f)
        {
            _hurtLockTimer -= Time.deltaTime;
        }

        if (_patrolPauseTimer > 0f)
        {
            _patrolPauseTimer -= Time.deltaTime;
        }
    }

    private void HandleFlip()
    {
        UpdateWallCheckDirection();

        if (_moveDirection > 0f && !_facingRight)
        {
            Flip();
        }
        else if (_moveDirection < 0f && _facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        _facingRight = !_facingRight;
        ApplyFacing();
    }

    private void UpdateWallCheckDirection()
    {
        if (wallChecker == null)
            return;

        var direction = _moveDirection == 0f
            ? (_facingRight ? Vector2.right : Vector2.left)
            : new Vector2(Mathf.Sign(_moveDirection), 0f);

        wallChecker.SetDirection(direction);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        if (_hasIsRunningParameter)
        {
            animator.SetBool("isRunning", Mathf.Abs(_moveDirection) > 0.01f);
        }

        if (_hasIsGroundedParameter)
        {
            animator.SetBool("isGrounded", gravity != null && gravity.IsGrounded);
        }

        if (_hasIsAttackingParameter)
        {
            animator.SetBool("isAttacking", CurrentStateId == EnemyStateId.Attack);
        }

        if (_hasYVelocityParameter)
        {
            animator.SetFloat("yVelocity", gravity != null ? gravity.VerticalVelocity : 0f);
        }
    }

    private void CloseAttackWindowFallback()
    {
        if (_attackTriggeredByAnimation)
            return;

        DisableAttackWindow();
    }

    private void HandleDamaged()
    {
        if (_isDead)
            return;

        FaceTarget();
        StopMove();
        DisableAttackWindow();
        _hurtRequested = true;
        _hurtLockTimer = hurtLockDuration;
        _attackStateTimer = 0f;
    }

    private void HandleDied()
    {
        _isDead = true;
        StopMove();
        DisableAttackWindow();
        _hurtRequested = false;
        _attackStateTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }

    private static bool HasTrigger(Animator targetAnimator, string parameterName)
    {
        return HasParameter(targetAnimator, parameterName, AnimatorControllerParameterType.Trigger);
    }

    private static bool HasBool(Animator targetAnimator, string parameterName)
    {
        return HasParameter(targetAnimator, parameterName, AnimatorControllerParameterType.Bool);
    }

    private static bool HasFloat(Animator targetAnimator, string parameterName)
    {
        return HasParameter(targetAnimator, parameterName, AnimatorControllerParameterType.Float);
    }

    private static bool HasParameter(Animator targetAnimator, string parameterName, AnimatorControllerParameterType type)
    {
        if (targetAnimator == null)
            return false;

        foreach (var parameter in targetAnimator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void InitializeFacing()
    {
        if (_initializedFacing)
            return;

        _initializedFacing = true;

        if (spriteRenderer != null)
        {
            _facingRight = !spriteRenderer.flipX;
            return;
        }

        _facingRight = transform.localScale.x >= 0f;
    }

    private void ApplyFacing()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !_facingRight;
        }

        var localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x);
        transform.localScale = localScale;

        MirrorChildX(wallChecker != null ? wallChecker.transform : null);
        MirrorChildX(damageDealer != null ? damageDealer.transform : null);
    }

    private void FaceTarget()
    {
        if (player == null)
            return;

        var delta = player.position.x - transform.position.x;
        if (Mathf.Abs(delta) <= 0.01f)
            return;

        var shouldFaceRight = delta > 0f;
        if (shouldFaceRight == _facingRight)
            return;

        _facingRight = shouldFaceRight;
        ApplyFacing();
        UpdateWallCheckDirection();
    }

    private void MirrorChildX(Transform target)
    {
        if (target == null)
            return;

        var localPosition = target.localPosition;
        localPosition.x = Mathf.Abs(localPosition.x) * (_facingRight ? 1f : -1f);
        target.localPosition = localPosition;
    }

    private bool CanMoveInDirection(float direction)
    {
        if (gravity == null || direction == 0f)
            return false;

        if (wallChecker != null)
        {
            wallChecker.SetDirection(new Vector2(Mathf.Sign(direction), 0f));
            if (wallChecker.IsTouches())
                return false;
        }

        var groundChecker = gravity.GroundChecker;
        if (groundChecker == null)
            return true;

        var collider2D = GetComponent<Collider2D>();
        var halfWidth = collider2D != null ? collider2D.bounds.extents.x : 0.15f;
        var checkerPosition = (Vector2)groundChecker.transform.position;
        var aheadPosition = checkerPosition + new Vector2(Mathf.Sign(direction) * (halfWidth + 0.08f), 0f);
        return groundChecker.IsTouchesAt(aheadPosition, Vector2.down);
    }

    private bool IsTargetVisibleForAggro()
    {
        if (player == null || !requireFacingTargetForAggro)
            return player != null;

        var delta = player.position.x - transform.position.x;
        if (Mathf.Abs(delta) <= 0.01f)
            return true;

        return delta > 0f ? _facingRight : !_facingRight;
    }

    private bool IsTargetAlive()
    {
        if (player == null)
            return false;

        var damageable = player.GetComponent<IDamageable>() ?? player.GetComponentInParent<IDamageable>();
        return damageable == null || !damageable.IsDead;
    }
}
