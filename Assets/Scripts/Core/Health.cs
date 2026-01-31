using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple health component. Use on player and enemies. When current health reaches zero, invokes OnDeath
/// and, if IsPlayer is set, raises EventBus.PlayerDied.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool isPlayer;

    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;

    private float _currentHealth;
    private bool _isDead;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => _isDead;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage. Returns true if this instance was killed (health reached zero).
    /// </summary>
    public bool TakeDamage(float amount)
    {
        if (_isDead || amount <= 0f) return false;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        if (_currentHealth <= 0f)
        {
            _isDead = true;
            onDeath?.Invoke();
            if (isPlayer)
            {
                EventBus.RaisePlayerDied();
            }
            return true;
        }
        return false;
    }

    /// <summary>Set max health and reset current health (e.g. from EnemyType at spawn).</summary>
    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        _currentHealth = maxHealth;
    }

    /// <summary>
    /// Heal (clamped to max health).
    /// </summary>
    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f) return;
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
    }
}
