using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized player drop management. Attach to the player (or a child). This is the single place
/// where drops are added, held, and removed:
/// <list type="bullet">
///   <item><b>Added:</b> <see cref="AddDrop"/> — call when a drop reaches the player (e.g. from <see cref="DroppableItem"/>).</item>
///   <item><b>Held:</b> <see cref="GetTotalDropCount"/>, <see cref="GetDropCount"/>, <see cref="GetAttached"/> — query current drops.</item>
///   <item><b>Removed:</b> <see cref="ClearAllDrops"/> — call when all drops are consumed (e.g. ultimate ability).</item>
/// </list>
/// Assign a <see cref="MaskAttachmentReceiver"/> (e.g. on the mask) or leave empty to resolve from children.
/// </summary>
public class PlayerDropManager : MonoBehaviour
{
    [Tooltip("Mask receiver that stores and places drops. If empty, resolved from this object or its children.")]
    [SerializeField] private MaskAttachmentReceiver maskReceiver;

    private MaskAttachmentReceiver Receiver => maskReceiver != null ? maskReceiver : (maskReceiver = ResolveReceiver());

    private MaskAttachmentReceiver ResolveReceiver()
    {
        return GetComponentInChildren<MaskAttachmentReceiver>(true);
    }

    /// <summary>World position drops fly towards before being added. Used by <see cref="DroppableItem"/>.</summary>
    public Vector3 FlyToPosition => Receiver != null ? Receiver.FlyToPosition : transform.position + Vector3.up;

    /// <summary>
    /// Add a drop when it reaches the player (single entry point for "added").
    /// Called by <see cref="DroppableItem"/> on arrival; delegates to <see cref="MaskAttachmentReceiver"/> for placement.
    /// </summary>
    public void AddDrop(DroppableItem item)
    {
        Debug.Log("Adding drop: " + item.name);
        if (Receiver != null)
            Receiver.Attach(item);
    }

    /// <summary>Total number of drops currently held (all types). Use for ultimate charge, UI, etc.</summary>
    public int GetTotalDropCount()
    {
        return Receiver != null ? Receiver.GetTotalAttachedCount() : 0;
    }

    /// <summary>Number of drops held for a specific type.</summary>
    public int GetDropCount(DropTypeId type)
    {
        return Receiver != null ? Receiver.GetAttachedCount(type) : 0;
    }

    /// <summary>All attached transforms for a type (read-only).</summary>
    public IReadOnlyList<Transform> GetAttached(DropTypeId type)
    {
        return Receiver != null ? Receiver.GetAttached(type) : (IReadOnlyList<Transform>)new List<Transform>();
    }

    /// <summary>
    /// Remove and destroy all held drops (single entry point for "removed").
    /// Call when the ultimate is fired or when resetting player state.
    /// </summary>
    public void ClearAllDrops()
    {
        if (Receiver != null)
            Receiver.ClearAllAttached();
    }
}
