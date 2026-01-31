using UnityEngine;

/// <summary>
/// Designer-configurable definition for an ability upgrade. Specifies which ability slot,
/// which stat the upgrade affects, and a curve (level on X, value on Y) for the base value.
/// The ability code decides what to do with the value (e.g. add to dash distance).
/// Create via Assets > Create > GGJ-2026 > Ability Upgrade Definition.
/// </summary>
[CreateAssetMenu(fileName = "New Ability Upgrade", menuName = "GGJ-2026/Ability Upgrade Definition")]
public class AbilityUpgradeDefinition : ScriptableObject
{
    [Tooltip("Display name for the upgrade card.")]
    [SerializeField] private string displayName = "Ability Upgrade";

    [Tooltip("Short description for the card.")]
    [SerializeField][TextArea(1, 3)] private string description = "";

    [Tooltip("Which ability slot (A/B/X/Y) this upgrade targets.")]
    [SerializeField] private PlayerAbilityManager.AbilitySlot abilitySlot = PlayerAbilityManager.AbilitySlot.A;

    [Tooltip("Stat id (ScriptableObject). Use the same asset as on the ability so they match by reference.")]
    [SerializeField] private AbilityStatId statId;

    [Tooltip("Curve: X = level, Y = base value. Evaluated at the ability's current level; rarity multiplier is applied separately.")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(1f, 0f, 10f, 1f);

    /// <summary>Display name for UI.</summary>
    public string DisplayName => displayName;

    /// <summary>Description for the card.</summary>
    public string Description => description;

    /// <summary>Ability slot this upgrade applies to.</summary>
    public PlayerAbilityManager.AbilitySlot AbilitySlot => abilitySlot;

    /// <summary>Stat id passed to the ability's ApplyUpgradeValue. Null if not assigned.</summary>
    public AbilityStatId StatId => statId;

    /// <summary>Evaluates the curve at the given level. Level is clamped to the curve's time range.</summary>
    public float EvaluateAtLevel(int level)
    {
        if (curve == null || curve.keys.Length == 0)
            return 0f;
        float minTime = curve.keys[0].time;
        float maxTime = curve.keys[curve.keys.Length - 1].time;
        float t = Mathf.Clamp(level, minTime, maxTime);
        return curve.Evaluate(t);
    }
}
