using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Light attack: 3-hit sword combo (right, left, right). Preferred slot X.
/// No walking during attacks; each swing moves the player forward. Dodge (dash) cancels attack and applies cooldown.
/// Hitbox is instant per swing; Size/AttackSpeed/Cooldown/Damage come from stats and upgrades.
/// </summary>
public class LightAttackAbility : PlayerAbility
{
    private enum State
    {
        Idle,
        Swing0,
        Swing1,
        Swing2,
        BetweenSwings0,
        BetweenSwings1
    }

    [Header("Light attack stat IDs (assign same assets as in upgrade definitions)")]
    [SerializeField] private AbilityStatId sizeStatId;
    [SerializeField] private AbilityStatId attackSpeedStatId;
    [SerializeField] private AbilityStatId cooldownStatId;
    [SerializeField] private AbilityStatId damageStatId;

    [Header("Light attack parameters")]
    [Tooltip("Size of the sword swing (hitbox scale).")]
    [SerializeField] private float size = 1f;
    [Tooltip("Higher = faster swings and shorter minimum time between combo hits.")]
    [SerializeField] private float attackSpeed = 1f;
    [Tooltip("Cooldown between combos (seconds), applied after combo ends or dodge cancel.")]
    [SerializeField] private float cooldown = 1f;
    [Tooltip("Damage per swing (base). Each swing is multiplied by Damage Multiplier Per Swing.")]
    [SerializeField] private float damage = 10f;
    [Tooltip("Multiplier per swing (X=swing 1, Y=swing 2, Z=swing 3). e.g. (1, 1.2, 1.5) makes 2nd and 3rd stronger.")]
    [SerializeField] private Vector3 damageMultiplierPerSwing = new Vector3(1f, 1.2f, 1.5f);

    [Header("Combo timing (designer)")]
    [Tooltip("Base duration of each swing (animation time). Actual = base / attackSpeed.")]
    [SerializeField] private float baseSwingDuration = 0.3f;
    [Tooltip("Base minimum time before next attack can be input. Actual = base / attackSpeed.")]
    [SerializeField] private float baseMinTimeBetweenHits = 0.2f;
    [Tooltip("Max time after a swing to press attack again to continue combo. After this, combo resets (no cooldown). Cooldown only applies after the 3rd swing or dodge cancel.")]
    [SerializeField] private float comboLinkWindow = 1f;

    [Header("Hitbox (designer)")]
    [Tooltip("Optional. Forward direction for attack (e.g. character model). Otherwise uses player root forward.")]
    [SerializeField] private Transform attackDirectionSource;
    [Tooltip("Shape of the instant hitbox.")]
    [SerializeField] private HitboxShape hitboxShape = HitboxShape.Sphere;
    [Tooltip("Center of hitbox in front of player (meters).")]
    [SerializeField] private float hitboxOffset = 0.5f;
    [Tooltip("Radius for sphere; half-extents for box. Scaled by Size.")]
    [SerializeField] private Vector3 hitboxSize = new Vector3(0.5f, 0.5f, 0.5f);
    [Tooltip("Layers to damage (e.g. Enemy).")]
    [SerializeField] private LayerMask damageableLayers = ~0;
    [Tooltip("Exclude these (e.g. Player layer) so we don't hit ourselves.")]
    [SerializeField] private LayerMask excludeLayers;

    [Header("Movement (designer)")]
    [Tooltip("Distance to move forward per swing (meters).")]
    [SerializeField] private float forwardStepPerSwing = 0.3f;

    [Header("Debug")]
    [Tooltip("Draw hitbox in Scene view when this object is selected.")]
    [SerializeField] private bool drawHitboxGizmo = true;
    [Tooltip("Log combo state, hits and cooldown to console.")]
    [SerializeField] private bool enableLogging;

