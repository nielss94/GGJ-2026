using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the player's mask object. Implementation detail of <see cref="PlayerDropManager"/>:
/// stores and places drops (spline/slot per DropTypeId). The public API for add/hold/remove is
/// <see cref="PlayerDropManager"/> on the player; this component is typically assigned there.
/// </summary>
public class MaskAttachmentReceiver : MonoBehaviour
{
    [Header("Mask")]
    [Tooltip("The mask transform items attach to. Leave empty to use this transform.")]
    [SerializeField] private Transform maskTransform;
    [Tooltip("Optional: transform drops fly towards (e.g. face/mask bone). If empty, uses mask position + Fly To Height Offset.")]
    [SerializeField] private Transform flyToTarget;
    [Tooltip("World Y offset above mask position when Fly To Target is empty. Use this so drops arc toward the mask instead of the feet.")]
    [SerializeField] private float flyToHeightOffset = 1f;

    [Header("Spline attachments (e.g. feathers)")]
    [Tooltip("Drop types that distribute along a spline. Items divide evenly along the spline by count.")]
    [SerializeField] private List<SplineAttachmentSetup> splineSetups = new List<SplineAttachmentSetup>();

    [Header("Slot attachments (e.g. horns)")]
    [Tooltip("Drop types that use fixed slots. Fill one by one; when full, extra items stack at the last slot.")]
    [SerializeField] private List<SlotAttachmentSetup> slotSetups = new List<SlotAttachmentSetup>();

    private Transform Mask => maskTransform != null ? maskTransform : transform;

    /// <summary>World position drops fly towards before attaching. Uses Fly To Target if set, else mask position + height offset.</summary>
    public Vector3 FlyToPosition =>
        flyToTarget != null
            ? flyToTarget.position
            : Mask.position + Vector3.up * flyToHeightOffset;

    /// <summary>Count of attached items per drop type (for redistribution when using spline).</summary>
    private readonly Dictionary<string, int> attachedCountByTypeId = new Dictionary<string, int>();

    /// <summary>Attached item instances per type (keyed by DropTypeId.Id so different asset instances match).</summary>
    private readonly Dictionary<string, List<Transform>> attachedByTypeId = new Dictionary<string, List<Transform>>();

    private void Awake()
    {
        if (maskTransform == null)
            maskTransform = transform;
    }

    /// <summary>
    /// Called when a dropped item reaches the mask. Places it using the configured strategy for its type.
    /// </summary>
    public void Attach(DroppableItem item)
    {
        if (item == null || item.DropType == null)
            return;

        DropTypeId type = item.DropType;
        string typeId = type.Id;

        if (!attachedByTypeId.TryGetValue(typeId, out var list))
        {
            list = new List<Transform>();
            attachedByTypeId[typeId] = list;
            attachedCountByTypeId[typeId] = 0;
        }

        int index = list.Count;
        list.Add(item.transform);
        attachedCountByTypeId[typeId] = list.Count;

        Quaternion rotationBeforePlace = item.transform.rotation;

        if (TryGetSplineSetup(type, out var spline))
        {
            // Parent under spline container so feathers move with the spline (e.g. when using SplineFollowTarget)
            Transform parent = spline.SplineTransform != null ? spline.SplineTransform : Mask;
            item.transform.SetParent(parent, worldPositionStays: true);
            RedistributeSpline(typeId, spline);
            item.AnimateSettleRotation(rotationBeforePlace, item.SettleRotationDuration);
            return;
        }

        // Parent under mask for non-spline attachments
        item.transform.SetParent(Mask, worldPositionStays: true);

        if (TryGetSlotSetup(type, out var slot))
        {
            PlaceInSlot(item.transform, index, slot);
            item.AnimateSettleRotation(rotationBeforePlace, item.SettleRotationDuration);
            return;
        }

        // Unknown type: just keep under mask at current position
    }

    private bool TryGetSplineSetup(DropTypeId type, out SplineAttachmentSetup setup)
    {
        if (type == null || splineSetups == null)
        {
            setup = null;
            return false;
        }

        string typeId = type.Id;
        foreach (var s in splineSetups)
        {
            if (s != null && s.DropType != null && s.DropType.Id == typeId && s.HasValidSpline)
            {
                setup = s;
                return true;
            }
        }

        setup = null;
        return false;
    }

    private bool TryGetSlotSetup(DropTypeId type, out SlotAttachmentSetup setup)
    {
        if (type == null || slotSetups == null)
        {
            setup = null;
            return false;
        }

        string typeId = type.Id;
        foreach (var s in slotSetups)
        {
            if (s != null && s.DropType != null && s.DropType.Id == typeId)
            {
                setup = s;
                return true;
            }
        }

        setup = null;
        return false;
    }

    private void RedistributeSpline(string typeId, SplineAttachmentSetup spline)
    {
        if (!attachedByTypeId.TryGetValue(typeId, out var list) || list.Count == 0)
            return;

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            spline.PlaceAt(list[i], i, count);
        }
    }

    private void PlaceInSlot(Transform item, int index, SlotAttachmentSetup slot)
    {
        slot.PlaceAt(item, index);
    }

    /// <summary>Number of attached items of this type. Useful for UI or ultimate ability.</summary>
    public int GetAttachedCount(DropTypeId type)
    {
        if (type == null) return 0;
        return attachedByTypeId.TryGetValue(type.Id, out var list) ? list.Count : 0;
    }

    /// <summary>Total number of attached drops across all types. Used for ultimate ability charge.</summary>
    public int GetTotalAttachedCount()
    {
        int total = 0;
        foreach (var list in attachedByTypeId.Values)
        {
            if (list != null)
                total += list.Count;
        }
        return total;
    }

    /// <summary>All attached transforms for a type. Read-only.</summary>
    public IReadOnlyList<Transform> GetAttached(DropTypeId type)
    {
        if (type == null) return (IReadOnlyList<Transform>)new List<Transform>();
        return attachedByTypeId.TryGetValue(type.Id, out var list) ? list : (IReadOnlyList<Transform>)new List<Transform>();
    }

    /// <summary>
    /// Removes and destroys all attached drops. Used when the ultimate ability is fired;
    /// the ability becomes available again once the player gathers enough drops again.
    /// </summary>
    public void ClearAllAttached()
    {
        foreach (var list in attachedByTypeId.Values)
        {
            if (list == null) continue;
            foreach (Transform t in list)
            {
                if (t != null && t.gameObject != null)
                    Destroy(t.gameObject);
            }
            list.Clear();
        }
        attachedCountByTypeId.Clear();
    }
}
