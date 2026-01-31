using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Optional: apply an EnemyType ScriptableObject to this GameObject at Start. Sets Health, NavMeshAgent speed,
/// telegraph/range config, and, if present, ContactDamage (melee) and RangedAttack (ranged). Ensures EnemySight
/// and EnemyAttackState exist for telegraphed attacks and line-of-sight.
/// </summary>
public class EnemyTypeApplier : MonoBehaviour
{
    [SerializeField] private EnemyType type;

    /// <summary>Power cost for spawn budget (from EnemyType). Used by Encounter when spawning.</summary>
    public int PowerCost => type != null ? type.PowerCost : 0;

    private void Start()
    {
        if (type == null) return;

        if (TryGetComponent(out Health health))
            health.SetMaxHealth(type.MaxHealth);

        if (TryGetComponent(out NavMeshAgent agent))
            agent.speed = type.MoveSpeed;

        if (!TryGetComponent(out EnemySight _))
            gameObject.AddComponent<EnemySight>();
        if (!TryGetComponent(out EnemyAttackState _))
            gameObject.AddComponent<EnemyAttackState>();

        if (TryGetComponent(out ContactDamage contact))
        {
            contact.SetDamageAndCooldown(type.ContactDamage, type.ContactCooldown);
            contact.SetTelegraphConfig(type.MeleeAttackRange, type.MeleeTelegraphDuration, type.MeleeAttackActiveDuration);
        }

        if (TryGetComponent(out RangedAttack ranged))
        {
            ranged.SetFromEnemyType(type.RangedDamage, type.RangedFireInterval, type.RangedProjectileSpeed, type.RangedAttackRange);
            ranged.SetTelegraphDuration(type.RangedTelegraphDuration);
        }

        if (TryGetComponent(out ChaseMovement chase))
        {
            float stopRange = 0f;
            if (GetComponent<ContactDamage>() != null)
                stopRange = type.MeleeAttackRange;
            else if (GetComponent<RangedAttack>() != null)
                stopRange = type.RangedAttackRange;
            chase.SetStopWhenInAttackRange(stopRange);
        }
    }
}
