using UnityEngine;

public class LaserDamage : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private float lifeTime = 1f;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        IDamageable damageable = collision.GetComponent<IDamageable>();

        if (damageable == null)
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        damageable.TakeDamage(damage);
        lastDamageTime = Time.time;
    }
}