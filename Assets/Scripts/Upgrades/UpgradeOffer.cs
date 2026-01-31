using UnityEngine;

/// <summary>
/// Runtime wrapper for an upgrade that was drawn from the database. Holds definition + rarity
/// so the UI can show name/description/rarity and the applier can evaluate value (curve * rarity multiplier).
/// </summary>
public class UpgradeOffer
{
    /// <summary>Rarity drawn for this offer (affects display and value multiplier).</summary>
    public UpgradeRarity Rarity { get; }

    /// <summary>True if this is an ability upgrade; false if stat upgrade.</summary>
    public bool IsAbility => AbilityDefinition != null;

    /// <summary>Set when this offer is for an ability upgrade.</summary>
    public AbilityUpgradeDefinition AbilityDefinition { get; }

    /// <summary>Set when this offer is for a stat upgrade.</summary>
    public StatUpgradeDefinition StatDefinition { get; }

    /// <summary>Display name for the card.</summary>
    public string DisplayName => IsAbility ? AbilityDefinition.DisplayName : StatDefinition.DisplayName;

    /// <summary>Description for the card.</summary>
    public string Description => IsAbility ? AbilityDefinition.Description : StatDefinition.Description;

    /// <summary>Rarity display name.</summary>
    public string RarityName => Rarity != null ? Rarity.DisplayName : "";

    /// <summary>Ability slot (only for ability upgrades).</summary>
    public PlayerAbilityManager.AbilitySlot? AbilitySlot => IsAbility ? AbilityDefinition.AbilitySlot : null;

    /// <summary>Ability stat id (only for ability upgrades). Pass to ability.ApplyUpgradeValue.</summary>
    public AbilityStatId AbilityStatIdRef => IsAbility ? AbilityDefinition.StatId : null;

    /// <summary>Stat upgrade id (only for stat upgrades). Use in ApplyStatUpgrade.</summary>
    public StatUpgradeId StatUpgradeIdRef => !IsAbility ? StatDefinition.StatId : null;

    /// <summary>Legacy string id: AbilityStatIdRef.Id or StatUpgradeIdRef.Id, or empty if null.</summary>
    public string StatId => IsAbility ? (AbilityDefinition.StatId != null ? AbilityDefinition.StatId.Id : "") : (StatDefinition.StatId != null ? StatDefinition.StatId.Id : "");

    /// <summary>Creates an ability upgrade offer.</summary>
    public UpgradeOffer(AbilityUpgradeDefinition definition, UpgradeRarity rarity)
    {
        AbilityDefinition = definition;
        StatDefinition = null;
        Rarity = rarity;
    }

    /// <summary>Creates a stat upgrade offer.</summary>
    public UpgradeOffer(StatUpgradeDefinition definition, UpgradeRarity rarity)
    {
        AbilityDefinition = null;
        StatDefinition = definition;
        Rarity = rarity;
    }

    /// <summary>
    /// Evaluates the upgrade value at the given level. Curve value * rarity multiplier.
    /// Use the ability's current level for ability upgrades; use a stack/level count for stat upgrades.
    /// </summary>
    public float EvaluateValue(int level)
    {
        float baseValue = IsAbility
            ? AbilityDefinition.EvaluateAtLevel(level)
            : StatDefinition.EvaluateAtLevel(level);
        float multiplier = Rarity != null ? Rarity.ValueMultiplier : 1f;
        return baseValue * multiplier;
    }
}
