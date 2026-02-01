using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Telegraphed melee attack: draw a decal where the hit will be (telegraph), then use a sphere/box overlap
/// during the attack window to damage the player. No contact damage. Requires line of sight.
/// </summary>
public class MeleeAttack : MonoBehaviour
{
    public enum HitZoneShape
    {
        Sphere,
        Box
    }

    [Header("Damage & timing")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float telegraphDuration = 0.4f;
    [SerializeField] private float attackActiveDuration = 0.25f;

    [Header("Hit zone (must match decal)")]
    [Tooltip("Shape used for overlap check during attack window.")]
    [SerializeField] private HitZoneShape hitZoneShape = HitZoneShape.Sphere;
    [Tooltip("Center of hit zone = transform.position + up * heightOffset + forward * this (use ~attackRange * 0.5 so zone is in front).")]
    [SerializeField] private float hitZoneOffset = 0.9f;
    [Tooltip("Height above feet for hit zone center (stops cast sitting in ground).")]
    [SerializeField] private float hitZoneHeightOffset = 0.3f;
    [Tooltip("Radius when using Sphere. Decal should match this size.")]
    [SerializeField] private float hitZoneRadius = 0.8f;
    [Tooltip("Half-extents when using Box (X=width/2, Y=height/2, Z=depth/2).")]
    [SerializeField] private Vector3 hitZoneHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Debug")]
    [Tooltip("Draw the hit zone (sphere/box) in the Scene view when this object is selected.")]
    [SerializeField] private bool drawGizmos;

    [Header("Overlap filter")]
    [Tooltip("Layers to check for damage (e.g. Player). Leave Everything to filter by tag/code.")]
    [SerializeField] private LayerMask hitLayers = -1;
    [Tooltip("If set, only objects with this tag are damaged (e.g. Player).")]
    [SerializeField] private string damageTag = "Player";

    [Header("Audio")]
    [Tooltip("Optional FMOD event played when the attack starts (same moment as attack animation).")]
    [SerializeField] private FmodEventAsset fmodAttack;

    [Header("Telegraph events")]
    [SerializeField] private UnityEvent onTelegraphStarted;
    [SerializeField] private UnityEvent onTelegraphEnded;
    [SerializeField] private UnityEvent onAttackWindowOpened;
    [SerializeField] private UnityEvent onAttackWindowEnded;
    [Tooltip("Fired when the overlap hits the player. Use for hit VFX/sound.")]
    [SerializeField] private UnityEvent onHit;

    /// <summary>Current melee attack range (for movement/decals).</summary>
    public float AttackRange => attackRange;

    /// <summary>Hit zone center in world (for decal placement).</summary>
    public Vector3 HitZoneCenterWorld => GetHitZoneCenterWorld();

    /// <summary>Hit zone radius (sphere) or approximate size (box). For decal sizing.</summary>
    public float HitZoneRadius => hitZoneShape == HitZoneShape.Sphere ? hitZoneRadius : Mathf.Max(hitZoneHalfExtents.x, hitZoneHalfExtents.z);

    /// <summary>Decal size (x = width, y = depth) to match cast exactly: sphere diameter or box extents.</summary>
    public Vector2 HitZoneDecalSize =>
        hitZoneShape == HitZoneShape.Sphere
            ? new Vector2(hitZoneRadius * 2f, hitZoneRadius * 2f)
            : new Vector2(hitZoneHalfExtents.x * 2f, hitZoneHalfExtents.z * 2f);

    /// <summary>Hit zone forward direction (for decal rotation).</summary>
    public Vector3 HitZoneForward
    {
        get
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
        }
    }

    public void SetDamageAndCooldown(float dmg, float cd)
    {
        damage = dmg;
        cooldown = cd;
    }

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
    private EnemyAnimatorDriver animatorDriver;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        sight = GetComponent<EnemySight>();
        attackState = GetComponent<EnemyAttackState>();
        animatorDriver = GetComponent<EnemyAnimatorDriver>();
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
                    onTelegraphEnded?.Invoke();
                    state = State.AttackActive;
                    stateEndTime = Time.time + attackActiveDuration;
                    onAttackWindowOpened?.Invoke();
                    animatorDriver?.SetAttackTrigger();
                    if (fmodAttack != null && AudioService.Instance != null)
                        AudioService.Instance.PlayOneShot(fmodAttack, transform.position);
                }
                break;

            case State.AttackActive:
                if (!dealtDamageThisSwing)
                    TryOverlapHit();
                if (Time.time >= stateEndTime)
                {
                    onAttackWindowEnded?.Invoke();
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

    private void TryOverlapHit()
    {
        Vector3 center = GetHitZoneCenterWorld();
        Quaternion rotation = Quaternion.LookRotation(HitZoneForward);

        if (hitZoneShape == HitZoneShape.Sphere)
        {
            Collider[] hits = Physics.OverlapSphere(center, hitZoneRadius, hitLayers);
            foreach (Collider c in hits)
            {
                if (ApplyHit(c.gameObject))
                    return;
            }
        }
        else
        {
            Collider[] hits = Physics.OverlapBox(center, hitZoneHalfExtents, rotation, hitLayers);
            foreach (Collider c in hits)
            {
                if (ApplyHit(c.gameObject))
                    return;
            }
        }
    }

    private bool ApplyHit(GameObject other)
    {
        if (!string.IsNullOrEmpty(damageTag) && !other.CompareTag(damageTag))
            return false;

        var health = other.GetComponent<Health>();
        if (health == null || health.IsDead)
            return false;

        health.TakeDamage(damage);
        dealtDamageThisSwing = true;
        onHit?.Invoke();
        onAttackWindowEnded?.Invoke();
        state = State.Cooldown;
        stateEndTime = Time.time + cooldown;
        return true;
    }

    private Vector3 GetHitZoneCenterWorld()
    {
        Vector3 forward = HitZoneForward;
        return transform.position + Vector3.up * hitZoneHeightOffset + forward * hitZoneOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 center = GetHitZoneCenterWorld();
        Quaternion rotation = Quaternion.LookRotation(HitZoneForward);

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        if (hitZoneShape == HitZoneShape.Sphere)
        {
            Gizmos.DrawWireSphere(center, hitZoneRadius);
        }
        else
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, hitZoneHalfExtents * 2f);
            Gizmos.matrix = prev;
        }
    }
}
