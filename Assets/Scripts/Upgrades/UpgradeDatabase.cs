using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer database of all upgrades and rarities. Configure rarities (probability + multiplier),
/// ability upgrades (name, slot, stat, curve), and stat upgrades. Use GetRandomAbilityUpgrade(s)
/// and GetRandomStatUpgrade(s) to draw weighted random offers.
/// Create via Assets > Create > GGJ-2026 > Upgrade Database.
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "GGJ-2026/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [Header("Rarities")]
    [Tooltip("All rarities. Probability weight and value multiplier are configured per rarity.")]
    [SerializeField] private List<UpgradeRarity> rarities = new List<UpgradeRarity>();

    [Header("Ability upgrades")]
    [Tooltip("All ability upgrades (slot, stat, curve). Drawn randomly with rarity weighting.")]
    [SerializeField] private List<AbilityUpgradeDefinition> abilityUpgrades = new List<AbilityUpgradeDefinition>();

    [Header("Stat upgrades")]
    [Tooltip("All stat upgrades. Drawn randomly with rarity weighting.")]
    [SerializeField] private List<StatUpgradeDefinition> statUpgrades = new List<StatUpgradeDefinition>();

    /// <summary>Read-only list of rarities.</summary>
    public IReadOnlyList<UpgradeRarity> Rarities => rarities;

    /// <summary>Read-only list of ability upgrade definitions.</summary>
    public IReadOnlyList<AbilityUpgradeDefinition> AbilityUpgrades => abilityUpgrades;

    /// <summary>Read-only list of stat upgrade definitions.</summary>
    public IReadOnlyList<StatUpgradeDefinition> StatUpgrades => statUpgrades;

    /// <summary>Picks a rarity by probability weight. Returns null if no rarities or total weight is 0.</summary>
    public UpgradeRarity GetRandomRarity()
    {
        if (rarities == null || rarities.Count == 0)
            return null;

        float total = 0f;
        foreach (var r in rarities)
        {
            if (r != null)
                total += r.ProbabilityWeight;
        }

        if (total <= 0f)
            return rarities[0];

        float roll = Random.Range(0f, total);
        foreach (var r in rarities)
        {
            if (r == null) continue;
            roll -= r.ProbabilityWeight;
            if (roll <= 0f)
                return r;
        }

        return rarities[rarities.Count - 1];
    }

    /// <summary>Returns a random ability upgrade offer (random rarity + random ability definition). Null if none configured.</summary>
    public UpgradeOffer GetRandomAbilityUpgrade()
    {
        if (abilityUpgrades == null || abilityUpgrades.Count == 0)
            return null;

        UpgradeRarity rarity = GetRandomRarity();
        if (rarity == null)
            return null;

        var def = abilityUpgrades[Random.Range(0, abilityUpgrades.Count)];
        if (def == null)
            return null;

        return new UpgradeOffer(def, rarity);
    }

    /// <summary>Returns a random stat upgrade offer. Null if none configured.</summary>
    public UpgradeOffer GetRandomStatUpgrade()
    {
        if (statUpgrades == null || statUpgrades.Count == 0)
            return null;

        UpgradeRarity rarity = GetRandomRarity();
        if (rarity == null)
            return null;

        var def = statUpgrades[Random.Range(0, statUpgrades.Count)];
        if (def == null)
            return null;

        return new UpgradeOffer(def, rarity);
    }

    /// <summary>
    /// Fills the list with random upgrade offers. Specify how many ability and how many stat upgrades to draw.
    /// Duplicates are possible; caller can filter or redraw if needed.
    /// </summary>
    public void GetRandomUpgrades(List<UpgradeOffer> outOffers, int abilityCount, int statCount)
    {
        if (outOffers == null) return;
        outOffers.Clear();

        for (int i = 0; i < abilityCount; i++)
        {
            var offer = GetRandomAbilityUpgrade();
            if (offer != null)
                outOffers.Add(offer);
        }

        for (int i = 0; i < statCount; i++)
        {
            var offer = GetRandomStatUpgrade();
            if (offer != null)
                outOffers.Add(offer);
        }
    }

    /// <summary>
    /// Returns a list of random upgrades: totalCount total, with a mix of ability and stat upgrades.
    /// If mixRatio is 0.5, half are ability and half stat. mixRatio 1 = all ability, 0 = all stat.
    /// </summary>
    public List<UpgradeOffer> GetRandomUpgrades(int totalCount, float abilityRatio = 0.5f)
    {
        int abilityCount = Mathf.RoundToInt(totalCount * Mathf.Clamp01(abilityRatio));
        int statCount = totalCount - abilityCount;
        var list = new List<UpgradeOffer>(totalCount);
        GetRandomUpgrades(list, abilityCount, statCount);
        return list;
    }
}
