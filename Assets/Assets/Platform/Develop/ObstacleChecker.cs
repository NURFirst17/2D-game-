using UnityEngine;

public class ObstacleChecker : MonoBehaviour
{
    [SerializeField] private LayerMask _mask;
    [SerializeField] private CapsuleCollider2D _collider;
    [SerializeField] private Vector2 _direction;
    
    [SerializeField] private float _distanceToCheck;
    
    public bool IsTouches()
        => Physics2D.CapsuleCast(_collider.bounds.center, _collider.size, _collider.direction, 0, _direction, _distanceToCheck, _mask).collider != null;
}