    [Header("FMOD (optional)")]
    [Tooltip("Played at start of swing 1.")]
    [SerializeField] private FmodEventAsset fmodSwing1;
    [Tooltip("Played at start of swing 2.")]
    [SerializeField] private FmodEventAsset fmodSwing2;
    [Tooltip("Played at start of swing 3.")]
    [SerializeField] private FmodEventAsset fmodSwing3;
    [Tooltip("Optional: played when an enemy is damaged (hook for later).")]
    [SerializeField] private FmodEventAsset fmodOnHit;

    public float Size => size;
    public float AttackSpeed => attackSpeed;
    public float Cooldown => cooldown;
    public float Damage => damage;

    /// <summary>Current combo state and timer countdowns for debug (e.g. VoodooDebug).</summary>
    public string GetDebugStatus()
    {
        float now = Time.time;
        if (state == State.Swing0 || state == State.Swing1 || state == State.Swing2)
            return $"[LightAttack] {state} — swing ends in {Mathf.Max(0f, swingEndTime - now):F2}s";
        if (state == State.BetweenSwings0 || state == State.BetweenSwings1)
        {
            float nextIn = Mathf.Max(0f, nextSwingAllowedAt - now);
            float windowClosesIn = Mathf.Max(0f, comboWindowEndTime - now);
            return $"[LightAttack] {state} — next in {nextIn:F2}s, window {windowClosesIn:F2}s";
        }
        if (state == State.Idle && cooldownUntil > now)
            return $"[LightAttack] Idle — cooldown {cooldownUntil - now:F2}s";
        return $"[LightAttack] {state} — ready";
    }

    private State state = State.Idle;
    private float cooldownUntil = float.NegativeInfinity;
    private float swingEndTime;
    private float nextSwingAllowedAt;
    private float comboWindowEndTime;

    private void Reset()
    {
        preferredSlot = PlayerAbilityManager.AbilitySlot.X;
        abilityName = "Light Attack";
    }

    private void OnEnable()
    {
        EventBus.PlayerDashStarted += OnPlayerDashStarted;
    }

    private void OnDisable()
    {
        EventBus.PlayerDashStarted -= OnPlayerDashStarted;
        if (state != State.Idle)
        {
            EventBus.RaisePlayerInputUnblockRequested(this);
            state = State.Idle;
        }
    }

    private void OnPlayerDashStarted(object source)
    {
        if (state == State.Idle) return;
        if (enableLogging) Debug.Log("[LightAttack] Dodge cancel: ending combo and applying cooldown.");
        EndComboAndApplyCooldown();
    }

    public override void ApplyUpgradeValue(AbilityStatId statId, float value)
    {
        TryApplyUpgrade(sizeStatId, statId, value, v => size += v);
        TryApplyUpgrade(attackSpeedStatId, statId, value, v => attackSpeed += v);
        TryApplyUpgrade(cooldownStatId, statId, value, v => cooldown += v);
        TryApplyUpgrade(damageStatId, statId, value, v => damage += v);
    }

    public override bool CanPerform
    {
        get
        {
            if (state == State.Swing0 || state == State.Swing1 || state == State.Swing2)
                return false;
            if (state == State.Idle)
                return Time.time >= cooldownUntil && base.CanPerform;
            if (state == State.BetweenSwings0 || state == State.BetweenSwings1)
                return Time.time >= nextSwingAllowedAt && Time.time <= comboWindowEndTime && base.CanPerform;
            return false;
        }
    }

    public override bool TryPerform()
    {
        if (!base.CanPerform) return false;

        switch (state)
        {
            case State.Idle:
                if (Time.time < cooldownUntil) return false;
                if (enableLogging) Debug.Log("[LightAttack] Combo started (swing 1/3).");
                StartSwing(0);
                return true;
            case State.BetweenSwings0:
                if (Time.time < nextSwingAllowedAt || Time.time > comboWindowEndTime) return false;
                StartSwing(1);
                return true;
            case State.BetweenSwings1:
                if (Time.time < nextSwingAllowedAt || Time.time > comboWindowEndTime) return false;
                StartSwing(2);
                return true;
            default:
                return false;
        }
    }

