using UnityEngine;

/// <summary>
/// Attack behaviour: deal damage at range. Either hitscan (raycast) or spawn a projectile.
/// Uses Enemy.PlayerTarget for aim. Add with ChaseMovement for enemies that chase and shoot.
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

    /// <summary>Set damage, fire interval, and projectile speed from EnemyType (e.g. via EnemyTypeApplier).</summary>
    public void SetFromEnemyType(float damage, float fireInterval, float projectileSpeedValue)
    {
        hitscanDamage = damage;
        projectileDamage = damage;
        fireInterval = fireInterval;
        projectileSpeed = projectileSpeedValue;
    }

    private Enemy _enemy;
    private float _nextFireTime;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
    }

    private void Update()
    {
        Transform target = _enemy != null ? _enemy.PlayerTarget : null;
        if (target == null || Time.time < _nextFireTime)
            return;

        float distSq = (target.position - transform.position).sqrMagnitude;
        if (range > 0f && distSq > range * range)
            return;

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

        _nextFireTime = Time.time + fireInterval;
    }
}
