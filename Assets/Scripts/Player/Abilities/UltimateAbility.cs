using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ultimate ability: charged by collecting drops from enemies. When enough drops are collected,
/// the ability becomes available. Using it creates a blast wave from the player that expands
/// over time; enemies are damaged only when the wave reaches them. Depletes all collected drops
/// and recharges when the player gathers enough again. Assign to a face button (e.g. Y) via PlayerAbilityManager.
/// </summary>
public class UltimateAbility : PlayerAbility
{
    [Header("Charge (drops)")]
    [Tooltip("Number of drops required on the mask before the ultimate can be used.")]
    [SerializeField] private int dropsRequired = 5;

    [Header("Blast")]
    [Tooltip("Radius of the blast wave (world units).")]
    [SerializeField] private float blastRadius = 8f;
    [Tooltip("Duration over which the wave expands from center to full radius (seconds).")]
    [SerializeField] private float blastDuration = 0.6f;
    [Tooltip("Damage dealt to each enemy when the wave hits them.")]
    [SerializeField] private float blastDamage = 50f;
    [Tooltip("Layers to damage (e.g. Enemy).")]
    [SerializeField] private LayerMask damageableLayers = ~0;
    [Tooltip("Layers to exclude (e.g. Player).")]
    [SerializeField] private LayerMask excludeLayers;

    [Header("Blast visual")]
    [Tooltip("Prefab to instantiate at blast origin (e.g. Quad with BlastWaveInstance). Expands then destroys itself.")]
    [SerializeField] private GameObject blastWavePrefab;
    [Tooltip("Optional: particle system prefab to spawn at blast origin when ultimate fires. Plays once then destroys itself.")]
    [SerializeField] private GameObject blastParticlesPrefab;

    [Header("FMOD")]
    [Tooltip("Optional FMOD event played when the ultimate (blast wave) is fired.")]
    [SerializeField] private FmodEventAsset fmodUltimate;
    [Tooltip("Optional FMOD event played when the blast wave hits an enemy (once per enemy).")]
    [SerializeField] private FmodEventAsset fmodUltimateHit;

    [Header("References")]
    [Tooltip("Player drop manager (add/hold/remove drops). If unset, resolved from player at runtime.")]
    [SerializeField] private PlayerDropManager dropManager;
    [Tooltip("Optional: used for knockback force on blasted enemies. If unset, resolved via FindFirstObjectByType.")]
    [SerializeField] private PlayerStats playerStats;

    private bool isBlastActive;
    private int debugExtraDrops;
    private float currentBlastRadius;
    private Vector3 blastOrigin;

    /// <summary>True while the blast wave is expanding. Use for visuals (e.g. UltimateBlastVisual).</summary>
    public bool IsBlastActive => isBlastActive;

    /// <summary>Current wave radius (0 when not active). Use for visuals.</summary>
    public float CurrentBlastRadius => currentBlastRadius;

    /// <summary>World position of the blast center. Use for visuals when not a child of the player.</summary>
    public Vector3 BlastOrigin => blastOrigin;

    private void Awake()
    {
        if (preferredSlot != PlayerAbilityManager.AbilitySlot.Y)
            preferredSlot = PlayerAbilityManager.AbilitySlot.Y;
    }

    private void OnEnable()
    {
        if (IsDesignatedPlayerAbility())
            EventBus.SetUltimateChargeProvider(GetUltimateCharge);
    }

    private void OnDisable()
    {
        if (IsDesignatedPlayerAbility())
            EventBus.ClearUltimateChargeProvider();
        isBlastActive = false;
    }

    private bool IsDesignatedPlayerAbility()
    {
        var manager = LevelProgressionManager.Instance;
        return manager != null && manager.DesignatedPlayer != null && PlayerTransform == manager.DesignatedPlayer;
    }

    /// <summary>Debug: add enough charge to use the ultimate once. Called by FillUltimateDebug.</summary>
    public void DebugFillUltimateCharge()
    {
        debugExtraDrops = Mathf.Max(debugExtraDrops, dropsRequired);
    }

    public override void ResetToBase()
    {
        debugExtraDrops = 0;
        base.ResetToBase();
    }

    private int GetEffectiveDropCount()
    {
        var manager = ResolveDropManager();
        int real = manager != null ? manager.GetTotalDropCount() : 0;
        return real + debugExtraDrops;
    }

    /// <summary>Current charge (drops collected) and required drops to use the ultimate. Use for UI (e.g. UltimateChargeDisplay).</summary>
    public (int current, int required) GetCharge()
    {
        return (GetEffectiveDropCount(), dropsRequired);
    }

