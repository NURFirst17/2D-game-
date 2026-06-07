using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class GolemBossAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Melee Attack")]
    [SerializeField] private Transform meleePoint;
    [SerializeField] private float meleeRadius = 1.5f;
    [SerializeField] private int meleeDamage = 20;
    [SerializeField] private float meleeDamageDelay = 0.4f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Arm Projectile")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject armProjectilePrefab;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float shootDelay = 0.4f;

    [Header("Laser Attack")]
    [SerializeField] private Transform laserPoint;
    [SerializeField] private GameObject laserChargeEffectPrefab;
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private float minLaserRange = 9f;
    [SerializeField] private float laserRange = 16f;
    [SerializeField] private float laserDelay = 0.8f;
    [SerializeField] private float laserEndDelay = 0.8f;

    [Header("AI Settings")]
    [SerializeField] private float meleeRange = 4f;
    [SerializeField] private float minShootRange = 5f;
    [SerializeField] private float shootRange = 12f;
    [SerializeField] private float minAttackCooldown = 3f;
    [SerializeField] private float maxAttackCooldown = 5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 6f;
    [SerializeField] private float verticalOffset = 1f;

    [Header("Phase Two")]
    [SerializeField] private float phaseTwoTransitionTime = 1.5f;
    [SerializeField] private float phaseTwoMinCooldown = 2f;
    [SerializeField] private float phaseTwoMaxCooldown = 3f;

    [Header("Phase Two Cutscene")]
    [SerializeField] private CinemachineCamera followCamera;
    [SerializeField] private Transform cameraFocusPoint;
    [SerializeField] private Transform phaseTwoPoint;
    [SerializeField] private float phaseTwoMoveSpeed = 4f;
    [SerializeField] private float cameraReturnTime = 0.8f;

    [Header("Timing")]
    [SerializeField] private float meleeEndDelay = 0.6f;
    [SerializeField] private float shootEndDelay = 0.6f;

    private Animator animator;
    private Rigidbody2D rb;
    private bool isAttacking;
    private float nextAttackTime;
    private bool facingRight = true;
    private bool isPhaseTwo;
    private bool isTransitioning;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (player == null)
            return;

        FlipToPlayer();
        MoveToPlayer();

        if (isAttacking || isTransitioning)
            return;

        if (Time.time < nextAttackTime)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= meleeRange)
        {
            StartMeleeAttack();
        }
        else if (isPhaseTwo && distanceToPlayer >= minLaserRange && distanceToPlayer <= laserRange)
        {
            StartLaserAttack();
        }
        else if (!isPhaseTwo && distanceToPlayer >= minShootRange && distanceToPlayer <= shootRange)
        {
            StartArmShoot();
        }
    }

    public void StartPhaseTwo()
    {
        if (isPhaseTwo || isTransitioning)
            return;

        StartCoroutine(PhaseTwoRoutine());
    }

    private IEnumerator PhaseTwoRoutine()
    {
        isTransitioning = true;
        isAttacking = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Transform previousTarget = null;

        if (followCamera != null)
        {
            previousTarget = followCamera.Follow;
            followCamera.Follow = cameraFocusPoint;
        }

        if (phaseTwoPoint != null)
        {
            while (Vector2.Distance(transform.position, phaseTwoPoint.position) > 0.05f)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    phaseTwoPoint.position,
                    phaseTwoMoveSpeed * Time.deltaTime
                );

                yield return null;
            }
        }

        if (animator != null)
            animator.SetTrigger("PhaseTwo");

        yield return new WaitForSeconds(phaseTwoTransitionTime);

        isPhaseTwo = true;

        minAttackCooldown = phaseTwoMinCooldown;
        maxAttackCooldown = phaseTwoMaxCooldown;

        if (followCamera != null)
            followCamera.Follow = previousTarget;

        yield return new WaitForSeconds(cameraReturnTime);

        isAttacking = false;
        isTransitioning = false;

        SetNextAttackCooldown();
    }

    private void StartMeleeAttack()
    {
        isAttacking = true;
        SetNextAttackCooldown();

        animator.SetTrigger("MeleeAttack");
        StartCoroutine(MeleeDamageRoutine());
    }

    private IEnumerator MeleeDamageRoutine()
    {
        yield return new WaitForSeconds(meleeDamageDelay);

        Collider2D hit = Physics2D.OverlapCircle(meleePoint.position, meleeRadius, playerLayer);

        if (hit != null)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
                damageable.TakeDamage(meleeDamage);
        }

        yield return new WaitForSeconds(meleeEndDelay);

        isAttacking = false;
    }

    private void StartArmShoot()
    {
        isAttacking = true;
        SetNextAttackCooldown();

        animator.SetTrigger("ArmShoot");
        StartCoroutine(ArmShootRoutine());
    }

    private IEnumerator ArmShootRoutine()
    {
        yield return new WaitForSeconds(shootDelay);

        if (armProjectilePrefab != null && shootPoint != null && player != null)
        {
            Vector2 direction = (player.position - shootPoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;

            GameObject projectile = Instantiate(
                armProjectilePrefab,
                shootPoint.position,
                Quaternion.Euler(0f, 0f, angle)
            );

            ArmProjectile armProjectile = projectile.GetComponent<ArmProjectile>();

            if (armProjectile != null)
                armProjectile.Init(direction, projectileSpeed, gameObject);
        }

        yield return new WaitForSeconds(shootEndDelay);

        isAttacking = false;
    }

    private void StartLaserAttack()
    {
        isAttacking = true;
        SetNextAttackCooldown();

        animator.SetTrigger("Laser");
        StartCoroutine(LaserRoutine());
    }

    private IEnumerator LaserRoutine()
    {
        if (laserPoint != null && laserChargeEffectPrefab != null)
        {
            Instantiate(laserChargeEffectPrefab, laserPoint.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(laserDelay);

        if (laserPrefab != null && laserPoint != null && player != null)
        {
            Vector2 direction = (player.position - laserPoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject laser = Instantiate(
                laserPrefab,
                laserPoint.position,
                Quaternion.Euler(0f, 0f, angle)
            );

            LaserDamage laserDamage = laser.GetComponent<LaserDamage>();

            if (laserDamage != null)
                laserDamage.SetOwner(gameObject);
        }

        yield return new WaitForSeconds(laserEndDelay);

        isAttacking = false;
    }

    private void SetNextAttackCooldown()
    {
        nextAttackTime = Time.time + Random.Range(minAttackCooldown, maxAttackCooldown);
    }

    private void FlipToPlayer()
    {
        if (player.position.x > transform.position.x && !facingRight)
            Flip();
        else if (player.position.x < transform.position.x && facingRight)
            Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void MoveToPlayer()
    {
        if (player == null || isAttacking || isTransitioning)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = new Vector2(
            player.position.x,
            player.position.y + verticalOffset
        );

        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        rb.linearVelocity = direction * moveSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        if (meleePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePoint.position, meleeRadius);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shootRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, laserRange);
    }
}