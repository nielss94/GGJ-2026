using UnityEngine;

/// <summary>
/// Designer-configurable definition for a stat upgrade (e.g. max health, move speed).
/// Uses a curve (level on X, value on Y) for the base value; rarity multiplier is applied when offered.
/// Create via Assets > Create > GGJ-2026 > Stat Upgrade Definition.
/// </summary>
[CreateAssetMenu(fileName = "New Stat Upgrade", menuName = "GGJ-2026/Stat Upgrade Definition")]
public class StatUpgradeDefinition : ScriptableObject
{
    [Tooltip("Display name for the upgrade card.")]
    [SerializeField] private string displayName = "Stat Upgrade";

    [Tooltip("Short description for the card.")]
    [SerializeField][TextArea(1, 3)] private string description = "";

    [Tooltip("Stat id (ScriptableObject). Use the same asset as in your stat applier so they match by reference.")]
    [SerializeField] private StatUpgradeId statId;

    [Tooltip("Curve: X = level / stack count, Y = base value. Rarity multiplier is applied when offered.")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(1f, 0f, 10f, 1f);

    [Tooltip("Optional. FMOD event/parameter name sent for this upgrade (e.g. parameter label). If empty, DisplayName (lowercase) is used.")]
    [SerializeField] private string fmodEventName = "";

    /// <summary>Display name for UI.</summary>
    public string DisplayName => displayName;

    /// <summary>FMOD event/parameter name for this stat upgrade. If not set, use DisplayName (lowercase) when sending parameters.</summary>
    public string FmodEventName => string.IsNullOrEmpty(fmodEventName) ? displayName : fmodEventName;

    /// <summary>Description for the card.</summary>
    public string Description => description;

    /// <summary>Stat id used when applying the upgrade. Null if not assigned.</summary>
    public StatUpgradeId StatId => statId;

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
