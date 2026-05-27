using UnityEngine;

public class ObstacleChecker : MonoBehaviour
{
    private const float MinCheckSize = 0.05f;

    [SerializeField] private LayerMask _mask;
    [SerializeField] private Collider2D _collider;
    [SerializeField] private Vector2 _direction;
    
    [SerializeField] private float _distanceToCheck;
    
    public bool IsTouches()
    {
        return GetTouchingCollider() != null;
    }

    public Collider2D GetTouchingCollider()
    {
        if (_collider == null)
        {
            return null;
        }

        var direction = _direction.sqrMagnitude > 0f ? _direction.normalized : Vector2.down;
        return GetTouchingColliderAt((Vector2)transform.position, direction);
    }

    public bool IsTouchesAt(Vector2 worldPosition, Vector2 direction)
    {
        return GetTouchingColliderAt(worldPosition, direction) != null;
    }

    public Collider2D GetTouchingColliderAt(Vector2 worldPosition, Vector2 direction)
    {
        if (_collider == null)
        {
            return null;
        }

        var bounds = _collider.bounds;
        var normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.down;
        var checkCenter = worldPosition + normalizedDirection * _distanceToCheck;
        var checkSize = GetCheckSize(bounds.size, normalizedDirection);

        return Physics2D.OverlapBox(checkCenter, checkSize, 0f, _mask);
    }

    private static Vector2 GetCheckSize(Vector2 colliderSize, Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > 0.5f)
        {
            return new Vector2(Mathf.Max(MinCheckSize, colliderSize.x * 0.75f), Mathf.Max(MinCheckSize, colliderSize.y * 0.08f));
        }

        return new Vector2(Mathf.Max(MinCheckSize, colliderSize.x * 0.08f), Mathf.Max(MinCheckSize, colliderSize.y * 0.75f));
    }

    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        _direction = direction.normalized;
    }
}
