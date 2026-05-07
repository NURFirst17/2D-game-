using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private bool damageWindowActiveOnStart;

    private readonly HashSet<IDamageable> _damagedTargets = new();
    private Collider2D _trigger;
    private bool _damageWindowActive;
    private Transform _ownerRoot;

    private void Awake()
    {
        _trigger = GetComponent<Collider2D>();
        _trigger.isTrigger = true;
        _damageWindowActive = damageWindowActiveOnStart;
        _ownerRoot = transform.root;
    }

    private void OnEnable()
    {
        _damagedTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_damageWindowActive)
        {
            return;
        }

        TryDealDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_damageWindowActive)
        {
            return;
        }

        TryDealDamage(other);
    }

    public void EnableDamageWindow()
    {
        _damagedTargets.Clear();
        _damageWindowActive = true;
    }

    public void DisableDamageWindow()
    {
        _damageWindowActive = false;
        _damagedTargets.Clear();
    }

    private void TryDealDamage(Collider2D other)
    {
        if (other.transform.root == _ownerRoot)
        {
            return;
        }

        var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable.IsDead || _damagedTargets.Contains(damageable))
        {
            return;
        }

        damageable.TakeDamage(damage);
        _damagedTargets.Add(damageable);
    }
}
