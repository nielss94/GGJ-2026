using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Context passed when this Health takes damage. Used for hit feedback (flash) and knockback.
/// </summary>
public struct DamageInfo
{
    public float Amount;
    public Vector3? KnockbackSourcePosition;
    public float KnockbackMultiplier;

    public DamageInfo(float amount, Vector3? knockbackSourcePosition = null, float knockbackMultiplier = 1f)
    {
        Amount = amount;
        KnockbackSourcePosition = knockbackSourcePosition;
        KnockbackMultiplier = knockbackMultiplier;
    }
}

/// <summary>
/// Simple health component. Use on player and enemies. When current health reaches zero, invokes OnDeath
/// and, if IsPlayer is set, raises EventBus.PlayerDied. When IsPlayer, raises EventBus.PlayerHealthChanged on change
/// and registers a health provider so UI can get initial value without a direct reference.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool isPlayer;

    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;

    /// <summary>Add a runtime listener for death (e.g. EnemyDropper). Same as wiring OnDeath in the inspector.</summary>
    public void AddOnDeathListener(UnityAction callback)
    {
        if (callback != null)
            onDeath.AddListener(callback);
    }

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsPlayer => isPlayer;

    /// <summary>Raised when this Health dies. Use for local listeners (e.g. level enemy registration).</summary>
    public event Action Died;

    /// <summary>Raised when this Health takes damage (before death check). Use for hit flash, knockback, etc.</summary>
    public event Action<DamageInfo> Damaged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        if (isPlayer)
        {
            EventBus.SetPlayerHealthProvider(() => (currentHealth, maxHealth));
            EventBus.RaisePlayerHealthChanged(currentHealth, maxHealth);
        }
    }

    private void OnDisable()
    {
        if (isPlayer)
            EventBus.ClearPlayerHealthProvider();
    }

    /// <summary>
    /// Apply damage. Returns true if this instance was killed (health reached zero).
    /// </summary>
    public bool TakeDamage(float amount)
    {
        return TakeDamage(amount, null, 1f);
    }

    /// <summary>
    /// Apply damage with optional knockback context. Use for player attacks so enemies can flash and be knocked back.
    /// knockbackMultiplier can later be driven by a player stat upgrade.
    /// </summary>
    public bool TakeDamage(float amount, Vector3? knockbackSourcePosition, float knockbackMultiplier = 1f)
    {
        if (isDead || amount <= 0f) return false;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        var info = new DamageInfo(amount, knockbackSourcePosition, knockbackMultiplier);
        Damaged?.Invoke(info);
        if (isPlayer)
            EventBus.RaisePlayerHealthChanged(currentHealth, maxHealth);
        if (currentHealth <= 0f)
        {
            isDead = true;
            onDeath?.Invoke();
            if (isPlayer)
                EventBus.RaisePlayerDied();
            else
            {
                Died?.Invoke();
                EventBus.RaiseEnemyDied();
            }
            return true;
        }
        return false;
    }

    /// <summary>Set max health and reset current health (e.g. from EnemyType at spawn).</summary>
    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Heal (clamped to max health).
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (isPlayer)
            EventBus.RaisePlayerHealthChanged(currentHealth, maxHealth);
    }
}
