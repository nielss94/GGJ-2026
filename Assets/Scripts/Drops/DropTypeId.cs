using UnityEngine;

/// <summary>
/// ScriptableObject identifier for a drop type (e.g. Feather, Horn). Used to match drops to
/// attachment strategies on the mask (spline vs slots). Create via Assets > Create > GGJ-2026 > Drop Type Id.
/// </summary>
[CreateAssetMenu(fileName = "New Drop Type Id", menuName = "GGJ-2026/Drop Type Id")]
public class DropTypeId : ScriptableObject
{
    [Tooltip("Optional display name. If empty, the asset name is used.")]
    [SerializeField] private string displayName = "";

    /// <summary>Stable id for this drop type. Uses displayName if set, otherwise the asset name.</summary>
    public string Id => !string.IsNullOrEmpty(displayName) ? displayName : name;
}
