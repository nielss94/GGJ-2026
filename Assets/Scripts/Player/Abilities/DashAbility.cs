using System.Collections;
using UnityEngine;

/// <summary>
/// Dash ability. Preferred slot A; use PlayerRigidbody or PlayerTransform to move the player.
/// Upgrade value comes from the upgrade database (curve * rarity); assign the same AbilityStatId as in the upgrade definition.
/// </summary>
public class DashAbility : PlayerAbility
{
    [Header("Dash stats")]
    [Tooltip("Assign the same Ability Stat Id asset as used by 'Dash Distance' ability upgrades in the database.")]
    [SerializeField] private AbilityStatId dashDistanceStatId;
    [Tooltip("Assign the same Ability Stat Id asset as used by 'Dash Speed' ability upgrades in the database.")]
    [SerializeField] private AbilityStatId dashSpeedStatId;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [Tooltip("Speed in m/s. Initial value should match dashDistance/dashDuration so dash covers the full distance. Upgrades add to this.")]
    [SerializeField] private float dashSpeed = 25f;
    [Tooltip("Used only to derive initial dashSpeed if desired (dashDistance/dashDuration). Movement ends when dashDistance is covered or wall hit.")]
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("When true, dash goes in the movement input direction (WASD / left stick) instead of look direction. Useful when attacks lock rotation but you can cancel with dash.")]
    [SerializeField] private bool useMovementDirectionForDash;
    [Tooltip("Optional. If set and not using movement direction, dash direction is this transform's forward (e.g. character model). Otherwise uses player root forward.")]
    [SerializeField] private Transform dashDirectionSource;
    [Tooltip("Stop dash when hitting a wall. If false, dash continues but movement is clamped each frame.")]
    [SerializeField] private bool stopDashOnWallHit = true;
    [Tooltip("Distance buffer from hit surface to avoid overlap (meters).")]
    [SerializeField] private float wallHitBuffer = 0.02f;
    [Tooltip("Only end dash when wall hit is at least this far (m). If already against wall (hit closer), we don't end early—dash fizzles for full duration. Set to 0 to always end on any wall hit.")]
    [SerializeField] private float minWallHitDistanceToEndDash = 0.05f;

    [Header("Audio")]
    [Tooltip("Played when dash starts.")]
    [SerializeField] private FmodEventAsset fmodDash;

    /// <summary>Current dash distance (base + values applied from upgrades).</summary>
    public float DashDistance => dashDistance;

    /// <summary>Current dash speed in m/s (base + values applied from upgrades).</summary>
    public float DashSpeed => dashSpeed;

    /// <summary>True while the player is currently dashing. Use for animator (e.g. IsDashing parameter).</summary>
    public bool IsDashing => isDashing;

    private bool isDashing;

    private void Reset()
    {
        preferredSlot = PlayerAbilityManager.AbilitySlot.A;
        abilityName = "Dash";
    }

    public override void ApplyLevel()
    {
        // Upgrade values come from the upgrade database (curve * rarity), applied via ApplyUpgradeValue.
    }

    public override void ApplyUpgradeValue(AbilityStatId statId, float value)
    {
        TryApplyUpgrade(dashDistanceStatId, statId, value, v => dashDistance += v);
        TryApplyUpgrade(dashSpeedStatId, statId, value, v => dashSpeed += v);
    }

    public override bool CanPerform => !isDashing && base.CanPerform;

    public override bool TryPerform()
    {
        if (!CanPerform) return false;

        var rb = PlayerRigidbody;
        if (rb == null) return false;

        Vector3 direction;
        if (useMovementDirectionForDash)
        {
            var playerMovement = PlayerTransform.GetComponent<PlayerMovement>();
            direction = playerMovement != null ? playerMovement.WorldMoveDirection : Vector3.zero;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = (dashDirectionSource != null ? dashDirectionSource.forward : PlayerTransform.forward);
            if (direction.sqrMagnitude >= 0.01f)
                direction.Normalize();
            else
                direction = Vector3.forward;
        }
        else
        {
            direction = (dashDirectionSource != null ? dashDirectionSource.forward : PlayerTransform.forward);
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector3.forward;
            else
                direction.Normalize();
        }

        StartCoroutine(PerformDashCoroutine(rb, direction));
        return true;
    }

    private void OnDisable()
    {
        if (isDashing)
        {
            EventBus.RaisePlayerInputUnblockRequested(this);
            isDashing = false;
        }
    }

    private IEnumerator PerformDashCoroutine(Rigidbody rb, Vector3 direction)
    {
        isDashing = true;
        EventBus.RaisePlayerDashStarted(this);
        EventBus.RaisePlayerInputBlockRequested(this);

        try
        {
            var playerMovement = PlayerTransform.GetComponent<PlayerMovement>();
            if (playerMovement != null && playerMovement.ModelTransform != null)
                playerMovement.ModelTransform.rotation = Quaternion.LookRotation(direction);

            if (AudioService.Instance != null && fmodDash != null && !fmodDash.IsNull)
                AudioService.Instance.PlayOneShot(fmodDash, PlayerTransform.position);
            // TODO: i-frames during dash — e.g. add EventBus.InvincibilityRequested(object source, bool invincible) and raise it here / at end; have health/damage script subscribe and ignore damage while any source has requested invincibility (same pattern as PlayerInputBlocker).

            float remainingDistance = dashDistance;
            float elapsed = 0f;
            const float noProgressThreshold = 0.0001f;
            int noProgressFrames = 0;

            while (remainingDistance > 0.001f)
            {
                yield return new WaitForFixedUpdate();
                float step = Time.fixedDeltaTime;
                elapsed += step;

                // Safety: end dash after max duration so we never block input forever (e.g. stuck in geometry).
                if (elapsed >= dashDuration * 2f)
                    break;

                float desiredDistance = Mathf.Min(dashSpeed * step, remainingDistance);
                float actualDistance = desiredDistance;

                if (desiredDistance > 0.001f && rb != null && rb.SweepTest(direction, out RaycastHit hit, desiredDistance))
                {
                    if (!hit.collider.isTrigger)
                    {
                        actualDistance = Mathf.Max(0f, hit.distance - wallHitBuffer);
                        bool farEnoughToCountAsWallHit = hit.distance >= minWallHitDistanceToEndDash;
                        if (stopDashOnWallHit && farEnoughToCountAsWallHit)
                        {
                            rb.MovePosition(rb.position + direction * actualDistance);
                            break;
                        }
                    }
                }

                rb.MovePosition(rb.position + direction * actualDistance);
                remainingDistance -= actualDistance;

                // Safety: if we're not making progress (stuck in/between tiles), end dash so input unblocks.
                if (actualDistance < noProgressThreshold && desiredDistance > noProgressThreshold)
                {
                    noProgressFrames++;
                    if (noProgressFrames >= 3)
                        break;
                }
                else
                {
                    noProgressFrames = 0;
                }
            }
        }
        finally
        {
            EventBus.RaisePlayerInputUnblockRequested(this);
            isDashing = false;
        }
    }
}
