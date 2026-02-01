using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Applies knockback when this object's Health takes damage with knockback context.
/// Knockback strength is based on the Health's max health (heavier enemies move less).
/// Max health is set at runtime by EnemyTypeApplier from the prefab's EnemyType, so each enemy
/// type gets knockback scaled by its type's MaxHealth. referenceMaxHealth is a tuning constant
/// (set in inspector) that defines the curve; it is not read from EnemyType.
/// knockbackMultiplier is passed from the damage source; later it can come from a player stat upgrade.
/// Works with NavMeshAgent: disables agent during knockback, then warps back onto the navmesh.
/// </summary>
[RequireComponent(typeof(Health))]
public class KnockbackReceiver : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("Base knockback distance (meters). Actual = base * strength * knockbackMultiplier, where strength = referenceMaxHealth / (referenceMaxHealth + health.MaxHealth).")]
    [SerializeField] private float baseKnockbackDistance = 4f;
    [Tooltip("Tuning constant for the formula (set here, not from EnemyType). Enemies whose MaxHealth equals this value get 50% of base distance. Higher reference = more knockback for all; lower = less. health.MaxHealth comes from EnemyTypeApplier at Start.")]
    [SerializeField] private float referenceMaxHealth = 100f;
    [Tooltip("Duration over which knockback is applied (seconds).")]
    [SerializeField] private float knockbackDuration = 0.2f;
    [Tooltip("Curve: x = time 0..1, y = fraction of displacement applied (0 at start, 1 at end).")]
    [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private Health health;
    private NavMeshAgent agent;
    private bool applyingKnockback;

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
    }

    private void OnDamaged(DamageInfo info)
    {
        if (applyingKnockback || !info.KnockbackSourcePosition.HasValue) return;
        if (health.IsDead) return;

        Vector3 source = info.KnockbackSourcePosition.Value;
        Vector3 dir = (transform.position - source).normalized;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        float strength = GetKnockbackStrength(health.MaxHealth) * info.KnockbackMultiplier;
        float distance = baseKnockbackDistance * strength;
        StartCoroutine(ApplyKnockbackRoutine(dir * distance));
    }

    /// <summary>Strength 0..1 based on max health. Heavier (higher max health) = less knockback.</summary>
    private float GetKnockbackStrength(float maxHealth)
    {
        if (referenceMaxHealth <= 0f) return 1f;
        return referenceMaxHealth / (referenceMaxHealth + maxHealth);
    }

    private IEnumerator ApplyKnockbackRoutine(Vector3 totalDisplacement)
    {
        applyingKnockback = true;
        bool hadAgent = agent != null && agent.enabled;
        if (hadAgent)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.enabled = false;
        }

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / knockbackDuration);
            float curveValue = knockbackCurve.Evaluate(t);
            transform.position = startPos + totalDisplacement * curveValue;
            yield return null;
        }

        transform.position = startPos + totalDisplacement;

        if (hadAgent && agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
                agent.Warp(transform.position);
            else if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, agent.areaMask))
                agent.Warp(hit.position);
        }

        applyingKnockback = false;
    }
}