    private void Update()
    {
        float now = Time.time;

        switch (state)
        {
            case State.Swing0:
            case State.Swing1:
            case State.Swing2:
                if (now >= swingEndTime)
                    OnSwingEnd();
                break;
            case State.BetweenSwings0:
            case State.BetweenSwings1:
                if (now > comboWindowEndTime)
                {
                    if (enableLogging) Debug.Log("[LightAttack] Combo link window expired. Resetting (no cooldown).");
                    EndComboWithoutCooldown();
                }
                break;
        }
    }

    private void StartSwing(int comboIndex)
    {
        EventBus.RaisePlayerInputBlockRequested(this);

        state = comboIndex == 0 ? State.Swing0 : (comboIndex == 1 ? State.Swing1 : State.Swing2);

        float speedFactor = Mathf.Max(0.01f, attackSpeed);
        float swingDuration = baseSwingDuration / speedFactor;
        swingEndTime = Time.time + swingDuration;

        ApplyHitbox(comboIndex + 1);
        ApplyForwardStep();
        PlaySwingSound(comboIndex);
        if (enableLogging) Debug.Log($"[LightAttack] Swing {comboIndex + 1}/3 started (duration={swingDuration:F2}s).");
    }

    private void OnSwingEnd()
    {
        float speedFactor = Mathf.Max(0.01f, attackSpeed);
        float minBetween = baseMinTimeBetweenHits / speedFactor;
        float now = Time.time;

        if (state == State.Swing2)
        {
            if (enableLogging) Debug.Log("[LightAttack] Combo complete (3/3). Applying cooldown.");
            EndComboAndApplyCooldown();
            return;
        }

        if (state == State.Swing0)
        {
            state = State.BetweenSwings0;
            nextSwingAllowedAt = now + minBetween;
            comboWindowEndTime = now + comboLinkWindow;
            EventBus.RaisePlayerInputUnblockRequested(this);
        }
        else if (state == State.Swing1)
        {
            state = State.BetweenSwings1;
            nextSwingAllowedAt = now + minBetween;
            comboWindowEndTime = now + comboLinkWindow;
            EventBus.RaisePlayerInputUnblockRequested(this);
        }
    }

    private void EndComboAndApplyCooldown()
    {
        state = State.Idle;
        cooldownUntil = Time.time + Mathf.Max(0f, cooldown);
        EventBus.RaisePlayerInputUnblockRequested(this);
    }

    private void EndComboWithoutCooldown()
    {
        state = State.Idle;
        EventBus.RaisePlayerInputUnblockRequested(this);
    }

    private void ApplyHitbox(int swingNumber)
    {
        Vector3 origin = GetHitboxOrigin();
        Vector3 forward = GetAttackForward();
        float scale = Mathf.Max(0.01f, size);
        float mult = GetDamageMultiplierForSwing(swingNumber);
        float damageThisSwing = damage * mult;

        if (hitboxShape == HitboxShape.Sphere)
        {
            float radius = (hitboxSize.x + hitboxSize.y + hitboxSize.z) / 3f * scale;
            Collider[] hits = Physics.OverlapSphere(origin, radius, damageableLayers);
            int damagedCount = DamageTargets(hits, damageThisSwing);
            if (enableLogging) Debug.Log($"[LightAttack] Swing {swingNumber} hitbox (sphere r={radius:F2}): {hits?.Length ?? 0} overlaps, {damagedCount} damaged.");
        }
        else
        {
            Vector3 halfExtents = Vector3.Scale(hitboxSize, new Vector3(scale, scale, scale)) * 0.5f;
            Quaternion orientation = Quaternion.LookRotation(forward);
            Collider[] hits = Physics.OverlapBox(origin, halfExtents, orientation, damageableLayers);
            int damagedCount = DamageTargets(hits, damageThisSwing);
            if (enableLogging) Debug.Log($"[LightAttack] Swing {swingNumber} hitbox (box): {hits?.Length ?? 0} overlaps, {damagedCount} damaged.");
        }
    }

