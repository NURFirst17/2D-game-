using UnityEngine;

public class LaserDamage : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private float lifeTime = 1f;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime;
    private GameObject owner;

    public void SetOwner(GameObject laserOwner)
    {
        owner = laserOwner;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (owner != null)
        {
            if (collision.gameObject == owner || collision.transform.IsChildOf(owner.transform))
                return;
        }

        IDamageable damageable = collision.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        damageable.TakeDamage(damage);
        lastDamageTime = Time.time;
    }
}