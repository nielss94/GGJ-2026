using System;
using FMOD.Studio;
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

    [Header("FMOD (player only)")]
    [Tooltip("Optional FMOD event played on Start when this is the player. Has a 'health' parameter (0–1).")]
    [SerializeField] private FmodEventAsset healthMusicEvent;
    [Tooltip("Optional FMOD event played when the player takes damage.")]
    [SerializeField] private FmodEventAsset fmodPlayerHit;

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
    private EventInstance healthMusicInstance;
    private const string HealthParamName = "health";

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    /// <summary>True if this is the player Health (Inspector flag or GameObject has "Player" tag).</summary>
    public bool IsPlayer => isPlayer || gameObject.CompareTag("Player");

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
        if (IsPlayer)
        {
            EventBus.SetPlayerHealthProvider(() => (currentHealth, maxHealth));
            EventBus.RaisePlayerHealthChanged(currentHealth, maxHealth);
        }
    }

    private void OnDisable()
    {
        if (IsPlayer)
        {
            EventBus.ClearPlayerHealthProvider();
            StopAndReleaseHealthMusic();
        }
    }

    private void Start()
    {
        if (IsPlayer && healthMusicEvent != null && !healthMusicEvent.IsNull)
            StartHealthMusic();
    }

    private void StartHealthMusic()
    {
        if (!FMODUnity.RuntimeManager.IsInitialized) return;
        healthMusicInstance = FMODUnity.RuntimeManager.CreateInstance(healthMusicEvent.EventReference);
        if (!healthMusicInstance.isValid()) return;
        UpdateHealthMusicParameter();
        healthMusicInstance.start();
    }

    private void UpdateHealthMusicParameter()
    {
        if (!healthMusicInstance.isValid()) return;
        float t = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        healthMusicInstance.setParameterByName(HealthParamName, t);
    }

    private void StopAndReleaseHealthMusic()
    {
        if (!healthMusicInstance.isValid()) return;
        healthMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        healthMusicInstance.release();
        healthMusicInstance.clearHandle();
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
        if (IsPlayer)
        {
            EventBus.RaisePlayerHealthChanged(currentHealth, maxHealth);
            UpdateHealthMusicParameter();
            if (fmodPlayerHit != null && !fmodPlayerHit.IsNull && AudioService.Instance != null)
                AudioService.Instance.PlayOneShot(fmodPlayerHit, transform.position);
        }
        // Treat zero or near-zero as death (avoids floating-point edge cases)
        if (currentHealth <= 0f || currentHealth < 0.001f)
        {
            isDead = true;
            onDeath?.Invoke();
            if (IsPlayer) {
                Debug.Log("Player died");
                EventBus.RaisePlayerDied();
            }
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
        if (IsPlayer)
        {
            EventBus.RaisePlayerHealthChanged(currentHealth, maxHealth);
            UpdateHealthMusicParameter();
        }
    }

    /// <summary>
    /// Reset health to full and clear dead state. Use when starting a new run (e.g. after death screen "Start New Run").
    /// For player Health, also raises EventBus.PlayerHealthChanged.
    /// </summary>
    public void ResetToFull()
    {
        isDead = false;
        currentHealth = maxHealth;
        if (IsPlayer)
        {
            EventBus.RaisePlayerHealthChanged(currentHealth, maxHealth);
            UpdateHealthMusicParameter();
        }
    }
}
