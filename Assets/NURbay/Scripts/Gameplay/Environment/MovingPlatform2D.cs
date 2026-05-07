using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    public enum MovementAxis
    {
        Horizontal,
        Vertical
    }

    [Header("Path")]
    [SerializeField] private MovementAxis axis = MovementAxis.Horizontal;
    [SerializeField] private float distance = 3f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private bool startFromTargetPoint;

    private Rigidbody2D _body;
    private Vector2 _pointA;
    private Vector2 _pointB;
    private Vector2 _currentTarget;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;

        var startPosition = (Vector2)transform.position;
        var offset = axis == MovementAxis.Horizontal
            ? Vector2.right * distance
            : Vector2.up * distance;

        _pointA = startPosition;
        _pointB = startPosition + offset;
        _currentTarget = startFromTargetPoint ? _pointA : _pointB;

        if (startFromTargetPoint)
        {
            _body.position = _pointB;
        }
    }

    private void FixedUpdate()
    {
        var nextPosition = Vector2.MoveTowards(_body.position, _currentTarget, speed * Time.fixedDeltaTime);
        _body.MovePosition(nextPosition);

        if (Vector2.Distance(nextPosition, _currentTarget) <= 0.01f)
        {
            _currentTarget = _currentTarget == _pointA ? _pointB : _pointA;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        var startPosition = transform.position;
        var offset = axis == MovementAxis.Horizontal
            ? Vector3.right * distance
            : Vector3.up * distance;

        Gizmos.DrawLine(startPosition, startPosition + offset);
        Gizmos.DrawWireCube(startPosition, Vector3.one * 0.4f);
        Gizmos.DrawWireCube(startPosition + offset, Vector3.one * 0.4f);
    }
}
