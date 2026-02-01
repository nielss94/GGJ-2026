using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Light attack: 3-hit sword combo (right, left, right). Preferred slot X.
/// No walking during attacks; each swing moves the player forward. Dodge (dash) cancels attack and applies cooldown.
/// Hitbox is instant per swing; Size/AttackSpeed/Cooldown/Damage come from stats and upgrades.
/// </summary>
public class LightAttackAbility : PlayerAbility, IInputBufferable
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
    [Tooltip("Per-swing durations in seconds (X=Attack1, Y=Attack2, Z=Attack3). Set to your clip lengths to align with animation. Actual = value / attackSpeed. Leave at 0 to use Base Swing Duration for all.")]
    [SerializeField] private Vector3 swingDurations = new Vector3(0.37f, 0.37f, 0.43f);
    [Tooltip("Fallback duration when Swing Durations are 0 for that swing. Actual = base / attackSpeed.")]
    [SerializeField] private float baseSwingDuration = 0.3f;
    [Tooltip("Base minimum time before next attack can be input. Only used for time-based transition; ignored when using animation events. Actual = base / attackSpeed.")]
    [SerializeField] private float baseMinTimeBetweenHits = 0.15f;
    [Tooltip("Max time after a swing to press attack again to continue combo (seconds). After this, combo resets (no cooldown).")]
    [SerializeField] private float comboLinkWindow = 0.9f;

    [Header("Stats (optional)")]
    [Tooltip("Leave empty to resolve at runtime via FindFirstObjectByType. Used for crit chance and knockback chance.")]
    [SerializeField] private PlayerStats playerStats;

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

    [Header("Attack sprites (optional, 2D)")]
    [Tooltip("One sprite GameObject per swing (Attack1, Attack2, Attack3). Shown in front of the player during that swing and hidden when the swing ends.")]
    [SerializeField] private GameObject[] attackSpritePerSwing = new GameObject[3];
    [Tooltip("Base scale per plane (set in engine, e.g. -1 on X to flip). Final scale = this * Size (upgrades). Leave at (1,1,1) for no flip.")]
    [SerializeField] private Vector3[] attackPlaneBaseScale = new Vector3[] { Vector3.one, Vector3.one, Vector3.one };
    [Tooltip("Distance in front of the player to place the sprite when shown (meters).")]
    [SerializeField] private float spriteShowOffset = 0.6f;

    [Header("Debug")]
    [Tooltip("Draw hitbox in Scene view when this object is selected.")]
    [SerializeField] private bool drawHitboxGizmo = true;
    [Tooltip("Log combo state, hits and cooldown to console.")]
    [SerializeField] private bool enableLogging;

    [Header("FMOD (optional)")]
    [Tooltip("Played at start of each swing. Use Combo Parameter to differentiate (0, 1, 2).")]
    [SerializeField] private FmodEventAsset fmodSwing;
    [Tooltip("FMOD parameter name for combo swing (value 0, 1, or 2). Set in inspector to match your event.")]
    [SerializeField] private string fmodSwingComboParameterName = "Combo";
    [Tooltip("Optional: played when an enemy is damaged (hook for later).")]
    [SerializeField] private FmodEventAsset fmodOnHit;

    public float Size => size;
    public float AttackSpeed => attackSpeed;
    public float Cooldown => cooldown;
    public float Damage => damage;

    /// <summary>Fired when a swing starts. Argument is combo index 0, 1, or 2 (for Attack1, Attack2, Attack3).</summary>
    public event System.Action<int> OnSwingStarted;

    /// <summary>
    /// Call from an Animation Event at the frame where the next attack can start (same GameObject as Animator).
    /// Parameter: 0 = Attack1 clip ended, 1 = Attack2 ended, 2 = Attack3 ended.
    /// Opens the combo link window exactly when the event fires for perfect animation sync.
    /// </summary>
    public void NotifySwingEndFromAnimation(int comboIndex)
    {
        if (comboIndex < 0 || comboIndex > 2) return;
        bool stateMatches = (state == State.Swing0 && comboIndex == 0) || (state == State.Swing1 && comboIndex == 1) || (state == State.Swing2 && comboIndex == 2);
        if (!stateMatches) return;

        float now = Time.time;
        if (state == State.Swing2)
        {
            if (enableLogging) Debug.Log("[LightAttack] Combo complete (3/3, from animation). Applying cooldown.");
            EndComboAndApplyCooldown();
            return;
        }

        if (state == State.Swing0)
        {
            HideAttackSprite();
            state = State.BetweenSwings0;
            nextSwingAllowedAt = now;
            comboWindowEndTime = now + comboLinkWindow;
            EventBus.RaisePlayerMovementUnblockRequested(this);
        }
        else if (state == State.Swing1)
        {
            HideAttackSprite();
            state = State.BetweenSwings1;
            nextSwingAllowedAt = now;
            comboWindowEndTime = now + comboLinkWindow;
            EventBus.RaisePlayerMovementUnblockRequested(this);
        }
    }

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
            string buf = bufferedSwingIndex != -1 ? $" buffered={bufferedSwingIndex + 1}" : "";
            return $"[LightAttack] {state} — next in {nextIn:F2}s, window {windowClosesIn:F2}s{buf}";
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
    /// <summary>Buffered next swing index (1 or 2). -1 = none. Only one buffer at a time; cannot buffer attack1 during attack3.</summary>
    private int bufferedSwingIndex = -1;

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
        bufferedSwingIndex = -1;
        HideAttackSprite();
        if (state != State.Idle)
        {
            EventBus.RaisePlayerMovementUnblockRequested(this);
            state = State.Idle;
        }
    }

    public bool TryBufferInput()
    {
        if (bufferedSwingIndex != -1) return true;
        if (state == State.Swing0 || state == State.BetweenSwings0)
        {
            bufferedSwingIndex = 1;
            if (enableLogging) Debug.Log("[LightAttack] Buffered attack2.");
            return true;
        }
        if (state == State.Swing1 || state == State.BetweenSwings1)
        {
            bufferedSwingIndex = 2;
            if (enableLogging) Debug.Log("[LightAttack] Buffered attack3.");
            return true;
        }
        return false;
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
                if (bufferedSwingIndex != -1 && now >= nextSwingAllowedAt)
                {
                    int next = state == State.BetweenSwings0 ? 1 : 2;
                    if (bufferedSwingIndex == next)
                    {
                        int toPerform = bufferedSwingIndex;
                        bufferedSwingIndex = -1;
                        StartSwing(toPerform);
                    }
                }
                if (now > comboWindowEndTime)
                {
                    if (enableLogging) Debug.Log("[LightAttack] Combo link window expired. Resetting (no cooldown).");
                    bufferedSwingIndex = -1;
                    EndComboWithoutCooldown();
                }
                break;
        }
    }

    private void StartSwing(int comboIndex)
    {
        EventBus.RaisePlayerMovementBlockRequested(this);

        state = comboIndex == 0 ? State.Swing0 : (comboIndex == 1 ? State.Swing1 : State.Swing2);

        float speedFactor = Mathf.Max(0.01f, attackSpeed);
        float baseDuration = GetBaseSwingDuration(comboIndex);
        float swingDuration = baseDuration / speedFactor;
        swingEndTime = Time.time + swingDuration;

        ApplyHitbox(comboIndex + 1);
        ApplyForwardStep();
        PlaySwingSound(comboIndex);
        ShowAttackSprite(comboIndex);
        OnSwingStarted?.Invoke(comboIndex);
        if (enableLogging) Debug.Log($"[LightAttack] Swing {comboIndex + 1}/3 started (duration={swingDuration:F2}s).");
    }

    private void ShowAttackSprite(int comboIndex)
    {
        if (attackSpritePerSwing == null || comboIndex < 0 || comboIndex >= attackSpritePerSwing.Length) return;

        GameObject planeObj = attackSpritePerSwing[comboIndex];
        if (planeObj == null) return;

        for (int i = 0; i < attackSpritePerSwing.Length; i++)
        {
            if (attackSpritePerSwing[i] != null)
                attackSpritePerSwing[i].SetActive(false);
        }

        Vector3 forward = GetAttackForward();
        float sizeFactor = Mathf.Max(0.01f, size);
        Vector3 baseScale = (attackPlaneBaseScale != null && comboIndex < attackPlaneBaseScale.Length)
            ? attackPlaneBaseScale[comboIndex]
            : Vector3.one;
        planeObj.transform.localScale = Vector3.Scale(baseScale, new Vector3(sizeFactor, sizeFactor, sizeFactor));
        planeObj.SetActive(true);
    }

    private void HideAttackSprite()
    {
        if (attackSpritePerSwing == null) return;
        for (int i = 0; i < attackSpritePerSwing.Length; i++)
        {
            if (attackSpritePerSwing[i] != null)
                attackSpritePerSwing[i].SetActive(false);
        }
    }

    private float GetBaseSwingDuration(int comboIndex)
    {
        float d = comboIndex == 0 ? swingDurations.x : (comboIndex == 1 ? swingDurations.y : swingDurations.z);
        return d > 0.001f ? d : baseSwingDuration;
    }

    private void OnSwingEnd()
    {
        HideAttackSprite();

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
            EventBus.RaisePlayerMovementUnblockRequested(this);
        }
        else if (state == State.Swing1)
        {
            state = State.BetweenSwings1;
            nextSwingAllowedAt = now + minBetween;
            comboWindowEndTime = now + comboLinkWindow;
            EventBus.RaisePlayerMovementUnblockRequested(this);
        }
    }

    private void EndComboAndApplyCooldown()
    {
        bufferedSwingIndex = -1;
        state = State.Idle;
        cooldownUntil = Time.time + Mathf.Max(0f, cooldown);
        HideAttackSprite();
        EventBus.RaisePlayerMovementUnblockRequested(this);
    }

    private void EndComboWithoutCooldown()
    {
        bufferedSwingIndex = -1;
        state = State.Idle;
        HideAttackSprite();
        EventBus.RaisePlayerMovementUnblockRequested(this);
    }

    private void ApplyHitbox(int swingNumber)
    {
        Vector3 origin = GetHitboxOrigin();
        Vector3 forward = GetAttackForward();
        float scale = Mathf.Max(0.01f, size);
        float mult = GetDamageMultiplierForSwing(swingNumber);
        float damageThisSwing = damage * mult;

        Vector3 effectiveHitboxSize = hitboxSize;
        if (swingNumber == 3 && attackSpritePerSwing != null && attackSpritePerSwing.Length > 2 && attackSpritePerSwing[2] != null)
        {
            Vector3 lastAttackSpriteSize = GetLastAttackSpriteWorldSize();
            if (lastAttackSpriteSize.sqrMagnitude > 0.0001f)
                effectiveHitboxSize = lastAttackSpriteSize;
        }

        if (hitboxShape == HitboxShape.Sphere)
        {
            float radius = (effectiveHitboxSize.x + effectiveHitboxSize.y + effectiveHitboxSize.z) / 3f * scale;
            Collider[] overlaps = Physics.OverlapSphere(origin, radius, damageableLayers);
            Collider[] hits = FilterToFrontHalfSphere(overlaps, origin, forward);
            int damagedCount = DamageTargets(hits, damageThisSwing);
            if (enableLogging) Debug.Log($"[LightAttack] Swing {swingNumber} hitbox (sphere r={radius:F2}): {overlaps?.Length ?? 0} overlaps, {hits?.Length ?? 0} in front half, {damagedCount} damaged.");
        }
        else
        {
            Vector3 halfExtents = Vector3.Scale(effectiveHitboxSize, new Vector3(scale, scale, scale)) * 0.5f;
            Quaternion orientation = Quaternion.LookRotation(forward);
            Collider[] hits = Physics.OverlapBox(origin, halfExtents, orientation, damageableLayers);
            int damagedCount = DamageTargets(hits, damageThisSwing);
            if (enableLogging) Debug.Log($"[LightAttack] Swing {swingNumber} hitbox (box): {hits?.Length ?? 0} overlaps, {damagedCount} damaged.");
        }
    }

    /// <summary>Returns the third attack sprite's size at scale 1 (used to match last-attack hitbox to the plane). Size upgrade is applied in ApplyHitbox.</summary>
    private Vector3 GetLastAttackSpriteWorldSize()
    {
        if (attackSpritePerSwing == null || attackSpritePerSwing.Length <= 2 || attackSpritePerSwing[2] == null)
            return Vector3.zero;

        var sr = attackSpritePerSwing[2].GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector3 localSize = sr.sprite.bounds.size;
            return new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        return Vector3.zero;
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
        PlayerStats stats = GetPlayerStats();

        foreach (Collider c in hits)
        {
            if (c == null) continue;
            if (((1 << c.gameObject.layer) & excludeLayers) != 0) continue;
            if (c.transform.IsChildOf(playerT) || c.transform == playerT) continue;

            var health = c.GetComponentInParent<Health>();
            if (health == null || health.IsDead || damaged.Contains(health)) continue;

            float finalDamage = damageAmount;
            bool isCrit = false;
            if (stats != null && stats.RollCrit())
            {
                isCrit = true;
                finalDamage *= stats.CritDamageMultiplier;
                Debug.Log($"[LightAttack] Crit! {c.gameObject.name} for {finalDamage} damage (base {damageAmount}).");
            }

            float knockbackForce = stats != null ? stats.KnockbackForce : 1f;
            bool willDie = health.CurrentHealth <= finalDamage;
            health.TakeDamage(finalDamage, PlayerTransform.position, knockbackForce);
            damaged.Add(health);
            if (enableLogging && !isCrit) Debug.Log($"[LightAttack] Hit {c.gameObject.name} for {finalDamage} damage.");
            if (fmodOnHit != null && !fmodOnHit.IsNull && AudioService.Instance != null)
            {
                var applier = c.GetComponentInParent<EnemyTypeApplier>();
                var enemyType = applier != null ? applier.Type : null;
                string enemyHittype = willDie ? "enemy_dies" : (enemyType != null && enemyType.Armored ? "armored" : "regular");
                var labelParams = new Dictionary<string, string> { { "enemy_hittype", enemyHittype } };
                var intParams = new Dictionary<string, int> { { "critical", isCrit ? 1 : 0 } };
                AudioService.Instance.PlayOneShotAtPositionWithParameters(fmodOnHit, c.ClosestPoint(GetHitboxOrigin()), labelParams, intParams);
            }
        }

        return damaged.Count;
    }

    private PlayerStats GetPlayerStats()
    {
        if (playerStats != null) return playerStats;
        return FindFirstObjectByType<PlayerStats>();
    }

    /// <summary>For sphere hitbox: only colliders in the front half of the sphere (in front of the player) are considered.</summary>
    private static Collider[] FilterToFrontHalfSphere(Collider[] overlaps, Vector3 origin, Vector3 forward)
    {
        if (overlaps == null || overlaps.Length == 0) return overlaps;
        Vector3 f = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
        var list = new List<Collider>(overlaps.Length);
        foreach (Collider c in overlaps)
        {
            if (c == null) continue;
            Vector3 closest = c.ClosestPoint(origin);
            if (Vector3.Dot(closest - origin, f) >= 0f)
                list.Add(c);
        }
        return list.ToArray();
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
        if (AudioService.Instance == null || fmodSwing == null || fmodSwing.IsNull) return;
        int comboValue = Mathf.Clamp(comboIndex, 0, 2);
        var parameters = new Dictionary<string, int> { { fmodSwingComboParameterName, comboValue } };
        AudioService.Instance.PlayOneShotWithParametersInt(fmodSwing, parameters);
    }

    private void OnDrawGizmos()
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
