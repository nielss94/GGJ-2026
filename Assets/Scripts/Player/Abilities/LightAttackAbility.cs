using UnityEngine;

/// <summary>
/// Light attack ability (boilerplate). Preferred slot X; use PlayerTransform for position/direction.
/// Implement attack logic in TryPerform() when ready.
/// </summary>
public class LightAttackAbility : PlayerAbility
{
    [Header("Light attack (placeholder)")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackDuration = 0.3f;

    private void Reset()
    {
        preferredSlot = PlayerAbilityManager.AbilitySlot.X;
        abilityName = "Light Attack";
    }

    public override bool TryPerform()
    {
        if (!CanPerform) return false;

        Debug.Log("Light attack performed");
        // TODO: Implement light attack (e.g. play animation, deal damage in front, cooldown).
        return true;
    }
}
