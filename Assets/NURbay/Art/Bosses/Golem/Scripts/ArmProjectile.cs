using UnityEngine;

public class ArmProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private int damage = 15;
    [SerializeField] private float lifeTime = 4f;

    private Vector2 direction;
    private GameObject owner;

    public void Init(Vector2 shootDirection, float projectileSpeed, GameObject projectileOwner)
    {
        direction = shootDirection.normalized;
        speed = projectileSpeed;
        owner = projectileOwner;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner != null)
        {
            if (collision.gameObject == owner || collision.transform.IsChildOf(owner.transform))
                return;
        }

        IDamageable damageable = collision.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}