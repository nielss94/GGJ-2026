using UnityEngine;

/// <summary>
/// Subscribes to EventBus.UpgradeChosen and applies the chosen upgrade (ability or stat from UpgradeOffer).
/// Ability upgrades: passes stat id and evaluated value to the ability's ApplyUpgradeValue.
/// Stat upgrades: applied via ApplyStatUpgrade (override or extend for your stat system).
/// </summary>
public class PlayerUpgradeApplier : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to resolve at runtime via FindFirstObjectByType.")]
    [SerializeField] private PlayerAbilityManager abilityManager;

    private void OnEnable()
    {
        EventBus.UpgradeChosen += OnUpgradeChosen;
    }

    private void OnDisable()
    {
        EventBus.UpgradeChosen -= OnUpgradeChosen;
    }

    private void OnUpgradeChosen(UpgradeOffer offer)
    {
        if (offer == null) return;

        if (offer.IsAbility)
        {
            ApplyAbilityUpgrade(offer);
            return;
        }

        ApplyStatUpgrade(offer);
    }

    private void ApplyAbilityUpgrade(UpgradeOffer offer)
    {
        PlayerAbilityManager manager = GetAbilityManager();
        if (manager == null) return;
        if (!offer.AbilitySlot.HasValue) return;
        if (offer.AbilityStatIdRef == null) return;

        PlayerAbility ability = manager.GetAbility(offer.AbilitySlot.Value);
        if (ability == null) return;

        int level = ability.level;
        float value = offer.EvaluateValue(level);
        ability.ApplyUpgradeValue(offer.AbilityStatIdRef, value);
    }

    /// <summary>
    /// Called for stat upgrades. Override to apply to your stat system (e.g. max health, move speed).
    /// Use offer.StatId and offer.EvaluateValue(level) where level is your stack count or stat level.
    /// </summary>
    protected virtual void ApplyStatUpgrade(UpgradeOffer offer)
    {
        // Placeholder: extend with your stat system (e.g. PlayerStats.Apply(offer.StatId, offer.EvaluateValue(stacks)))
    }

    private PlayerAbilityManager GetAbilityManager()
    {
        if (abilityManager != null)
            return abilityManager;
        return FindFirstObjectByType<PlayerAbilityManager>();
    }
}