    private (int current, int required) GetUltimateCharge()
    {
        return GetCharge();
    }

    public override bool CanPerform
    {
        get
        {
            if (isBlastActive) return false;
            return GetEffectiveDropCount() >= dropsRequired;
        }
    }

    public override bool TryPerform()
    {
        var manager = ResolveDropManager();
        if (manager == null || GetEffectiveDropCount() < dropsRequired || isBlastActive)
            return false;

        debugExtraDrops = 0;
        manager.ClearAllDrops();
        EventBus.RaiseUltimateUsed();
        if (fmodUltimate != null && !fmodUltimate.IsNull && AudioService.Instance != null)
            AudioService.Instance.PlayOneShot(fmodUltimate, PlayerTransform.position);
        isBlastActive = true;
        StartCoroutine(ExpandBlastWave());
        return true;
    }

    private IEnumerator ExpandBlastWave()
    {
        Vector3 origin = PlayerTransform.position;
        blastOrigin = origin;
        currentBlastRadius = 0f;

        if (blastWavePrefab != null)
        {
            GameObject instance = Instantiate(blastWavePrefab, origin, Quaternion.identity);
            var blastVisual = instance.GetComponentInChildren<BlastWaveInstance>();
            if (blastVisual != null)
                blastVisual.Init(blastDuration, blastRadius);
        }

        if (blastParticlesPrefab != null)
        {
            GameObject particlesInstance = Instantiate(blastParticlesPrefab, origin, Quaternion.identity);
            var ps = particlesInstance.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                float destroyDelay = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(particlesInstance, destroyDelay);
            }
            else
            {
                Destroy(particlesInstance);
            }
        }

        float knockbackForce = GetKnockbackForce();

        // Collect all potential targets within full radius (with their distance)
        var candidates = new List<(Health health, float distance)>();
        var seen = new HashSet<Health>();
        Collider[] overlaps = Physics.OverlapSphere(origin, blastRadius, damageableLayers);

        foreach (Collider c in overlaps)
        {
            if (c == null) continue;
            if (((1 << c.gameObject.layer) & excludeLayers) != 0) continue;
            if (c.transform.IsChildOf(PlayerTransform) || c.transform == PlayerTransform) continue;

            var health = c.GetComponentInParent<Health>();
            if (health == null || health.IsDead || health.IsPlayer || seen.Contains(health))
                continue;

            float distance = Vector3.Distance(origin, c.ClosestPoint(origin));
            candidates.Add((health, distance));
            seen.Add(health);
        }

        var damaged = new HashSet<Health>();
        float currentRadius = 0f;
        float elapsed = 0f;

        while (elapsed < blastDuration)
        {
            elapsed += Time.deltaTime;
            currentRadius = Mathf.Min(blastRadius, (elapsed / blastDuration) * blastRadius);
            currentBlastRadius = currentRadius;

            foreach (var (health, distance) in candidates)
            {
                if (health == null || health.IsDead || damaged.Contains(health)) continue;
                if (distance > currentRadius) continue;

                health.TakeDamage(blastDamage, origin, knockbackForce);
                damaged.Add(health);
                if (fmodUltimateHit != null && !fmodUltimateHit.IsNull && AudioService.Instance != null)
                    AudioService.Instance.PlayOneShot(fmodUltimateHit, health.transform.position);
            }

            yield return null;
        }

        // Final pass in case any were exactly at radius or missed by one frame
        foreach (var (health, distance) in candidates)
        {
            if (health == null || health.IsDead || damaged.Contains(health)) continue;
            if (distance > blastRadius) continue;
            health.TakeDamage(blastDamage, origin, knockbackForce);
            if (fmodUltimateHit != null && !fmodUltimateHit.IsNull && AudioService.Instance != null)
                AudioService.Instance.PlayOneShot(fmodUltimateHit, health.transform.position);
        }

        currentBlastRadius = 0f;
        isBlastActive = false;
    }

    private float GetKnockbackForce()
    {
        if (playerStats != null) return playerStats.KnockbackForce;
        var stats = FindFirstObjectByType<PlayerStats>();
        return stats != null ? stats.KnockbackForce : 1f;
    }

    private PlayerDropManager ResolveDropManager()
    {
        if (dropManager != null) return dropManager;
        return PlayerTransform.GetComponentInChildren<PlayerDropManager>(true);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(PlayerTransform != null ? PlayerTransform.position : transform.position, blastRadius);
    }
#endif
}
