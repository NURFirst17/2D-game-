using UnityEngine;

public class ArmProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private int damage = 15;
    [SerializeField] private float lifeTime = 4f;

    private Vector2 direction;

    public void Init(Vector2 shootDirection, float projectileSpeed)
    {
        direction = shootDirection.normalized;
        speed = projectileSpeed;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable damageable = collision.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
    }
}