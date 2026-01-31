using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Telegraphed melee attack: channel (telegraph) then a short window where contact deals damage.
/// Requires line of sight; does not attack through walls. Stops movement during telegraph via EnemyAttackState.
/// </summary>
public class ContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float telegraphDuration = 0.4f;
    [SerializeField] private float attackActiveDuration = 0.25f;

    [Header("Telegraph events (optional)")]
    [Tooltip("Fired when telegraph/channel starts. Use for warning VFX or animation.")]
    [SerializeField] private UnityEvent onTelegraphStarted;
    [Tooltip("Fired when attack window opens (hitbox active). Use for swing VFX or sound.")]
    [SerializeField] private UnityEvent onAttackWindowOpened;

    /// <summary>Set damage and cooldown (e.g. from EnemyType at spawn).</summary>
    public void SetDamageAndCooldown(float dmg, float cd)
    {
        damage = dmg;
        cooldown = cd;
    }

    /// <summary>Set telegraph and range (e.g. from EnemyType).</summary>
    public void SetTelegraphConfig(float range, float telegraph, float active)
    {
        attackRange = Mathf.Max(0.1f, range);
        telegraphDuration = Mathf.Max(0.01f, telegraph);
        attackActiveDuration = Mathf.Max(0.01f, active);
    }

    private enum State { Idle, Telegraphing, AttackActive, Cooldown }

    private State state = State.Idle;
    private float stateEndTime;
    private bool dealtDamageThisSwing;
    private Enemy enemy;
    private EnemySight sight;
    private EnemyAttackState attackState;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        sight = GetComponent<EnemySight>();
        attackState = GetComponent<EnemyAttackState>();
    }

    private void Update()
    {
        Transform player = enemy != null ? enemy.PlayerTarget : null;

        switch (state)
        {
            case State.Idle:
                if (player == null) break;
                if (TryGetComponent(out Health myHealth) && myHealth.IsDead) break;
                float distSq = (player.position - transform.position).sqrMagnitude;
                bool inRange = distSq <= attackRange * attackRange;
                bool hasLos = sight == null || sight.HasLineOfSightTo(player);
                if (inRange && hasLos)
                {
                    state = State.Telegraphing;
                    stateEndTime = Time.time + telegraphDuration;
                    dealtDamageThisSwing = false;
                    if (attackState != null)
                        attackState.IsChanneling = true;
                    onTelegraphStarted?.Invoke();
                }
                break;

            case State.Telegraphing:
                if (Time.time >= stateEndTime)
                {
                    if (attackState != null)
                        attackState.IsChanneling = false;
                    state = State.AttackActive;
                    stateEndTime = Time.time + attackActiveDuration;
                    onAttackWindowOpened?.Invoke();
                }
                break;

            case State.AttackActive:
                if (Time.time >= stateEndTime)
                {
                    state = State.Cooldown;
                    stateEndTime = Time.time + cooldown;
                }
                break;

            case State.Cooldown:
                if (Time.time >= stateEndTime)
                    state = State.Idle;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryDamage(collision.gameObject);
    }

    private void TryDamage(GameObject other)
    {
        if (state != State.AttackActive || dealtDamageThisSwing)
            return;

        var health = other.GetComponent<Health>();
        if (health == null || health.IsDead)
            return;

        health.TakeDamage(damage);
        dealtDamageThisSwing = true;
        state = State.Cooldown;
        stateEndTime = Time.time + cooldown;
    }
}
