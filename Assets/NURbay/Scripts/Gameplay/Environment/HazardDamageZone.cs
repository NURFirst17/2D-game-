using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class HazardDamageZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 1f;

    private float _damageTimer;

    private void Reset()
    {
        var trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void Update()
    {
        if (_damageTimer > 0f)
        {
            _damageTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider2D other)
    {
        if (_damageTimer > 0f || damage <= 0 || !other.CompareTag(playerTag))
        {
            return;
        }

        var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable.IsDead)
        {
            return;
        }

        damageable.TakeDamage(damage);
        _damageTimer = damageCooldown;
    }
}
    