    private float GetDamageMultiplierForSwing(int swingNumber)
    {
        if (swingNumber == 1) return Mathf.Max(0.01f, damageMultiplierPerSwing.x);
        if (swingNumber == 2) return Mathf.Max(0.01f, damageMultiplierPerSwing.y);
        if (swingNumber == 3) return Mathf.Max(0.01f, damageMultiplierPerSwing.z);
        return 1f;
    }

    private int DamageTargets(Collider[] hits, float damageAmount)
    {
        if (hits == null || hits.Length == 0) return 0;

        Transform playerT = PlayerTransform;
        HashSet<Health> damaged = new HashSet<Health>();

        foreach (Collider c in hits)
        {
            if (c == null) continue;
            if (((1 << c.gameObject.layer) & excludeLayers) != 0) continue;
            if (c.transform.IsChildOf(playerT) || c.transform == playerT) continue;

            var health = c.GetComponentInParent<Health>();
            if (health == null || health.IsDead || damaged.Contains(health)) continue;

            health.TakeDamage(damageAmount);
            damaged.Add(health);
            if (enableLogging) Debug.Log($"[LightAttack] Hit {c.gameObject.name} for {damageAmount} damage.");
            if (fmodOnHit != null && !fmodOnHit.IsNull && AudioService.Instance != null)
                AudioService.Instance.PlayOneShot(fmodOnHit, c.ClosestPoint(GetHitboxOrigin()));
        }

        return damaged.Count;
    }

    private Vector3 GetHitboxOrigin()
    {
        Transform t = PlayerTransform;
        Vector3 forward = GetAttackForward();
        return t.position + forward * hitboxOffset;
    }

    private Vector3 GetAttackForward()
    {
        Transform source = attackDirectionSource != null ? attackDirectionSource : PlayerTransform;
        Vector3 f = source.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.01f ? f.normalized : PlayerTransform.forward;
    }

    private void ApplyForwardStep()
    {
        var rb = PlayerRigidbody;
        if (rb == null || forwardStepPerSwing <= 0f) return;
        Vector3 step = GetAttackForward() * forwardStepPerSwing;
        rb.MovePosition(rb.position + step);
    }

    private void PlaySwingSound(int comboIndex)
    {
        if (AudioService.Instance == null) return;
        FmodEventAsset evt = comboIndex == 0 ? fmodSwing1 : (comboIndex == 1 ? fmodSwing2 : fmodSwing3);
        if (evt != null && !evt.IsNull)
            AudioService.Instance.PlayOneShot(evt, PlayerTransform.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawHitboxGizmo) return;

        Transform t = Application.isPlaying ? PlayerTransform : transform;
        if (t == null) return;

        Vector3 forward = GetAttackForwardForGizmo(t);
        Vector3 origin = t.position + forward * hitboxOffset;
        float scale = Mathf.Max(0.01f, size);

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);

        if (hitboxShape == HitboxShape.Sphere)
        {
            float radius = (hitboxSize.x + hitboxSize.y + hitboxSize.z) / 3f * scale;
            Gizmos.DrawWireSphere(origin, radius);
        }
        else
        {
            Vector3 halfExtents = Vector3.Scale(hitboxSize, new Vector3(scale, scale, scale)) * 0.5f;
            Gizmos.matrix = Matrix4x4.TRS(origin, Quaternion.LookRotation(forward), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    private Vector3 GetAttackForwardForGizmo(Transform fallbackTransform)
    {
        if (Application.isPlaying) return GetAttackForward();
        Transform source = attackDirectionSource != null ? attackDirectionSource : fallbackTransform;
        Vector3 f = source.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
    }

    private enum HitboxShape
    {
        Sphere,
        Box
    }
}
