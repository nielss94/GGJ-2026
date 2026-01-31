using UnityEngine;

/// <summary>
/// ScriptableObject identifier for a global stat (e.g. MaxHealth, MoveSpeed). Use the same asset
/// in stat upgrade definitions and in your stat applier so they match by reference.
/// Create via Assets > Create > GGJ-2026 > Stat Upgrade Id.
/// </summary>
[CreateAssetMenu(fileName = "New Stat Upgrade Id", menuName = "GGJ-2026/Stat Upgrade Id")]
public class StatUpgradeId : ScriptableObject
{
    [Tooltip("Optional display name. If empty, the asset name is used.")]
    [SerializeField] private string displayName = "";

    /// <summary>Stable id for this stat. Uses displayName if set, otherwise the asset name.</summary>
    public string Id => !string.IsNullOrEmpty(displayName) ? displayName : name;
}
