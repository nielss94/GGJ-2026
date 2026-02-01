using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Telegraphed ranged attack: channel (telegraph) then fire. Requires line of sight; does not shoot through walls.
/// Stops movement during telegraph via EnemyAttackState. Uses Enemy.PlayerTarget for aim.
/// </summary>
public class RangedAttack : MonoBehaviour
{
    public enum AttackType
    {
        Hitscan,
        Projectile
    }

    [Header("Target & timing")]
    [Tooltip("Leave 0 for no range limit.")]
    [SerializeField] private float range = 15f;
    [SerializeField] private float fireInterval = 1.5f;
    [Tooltip("Telegraph/channel duration before firing. Player can see intent before shot.")]
    [SerializeField] private float telegraphDuration = 0.3f;

    [Header("Attack type")]
    [SerializeField] private AttackType attackType = AttackType.Hitscan;

    [Header("Hitscan")]
    [SerializeField] private float hitscanDamage = 15f;

    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileDamage = 15f;
    [Tooltip("Where to spawn the projectile. Leave empty to use this transform.")]
    [SerializeField] private Transform firePoint;

    [Header("Telegraph events (optional)")]
    [Tooltip("Fired when telegraph/channel starts. Use for aim warning VFX or sound.")]
    [SerializeField] private UnityEvent onTelegraphStarted;
    [Tooltip("Fired when telegraph ends and shot is fired.")]
    [SerializeField] private UnityEvent onTelegraphEnded;

    /// <summary>Set damage, fire interval, range, and projectile speed from EnemyType (e.g. via EnemyTypeApplier).</summary>
    public void SetFromEnemyType(float damage, float fireInterval, float projectileSpeedValue, float rangeValue = -1f)
    {
        hitscanDamage = damage;
        projectileDamage = damage;
        this.fireInterval = fireInterval;
        projectileSpeed = projectileSpeedValue;
        if (rangeValue >= 0f)
            range = rangeValue;
    }

    /// <summary>Set telegraph duration (e.g. from EnemyType).</summary>
    public void SetTelegraphDuration(float duration)
    {
        telegraphDuration = Mathf.Max(0.01f, duration);
    }

    private enum State { Idle, Telegraphing }

    private State state = State.Idle;
    private float telegraphEndTime;
    private Transform cachedTargetForTelegraph;
    private Enemy enemy;
    private EnemySight sight;
    private EnemyAttackState attackState;
    private EnemyAnimatorDriver animatorDriver;
    private float nextFireTime;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        sight = GetComponent<EnemySight>();
        attackState = GetComponent<EnemyAttackState>();
        animatorDriver = GetComponent<EnemyAnimatorDriver>();
    }

    private void Update()
    {
        Transform target = enemy != null ? enemy.PlayerTarget : null;

        if (state == State.Telegraphing)
        {
            if (Time.time >= telegraphEndTime)
            {
                if (attackState != null)
                    attackState.IsChanneling = false;
                onTelegraphEnded?.Invoke();
                if (cachedTargetForTelegraph != null)
                    FireAt(cachedTargetForTelegraph);
                cachedTargetForTelegraph = null;
                state = State.Idle;
                nextFireTime = Time.time + fireInterval;
            }
            return;
        }

        if (target == null || Time.time < nextFireTime)
            return;

        float distSq = (target.position - transform.position).sqrMagnitude;
        if (range > 0f && distSq > range * range)
            return;

        bool hasLos = sight == null || sight.HasLineOfSightTo(target);
        if (!hasLos)
            return;

        state = State.Telegraphing;
        telegraphEndTime = Time.time + telegraphDuration;
        cachedTargetForTelegraph = target;
        if (attackState != null)
            attackState.IsChanneling = true;
        onTelegraphStarted?.Invoke();
    }

    private void FireAt(Transform target)
    {
        animatorDriver?.SetAttackTrigger();
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 aimDir = (target.position - origin).normalized;

        switch (attackType)
        {
            case AttackType.Projectile:
                if (projectilePrefab != null)
                {
                    Projectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(aimDir));
                    p.Init(projectileDamage, projectileSpeed, gameObject);
                }
                break;
            case AttackType.Hitscan:
            default:
                float maxDist = range > 0f ? range : 1000f;
                if (Physics.Raycast(origin, aimDir, out RaycastHit hit, maxDist))
                {
                    var health = hit.collider.GetComponent<Health>();
                    if (health != null && !health.IsDead)
                        health.TakeDamage(hitscanDamage);
                }
                break;
        }
    }
}
