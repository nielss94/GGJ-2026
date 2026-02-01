using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds player stats (e.g. Crit Chance, Knockback Force). Stat upgrades apply via ApplyStatUpgradeValue
/// using the same pattern as abilities' ApplyUpgradeValue. Levels/stacks are tracked per StatUpgradeId
/// so EvaluateValue(level) can be used when applying from UpgradeOffer.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Stat IDs (assign same assets as in Stat Upgrade Definitions)")]
    [SerializeField] private StatUpgradeId critChanceStatId;
    [SerializeField] private StatUpgradeId knockbackForceStatId;

    [Header("Base values (before upgrades)")]
    [Tooltip("Base crit chance 0..1. Upgrades add to this.")]
    [SerializeField][Range(0f, 1f)] private float baseCritChance;
    [Tooltip("Base knockback force multiplier (1 = normal). Upgrades add to this.")]
    [SerializeField] private float baseKnockbackForce = 1f;
    [Tooltip("Damage multiplier when a hit crits (e.g. 2 = double damage).")]
    [SerializeField] private float critDamageMultiplier = 2f;

    private float critChance;
    private float knockbackForce;
    private readonly Dictionary<StatUpgradeId, int> levels = new Dictionary<StatUpgradeId, int>();

    /// <summary>Current crit chance (0..1).</summary>
    public float CritChance => Mathf.Clamp01(critChance);

    /// <summary>Current knockback force multiplier (1 = normal, higher = stronger knockback sent to enemies).</summary>
    public float KnockbackForce => Mathf.Max(0f, knockbackForce);

    /// <summary>Multiplier applied to damage when a hit is a crit.</summary>
    public float CritDamageMultiplier => Mathf.Max(1f, critDamageMultiplier);

    private void Awake()
    {
        critChance = baseCritChance;
        knockbackForce = baseKnockbackForce;
    }

    /// <summary>
    /// Returns the current level/stack count for this stat (number of times this upgrade was applied).
    /// Use with offer.EvaluateValue(level) when applying a stat upgrade.
    /// </summary>
    public int GetLevel(StatUpgradeId statId)
    {
        if (statId == null) return 0;
        return levels.TryGetValue(statId, out int level) ? level : 0;
    }

    /// <summary>
    /// Applies a stat upgrade value (from offer.EvaluateValue(level)). Same role as ability's ApplyUpgradeValue:
    /// a single place to do calculations. Call IncrementLevel(statId) after applying.
    /// </summary>
    public void ApplyStatUpgradeValue(StatUpgradeId statId, float value)
    {
        if (statId == null) return;

        if (statId == critChanceStatId)
        {
            critChance += value;
            critChance = Mathf.Clamp01(critChance);
            return;
        }

        if (statId == knockbackForceStatId)
        {
            knockbackForce += value;
            knockbackForce = Mathf.Max(0f, knockbackForce);
        }
    }

    /// <summary>
    /// Increments the level/stack for this stat after applying an upgrade.
    /// </summary>
    public void IncrementLevel(StatUpgradeId statId)
    {
        if (statId == null) return;
        if (!levels.ContainsKey(statId))
            levels[statId] = 0;
        levels[statId]++;
    }

    /// <summary>
    /// Rolls for crit. Returns true if the hit is a crit.
    /// </summary>
    public bool RollCrit()
    {
        return CritChance > 0f && Random.value < CritChance;
    }
}
