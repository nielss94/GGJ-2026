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

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("Optional. If set, dash direction is this transform's forward (e.g. character model). Otherwise uses player root forward.")]
    [SerializeField] private Transform dashDirectionSource;
    [Tooltip("Stop dash when hitting a wall. If false, dash continues but movement is clamped each frame.")]
    [SerializeField] private bool stopDashOnWallHit = true;
    [Tooltip("Distance buffer from hit surface to avoid overlap (meters).")]
    [SerializeField] private float wallHitBuffer = 0.02f;
    [Tooltip("Only end dash when wall hit is at least this far (m). If already against wall (hit closer), we don't end early—dash fizzles for full duration. Set to 0 to always end on any wall hit.")]
    [SerializeField] private float minWallHitDistanceToEndDash = 0.05f;

    /// <summary>Current dash distance (base + values applied from upgrades).</summary>
    public float DashDistance => dashDistance;

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
    }

    public override bool CanPerform => !isDashing && base.CanPerform;

    public override bool TryPerform()
    {
        if (!CanPerform) return false;

        var rb = PlayerRigidbody;
        if (rb == null) return false;

        Vector3 forward = (dashDirectionSource != null ? dashDirectionSource.forward : PlayerTransform.forward);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        StartCoroutine(PerformDashCoroutine(rb, forward));
        return true;
    }

    private IEnumerator PerformDashCoroutine(Rigidbody rb, Vector3 direction)
    {
        isDashing = true;
        EventBus.RaisePlayerDashStarted(this);
        EventBus.RaisePlayerInputBlockRequested(this);
        // TODO: i-frames during dash — e.g. add EventBus.InvincibilityRequested(object source, bool invincible) and raise it here / at end; have health/damage script subscribe and ignore damage while any source has requested invincibility (same pattern as PlayerInputBlocker).

        float speed = dashDistance / dashDuration;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            yield return new WaitForFixedUpdate();
            float step = Mathf.Min(Time.fixedDeltaTime, dashDuration - elapsed);
            float desiredDistance = speed * step;
            float actualDistance = desiredDistance;

            if (desiredDistance > 0.001f && rb.SweepTest(direction, out RaycastHit hit, desiredDistance))
            {
                if (!hit.collider.isTrigger)
                {
                    actualDistance = Mathf.Max(0f, hit.distance - wallHitBuffer);
                    bool farEnoughToCountAsWallHit = hit.distance >= minWallHitDistanceToEndDash;
                    if (stopDashOnWallHit && farEnoughToCountAsWallHit)
                    {
                        rb.MovePosition(rb.position + direction * actualDistance);
                        elapsed += dashDuration;
                        break;
                    }
                }
            }

            rb.MovePosition(rb.position + direction * actualDistance);
            elapsed += step;
        }

        EventBus.RaisePlayerInputUnblockRequested(this);
        isDashing = false;
    }
}
