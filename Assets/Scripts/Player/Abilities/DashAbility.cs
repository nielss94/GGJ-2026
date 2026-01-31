using UnityEngine;

/// <summary>
/// Dash ability (boilerplate). Preferred slot A; use PlayerRigidbody or PlayerTransform to move the player.
/// Implement dash logic in TryPerform() when ready.
/// </summary>
public class DashAbility : PlayerAbility
{
    [Header("Dash (placeholder)")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;

    private void Reset()
    {
        preferredSlot = PlayerAbilityManager.AbilitySlot.A;
        abilityName = "Dash";
    }

    public override bool TryPerform()
    {
        if (!CanPerform) return false;

        Debug.Log("Dash performed");
        // TODO: Implement dash (e.g. move player forward, disable input briefly, add i-frames).
        return true;
    }
}
