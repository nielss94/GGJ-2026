using UnityEngine;

/// <summary>
/// ScriptableObject identifier for an ability stat. Use the same asset in ability upgrade definitions
/// and in abilities so they match by reference. Create via Assets > Create > GGJ-2026 > Ability Stat Id.
/// </summary>
[CreateAssetMenu(fileName = "New Ability Stat Id", menuName = "GGJ-2026/Ability Stat Id")]
public class AbilityStatId : ScriptableObject
{
    [Tooltip("Optional display name. If empty, the asset name is used.")]
    [SerializeField] private string displayName = "";

    /// <summary>Stable id for this stat. Uses displayName if set, otherwise the asset name.</summary>
    public string Id => !string.IsNullOrEmpty(displayName) ? displayName : name;
}
