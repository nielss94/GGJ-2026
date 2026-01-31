using UnityEngine;

/// <summary>
/// Subscribes to EventBus.UpgradeChosen and applies the chosen upgrade (e.g. levels up the corresponding ability).
/// Keeps UpgradePanel decoupled from PlayerAbilityManager; attach to the player or a persistent manager.
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

    private void OnUpgradeChosen(UpgradeType type)
    {
        // --- Differentiate here: ability-based upgrades (level up a slot) vs stat/other upgrades (e.g. max health, modifiers). ---
        // If type is a stat upgrade (e.g. Health), apply it here and return; otherwise fall through to ability level-up.

        PlayerAbilityManager manager = GetAbilityManager();
        if (manager == null)
            return;

        PlayerAbilityManager.AbilitySlot? slot = GetAbilitySlotForUpgrade(type);
        if (slot == null)
            return;

        PlayerAbility ability = manager.GetAbility(slot.Value);
        if (ability != null)
            ability.LevelUp();
    }

    private PlayerAbilityManager GetAbilityManager()
    {
        if (abilityManager != null)
            return abilityManager;
        return FindFirstObjectByType<PlayerAbilityManager>();
    }

    /// <summary>
    /// Maps upgrade type to the ability slot to level up. Override or extend for custom mapping or future Health/Speed stats.
    /// </summary>
    protected virtual PlayerAbilityManager.AbilitySlot? GetAbilitySlotForUpgrade(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.Damage => PlayerAbilityManager.AbilitySlot.X,
            UpgradeType.Speed => PlayerAbilityManager.AbilitySlot.A,
            UpgradeType.Health => null,
            _ => null
        };
    }
}
