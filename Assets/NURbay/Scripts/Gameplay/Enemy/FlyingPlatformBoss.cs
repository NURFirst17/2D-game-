using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class FlyingPlatformBoss : MonoBehaviour
{
    private enum BossState
    {
        Move,
        Attack,
        Cooldown,
        Dead
    }

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Platform Movement")]
    [SerializeField] private LayerMask platformMask = 1 << 8;
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float jumpForce = 9.5f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float groundedRayDistance = 0.35f;
    [SerializeField] private float edgeProbeDistance = 0.85f;
    [SerializeField] private float jumpCooldown = 0.75f;
    [SerializeField] private float jumpWhenPlayerAbove = 1.3f;
    [SerializeField] private float jumpHorizontalRange = 4.5f;
    [SerializeField] private float dropWhenPlayerBelow = 1.2f;
    [SerializeField] private float dropThroughDuration = 0.45f;
    [SerializeField] private float dropCooldown = 0.65f;
    [SerializeField] private float horizontalSearchStep = 1.5f;
    [SerializeField] private int horizontalSearchSteps = 8;
    [SerializeField] private float platformRayStartHeight = 10f;
    [SerializeField] private float platformRayDistance = 25f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2.4f;
    [SerializeField] private float attackDuration = 0.9f;
    [SerializeField] private float attackDamageDelay = 0.25f;
    [SerializeField] private float attackDamageWindow = 0.25f;
    [SerializeField] private float attackCooldown = 1.1f;
    [SerializeField] private DamageDealer damageDealer;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string attackStateName = "Attack";

    private Rigidbody2D _body;
    private Collider2D _bodyCollider;
    private EnemyHealth _health;
    private BossState _state;
    private float _stateTimer;
    private float _damageOpenTimer;
    private float _damageCloseTimer;
    private float _jumpCooldownTimer;
    private float _dropTimer;
    private float _dropCooldownTimer;
    private bool _damageWindowOpened;
    private bool _facingRight = true;
    private Collider2D _ignoredPlatform;
    private Transform _collisionIgnoredPlayer;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _bodyCollider = GetComponent<Collider2D>();
        _body.gravityScale = gravityScale;
        _body.bodyType = RigidbodyType2D.Dynamic;
        _body.freezeRotation = true;

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

        _health = GetComponent<EnemyHealth>();
        PlayIdle();
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.Died -= HandleDied;
        }

        if (_ignoredPlatform != null)
        {
            SetPlatformIgnored(_ignoredPlatform, false);
        }

        damageDealer?.DisableDamageWindow();
    }

    private void FixedUpdate()
    {
        if (_state == BossState.Dead)
        {
            return;
        }

        ResolvePlayer();
        IgnorePlayerBodyCollision();
        TickTimers();
        TickPlatformDrop();

        switch (_state)
        {
            case BossState.Move:
                TickMove();
                break;
            case BossState.Attack:
                TickAttack();
                break;
            case BossState.Cooldown:
                TickCooldown();
                break;
        }
    }

    private void TickMove()
    {
        if (CanAttackPlayer())
        {
            StartAttack();
            return;
        }

        var horizontalDirection = GetMoveDirection();
        FaceMoveDirection(horizontalDirection);

        if (ShouldDropThroughPlatform())
        {
            DropThroughCurrentPlatform();
        }

        if (ShouldJump(horizontalDirection))
        {
            Jump();
        }

        if (!CanMoveOnPlatform(horizontalDirection))
        {
            horizontalDirection = 0f;
        }

        var velocity = _body.linearVelocity;
        velocity.x = horizontalDirection * moveSpeed;
        _body.linearVelocity = velocity;
    }

    private void TickAttack()
    {
        _stateTimer -= Time.fixedDeltaTime;
        _damageOpenTimer -= Time.fixedDeltaTime;
        _damageCloseTimer -= Time.fixedDeltaTime;

        if (player != null)
        {
            FacePosition(player.position);
        }

        MoveTowardPlayer();

        if (!_damageWindowOpened && _damageOpenTimer <= 0f)
        {
            damageDealer?.EnableDamageWindow();
            _damageWindowOpened = true;
        }

        if (_damageWindowOpened && _damageCloseTimer <= 0f)
        {
            damageDealer?.DisableDamageWindow();
        }

        if (_stateTimer > 0f)
        {
            return;
        }

        damageDealer?.DisableDamageWindow();
        _state = BossState.Cooldown;
        _stateTimer = attackCooldown;
        PlayIdle();
    }

    private void TickCooldown()
    {
        MoveTowardPlayer();

        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer > 0f)
        {
            return;
        }

        _state = BossState.Move;
    }

    private void StartAttack()
    {
        _state = BossState.Attack;
        _stateTimer = attackDuration;
        _damageOpenTimer = attackDamageDelay;
        _damageCloseTimer = attackDamageDelay + attackDamageWindow;
        _damageWindowOpened = false;
        MoveTowardPlayer();
        PlayAttack();
    }

    private void TickTimers()
    {
        if (_jumpCooldownTimer > 0f)
        {
            _jumpCooldownTimer -= Time.fixedDeltaTime;
        }

        if (_dropCooldownTimer > 0f)
        {
            _dropCooldownTimer -= Time.fixedDeltaTime;
        }
    }

    private float GetMoveDirection()
    {
        if (player == null)
        {
            return 0f;
        }

        var deltaX = player.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= 0.05f)
        {
            return 0f;
        }

        return Mathf.Sign(deltaX);
    }

    private void MoveTowardPlayer()
    {
        var horizontalDirection = GetMoveDirection();
        FaceMoveDirection(horizontalDirection);

        if (!CanMoveOnPlatform(horizontalDirection))
        {
            horizontalDirection = 0f;
        }

        var velocity = _body.linearVelocity;
        velocity.x = horizontalDirection * moveSpeed;
        _body.linearVelocity = velocity;
    }

    private bool ShouldJump(float horizontalDirection)
    {
        if (_jumpCooldownTimer > 0f || !IsGrounded())
        {
            return false;
        }

        if (player != null)
        {
            var delta = player.position - transform.position;
            if (delta.y >= jumpWhenPlayerAbove && Mathf.Abs(delta.x) <= jumpHorizontalRange)
            {
                return HasPlatformAboveNearPlayer();
            }
        }

        if (horizontalDirection == 0f)
        {
            return false;
        }

        return !HasPlatformAhead(horizontalDirection) && HasPlatformAheadAfterJump(horizontalDirection);
    }

    private void Jump()
    {
        var velocity = _body.linearVelocity;
        velocity.y = jumpForce;
        _body.linearVelocity = velocity;
        _jumpCooldownTimer = jumpCooldown;
    }

    private bool ShouldDropThroughPlatform()
    {
        if (player == null || _bodyCollider == null || _dropCooldownTimer > 0f || _dropTimer > 0f || !IsGrounded())
        {
            return false;
        }

        var deltaY = player.position.y - transform.position.y;
        return deltaY <= -dropWhenPlayerBelow && GetCurrentPlatform() != null;
    }

    private void DropThroughCurrentPlatform()
    {
        var platform = GetCurrentPlatform();
        if (platform == null)
        {
            return;
        }

        SetPlatformIgnored(platform, true);
        _dropTimer = dropThroughDuration;
        _dropCooldownTimer = dropCooldown;

        var velocity = _body.linearVelocity;
        velocity.y = Mathf.Min(velocity.y, -2f);
        _body.linearVelocity = velocity;
    }

    private void TickPlatformDrop()
    {
        if (_ignoredPlatform == null)
        {
            return;
        }

        _dropTimer -= Time.fixedDeltaTime;
        if (_dropTimer > 0f)
        {
            return;
        }

        SetPlatformIgnored(_ignoredPlatform, false);
        _ignoredPlatform = null;
    }

    private void SetPlatformIgnored(Collider2D platform, bool ignored)
    {
        if (_bodyCollider == null || platform == null)
        {
            return;
        }

        Physics2D.IgnoreCollision(_bodyCollider, platform, ignored);
        _ignoredPlatform = ignored ? platform : null;
    }

    private bool CanMoveOnPlatform(float horizontalDirection)
    {
        if (horizontalDirection == 0f)
        {
            return true;
        }

        return !IsGrounded() || HasPlatformAhead(horizontalDirection) || HasPlatformAheadAfterJump(horizontalDirection);
    }

    private bool IsGrounded()
    {
        var hit = GetCurrentPlatformHit();
        return hit.collider != null && hit.normal.y > 0.5f;
    }

    private Collider2D GetCurrentPlatform()
    {
        return GetCurrentPlatformHit().collider;
    }

    private RaycastHit2D GetCurrentPlatformHit()
    {
        var bounds = GetBodyBounds();
        var origin = new Vector2(bounds.center.x, bounds.min.y + 0.08f);
        return Physics2D.Raycast(origin, Vector2.down, groundedRayDistance, platformMask);
    }

    private bool HasPlatformAhead(float horizontalDirection)
    {
        var bounds = GetBodyBounds();
        var x = horizontalDirection > 0f
            ? bounds.max.x + edgeProbeDistance
            : bounds.min.x - edgeProbeDistance;
        var origin = new Vector2(x, bounds.min.y + 0.25f);
        var hit = Physics2D.Raycast(origin, Vector2.down, groundedRayDistance + 0.5f, platformMask);
        return hit.collider != null && hit.normal.y > 0.5f;
    }

    private bool HasPlatformAheadAfterJump(float horizontalDirection)
    {
        var bounds = GetBodyBounds();
        var originX = transform.position.x + horizontalDirection * horizontalSearchStep;
        var bestPointFound = false;

        for (var i = 1; i <= horizontalSearchSteps; i++)
        {
            var x = originX + horizontalDirection * horizontalSearchStep * i;
            var origin = new Vector2(x, bounds.max.y + platformRayStartHeight);
            var hit = Physics2D.Raycast(origin, Vector2.down, platformRayDistance, platformMask);
            if (hit.collider == null || hit.normal.y < 0.5f)
            {
                continue;
            }

            var heightDelta = hit.point.y - bounds.min.y;
            if (heightDelta > -0.4f && heightDelta <= jumpWhenPlayerAbove + 1.2f)
            {
                bestPointFound = true;
                break;
            }
        }

        return bestPointFound;
    }

    private bool HasPlatformAboveNearPlayer()
    {
        if (player == null)
        {
            return false;
        }

        var rayOriginY = player.position.y + platformRayStartHeight;
        var hit = Physics2D.Raycast(new Vector2(player.position.x, rayOriginY), Vector2.down, platformRayDistance, platformMask);
        return hit.collider != null && hit.normal.y > 0.5f && hit.point.y > transform.position.y;
    }

    private Bounds GetBodyBounds()
    {
        var collider2D = GetComponent<Collider2D>();
        return collider2D != null ? collider2D.bounds : new Bounds(transform.position, Vector3.one);
    }

    private bool CanAttackPlayer()
    {
        if (player == null)
        {
            return false;
        }

        var damageable = player.GetComponent<IDamageable>() ?? player.GetComponentInParent<IDamageable>();
        if (damageable != null && damageable.IsDead)
        {
            return false;
        }

        return Vector2.Distance(transform.position, player.position) <= attackRange;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        var playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void IgnorePlayerBodyCollision()
    {
        if (_bodyCollider == null || player == null || _collisionIgnoredPlayer == player)
        {
            return;
        }

        var playerColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (var playerCollider in playerColliders)
        {
            if (playerCollider == null || playerCollider.isTrigger)
            {
                continue;
            }

            Physics2D.IgnoreCollision(_bodyCollider, playerCollider, true);
        }

        _collisionIgnoredPlayer = player;
    }

    private void FacePosition(Vector3 targetPosition)
    {
        var deltaX = targetPosition.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= 0.01f)
        {
            return;
        }

        var shouldFaceRight = deltaX > 0f;
        if (shouldFaceRight == _facingRight)
        {
            return;
        }

        _facingRight = shouldFaceRight;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !_facingRight;
        }
        else
        {
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (_facingRight ? 1f : -1f);
            transform.localScale = scale;
        }

        MirrorDamageDealer();
    }

    private void FaceMoveDirection(float horizontalDirection)
    {
        if (horizontalDirection > 0f)
        {
            FacePosition(transform.position + Vector3.right);
        }
        else if (horizontalDirection < 0f)
        {
            FacePosition(transform.position + Vector3.left);
        }
        else if (player != null)
        {
            FacePosition(player.position);
        }
    }

    private void MirrorDamageDealer()
    {
        if (damageDealer == null)
        {
            return;
        }

        var damageTransform = damageDealer.transform;
        var localPosition = damageTransform.localPosition;
        localPosition.x = Mathf.Abs(localPosition.x) * (_facingRight ? 1f : -1f);
        damageTransform.localPosition = localPosition;
    }

    private void PlayIdle()
    {
        if (animator != null && !string.IsNullOrWhiteSpace(idleStateName))
        {
            animator.CrossFade(idleStateName, 0.05f);
        }
    }

    private void PlayAttack()
    {
        if (animator != null && !string.IsNullOrWhiteSpace(attackStateName))
        {
            animator.CrossFade(attackStateName, 0.05f);
        }
    }

    private void HandleDied()
    {
        _state = BossState.Dead;
        _body.linearVelocity = Vector2.zero;
        damageDealer?.DisableDamageWindow();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        var bounds = GetBodyBounds();
        Gizmos.DrawLine(new Vector3(bounds.center.x, bounds.min.y + 0.08f), new Vector3(bounds.center.x, bounds.min.y - groundedRayDistance));
    }
}
