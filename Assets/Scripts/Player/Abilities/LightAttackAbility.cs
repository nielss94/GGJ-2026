using UnityEngine;

/// <summary>
/// Light attack ability: 3-hit sword combo (right, left, right). Preferred slot X.
/// Parameters: size (swing), attack speed, cooldown between combos, damage per swing.
/// Attack logic to be implemented in TryPerform().
/// </summary>
public class LightAttackAbility : PlayerAbility
{
    [Header("Light attack stat IDs (assign same assets as in upgrade definitions)")]
    [SerializeField] private AbilityStatId sizeStatId;
    [SerializeField] private AbilityStatId attackSpeedStatId;
    [SerializeField] private AbilityStatId cooldownStatId;
    [SerializeField] private AbilityStatId damageStatId;

    [Header("Light attack parameters")]
    [Tooltip("Size of the sword swing (e.g. hitbox scale).")]
    [SerializeField] private float size = 1f;
    [Tooltip("Speed of the sword and time between combo hits (lower = faster).")]
    [SerializeField] private float attackSpeed = 1f;
    [Tooltip("Cooldown between combos (seconds).")]
    [SerializeField] private float cooldown = 1f;
    [Tooltip("Damage per swing.")]
    [SerializeField] private float damage = 10f;

    /// <summary>Current swing size (base + upgrades).</summary>
    public float Size => size;

    /// <summary>Current attack speed (base + upgrades).</summary>
    public float AttackSpeed => attackSpeed;

    /// <summary>Current cooldown between combos (base + upgrades).</summary>
    public float Cooldown => cooldown;

    /// <summary>Current damage per swing (base + upgrades).</summary>
    public float Damage => damage;

    private void Reset()
    {
        preferredSlot = PlayerAbilityManager.AbilitySlot.X;
        abilityName = "Light Attack";
    }

    public override void ApplyUpgradeValue(AbilityStatId statId, float value)
    {
        TryApplyUpgrade(sizeStatId, statId, value, v => size += v);
        TryApplyUpgrade(attackSpeedStatId, statId, value, v => attackSpeed += v);
        TryApplyUpgrade(cooldownStatId, statId, value, v => cooldown += v);
        TryApplyUpgrade(damageStatId, statId, value, v => damage += v);
    }

    public override bool TryPerform()
    {
        if (!CanPerform) return false;

        Debug.Log("Light attack performed");
        // TODO: Implement light attack (3-hit combo, animation, hitbox, cooldown, dodge cancel).
        return true;
    }
}
