using System.Collections.Generic;
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

    [Header("One Way Platforms")]
    [SerializeField] private Collider2D[] solidColliders;
    [SerializeField] private LayerMask oneWayPlatformMask = 1 << 8;
    [SerializeField] private float oneWayStandTolerance = 0.08f;
    [SerializeField] private float oneWayPlatformProbePadding = 1.5f;
    [SerializeField] private float oneWayPlatformRayStartOffset = 0.08f;

    private Vector2 _velocity;
    private float _targetHorizontalVelocity;
    private bool _jumpRequested;
    private float _requestedJumpForce;
    private readonly Collider2D[] _oneWayPlatformBuffer = new Collider2D[16];
    private readonly HashSet<Collider2D> _ignoredOneWayPlatforms = new();
    private readonly HashSet<Collider2D> _visibleOneWayPlatforms = new();
    private readonly List<Collider2D> _releasedOneWayPlatforms = new();
    private readonly RaycastHit2D[] _oneWayPlatformRaycastHits = new RaycastHit2D[8];

    public bool IsGrounded => IsGroundedOnValidSurface();
    public bool IsCeilingTouched => IsTouchingSolidCeiling();
    public float VerticalVelocity => _velocity.y;
    public Vector2 Velocity => _velocity;
    public ObstacleChecker GroundChecker => groundChecker;

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (solidColliders == null || solidColliders.Length == 0)
        {
            solidColliders = GetComponents<Collider2D>();
        }

        SyncFromBody();
    }

    private void FixedUpdate()
    {
        SyncFromBody();
        _velocity.x = _targetHorizontalVelocity;
        HandleJumpRequest();
        RefreshOneWayPlatformCollisions();
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

    private bool IsGroundedOnValidSurface()
    {
        if (groundChecker == null)
        {
            return false;
        }

        var groundCollider = groundChecker.GetTouchingCollider();
        if (groundCollider == null)
        {
            return IsStandingOnAnyOneWayPlatform();
        }

        return !IsOneWayPlatform(groundCollider) || CanStandOnOneWayPlatform(groundCollider);
    }

    private bool IsTouchingSolidCeiling()
    {
        if (ceilChecker == null)
        {
            return false;
        }

        var ceilingCollider = ceilChecker.GetTouchingCollider();
        return ceilingCollider != null && !IsOneWayPlatform(ceilingCollider);
    }

    private void RefreshOneWayPlatformCollisions()
    {
        if (solidColliders == null || solidColliders.Length == 0)
        {
            return;
        }

        _visibleOneWayPlatforms.Clear();

        foreach (var ownCollider in solidColliders)
        {
            if (ownCollider == null || ownCollider.isTrigger)
            {
                continue;
            }

            var bounds = ownCollider.bounds;
            var probeSize = new Vector2(bounds.size.x + oneWayPlatformProbePadding, bounds.size.y + oneWayPlatformProbePadding);
            var platformFilter = new ContactFilter2D();
            platformFilter.SetLayerMask(oneWayPlatformMask);
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
        foreach (var ownCollider in solidColliders)
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
        if (solidColliders == null)
        {
            return false;
        }

        foreach (var ownCollider in solidColliders)
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
        var rayDistance = oneWayStandTolerance + oneWayPlatformRayStartOffset + Mathf.Max(0.05f, -_velocity.y * Time.fixedDeltaTime);
        var leftX = bounds.min.x + Mathf.Min(0.05f, bounds.extents.x);
        var rightX = bounds.max.x - Mathf.Min(0.05f, bounds.extents.x);
        var centerX = bounds.center.x;
        var startY = bounds.min.y + oneWayPlatformRayStartOffset;

        return IsPlatformBelowFoot(new Vector2(leftX, startY), rayDistance, platformCollider) ||
               IsPlatformBelowFoot(new Vector2(centerX, startY), rayDistance, platformCollider) ||
               IsPlatformBelowFoot(new Vector2(rightX, startY), rayDistance, platformCollider);
    }

    private bool IsStandingOnAnyOneWayPlatform()
    {
        if (solidColliders == null || _velocity.y > 0.01f)
        {
            return false;
        }

        var platformFilter = new ContactFilter2D();
        platformFilter.SetLayerMask(oneWayPlatformMask);
        platformFilter.useTriggers = false;

        foreach (var ownCollider in solidColliders)
        {
            if (ownCollider == null || ownCollider.isTrigger)
            {
                continue;
            }

            var bounds = ownCollider.bounds;
            var rayDistance = oneWayStandTolerance + oneWayPlatformRayStartOffset + Mathf.Max(0.05f, -_velocity.y * Time.fixedDeltaTime);
            var origin = new Vector2(bounds.center.x, bounds.min.y + oneWayPlatformRayStartOffset);
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
        platformFilter.SetLayerMask(oneWayPlatformMask);
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

        return (oneWayPlatformMask.value & (1 << targetCollider.gameObject.layer)) != 0;
    }
}
