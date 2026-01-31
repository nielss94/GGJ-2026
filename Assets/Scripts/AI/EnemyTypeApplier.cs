using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Optional: apply an EnemyType ScriptableObject to this GameObject at Start. Sets Health, NavMeshAgent speed,
/// and, if present, ContactDamage (melee) and RangedAttack (ranged) from the type.
/// </summary>
public class EnemyTypeApplier : MonoBehaviour
{
    [SerializeField] private EnemyType type;

    private void Start()
    {
        if (type == null) return;

        if (TryGetComponent(out Health health))
            health.SetMaxHealth(type.MaxHealth);

        if (TryGetComponent(out NavMeshAgent agent))
            agent.speed = type.MoveSpeed;

        if (TryGetComponent(out ContactDamage contact))
            contact.SetDamageAndCooldown(type.ContactDamage, type.ContactCooldown);

        if (TryGetComponent(out RangedAttack ranged))
            ranged.SetFromEnemyType(type.RangedDamage, type.RangedFireInterval, type.RangedProjectileSpeed);
    }
}
