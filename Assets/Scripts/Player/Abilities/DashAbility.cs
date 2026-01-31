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

    [Header("Dash (placeholder)")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;

    /// <summary>Current dash distance (base + values applied from upgrades).</summary>
    public float DashDistance => dashDistance;

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
        if (statId == dashDistanceStatId)
            dashDistance += value;
    }

    public override bool TryPerform()
    {
        if (!CanPerform) return false;

        Debug.Log("Dash performed");
        // TODO: Implement dash (e.g. move player forward, disable input briefly, add i-frames).
        return true;
    }
}
