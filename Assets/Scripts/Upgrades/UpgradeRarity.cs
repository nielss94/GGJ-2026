using UnityEngine;

/// <summary>
/// Designer-configurable rarity for upgrades. Probability is used when drawing random upgrades;
/// value multiplier is applied to the curve-evaluated value when the upgrade is applied.
/// Create via Assets > Create > GGJ-2026 > Upgrade Rarity.
/// </summary>
[CreateAssetMenu(fileName = "New Rarity", menuName = "GGJ-2026/Upgrade Rarity")]
public class UpgradeRarity : ScriptableObject
{
    [Tooltip("Display name (e.g. Common, Rare, Legendary).")]
    [SerializeField] private string displayName = "Common";

    [Tooltip("Probability weight when drawing a random upgrade. Higher = more likely. Need not sum to 1.")]
    [SerializeField][Min(0f)] private float probabilityWeight = 1f;

    [Tooltip("Multiplier applied to the curve-evaluated value when this rarity is rolled.")]
    [SerializeField][Min(0.01f)] private float valueMultiplier = 1f;

    [Tooltip("Color used to visualise this rarity in the upgrade panel (e.g. rarity text or card tint).")]
    [SerializeField] private Color displayColor = Color.white;

    [Tooltip("Optional. FMOD event/parameter name sent for this rarity (e.g. parameter label). If empty, DisplayName (lowercase) is used.")]
    [SerializeField] private string fmodEventName = "";

    /// <summary>Display name for UI.</summary>
    public string DisplayName => displayName;

    /// <summary>FMOD event/parameter name for this rarity. If not set, use DisplayName (lowercase) when sending parameters.</summary>
    public string FmodEventName => string.IsNullOrEmpty(fmodEventName) ? displayName : fmodEventName;

    /// <summary>Color for rarity visualisation in the upgrade panel.</summary>
    public Color DisplayColor => displayColor;

    /// <summary>Weight used in weighted random selection. Higher = more likely.</summary>
    public float ProbabilityWeight => probabilityWeight;

    /// <summary>Multiplier applied to the upgrade's evaluated curve value.</summary>
    public float ValueMultiplier => valueMultiplier;
}
