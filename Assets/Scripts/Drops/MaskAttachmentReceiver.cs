using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the player's mask object. Receives dropped items that fly in and places them using
/// spline or slot strategies per drop type. Assign mask transform (usually this transform) and
/// configure spline/slot setups for each DropTypeId.
/// </summary>
public class MaskAttachmentReceiver : MonoBehaviour
{
    [Header("Mask")]
    [Tooltip("The mask transform items attach to. Leave empty to use this transform.")]
    [SerializeField] private Transform maskTransform;

    [Header("Spline attachments (e.g. feathers)")]
    [Tooltip("Drop types that distribute along a spline. Items divide evenly along the spline by count.")]
    [SerializeField] private List<SplineAttachmentSetup> splineSetups = new List<SplineAttachmentSetup>();

    [Header("Slot attachments (e.g. horns)")]
    [Tooltip("Drop types that use fixed slots. Fill one by one; when full, extra items stack at the last slot.")]
    [SerializeField] private List<SlotAttachmentSetup> slotSetups = new List<SlotAttachmentSetup>();

    private Transform Mask => maskTransform != null ? maskTransform : transform;

    /// <summary>World position drops fly towards before attaching. Use when receiver is not on the mask object.</summary>
    public Vector3 FlyToPosition => Mask.position;

    /// <summary>Count of attached items per drop type (for redistribution when using spline).</summary>
    private readonly Dictionary<DropTypeId, int> attachedCountByType = new Dictionary<DropTypeId, int>();

    /// <summary>Attached item instances per type, in order (for slot placement and spline redistribution).</summary>
    private readonly Dictionary<DropTypeId, List<Transform>> attachedByType = new Dictionary<DropTypeId, List<Transform>>();

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

        if (!attachedByType.TryGetValue(type, out var list))
        {
            list = new List<Transform>();
            attachedByType[type] = list;
            attachedCountByType[type] = 0;
        }

        int index = list.Count;
        list.Add(item.transform);
        attachedCountByType[type] = list.Count;

        Quaternion rotationBeforePlace = item.transform.rotation;

        if (TryGetSplineSetup(type, out var spline))
        {
            // Parent under spline container so feathers move with the spline (e.g. when using SplineFollowTarget)
            Transform parent = spline.SplineTransform != null ? spline.SplineTransform : Mask;
            item.transform.SetParent(parent, worldPositionStays: true);
            RedistributeSpline(type, spline);
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
        if (splineSetups != null)
        {
            foreach (var s in splineSetups)
            {
                if (s != null && s.DropType == type)
                {
                    setup = s;
                    return true;
                }
            }
        }

        setup = null;
        return false;
    }

    private bool TryGetSlotSetup(DropTypeId type, out SlotAttachmentSetup setup)
    {
        if (slotSetups != null)
        {
            foreach (var s in slotSetups)
            {
                if (s != null && s.DropType == type)
                {
                    setup = s;
                    return true;
                }
            }
        }

        setup = null;
        return false;
    }

    private void RedistributeSpline(DropTypeId type, SplineAttachmentSetup spline)
    {
        if (!attachedByType.TryGetValue(type, out var list) || list.Count == 0)
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
        return attachedByType.TryGetValue(type, out var list) ? list.Count : 0;
    }

    /// <summary>Total number of attached drops across all types. Used for ultimate ability charge.</summary>
    public int GetTotalAttachedCount()
    {
        int total = 0;
        foreach (var list in attachedByType.Values)
        {
            if (list != null)
                total += list.Count;
        }
        return total;
    }

    /// <summary>All attached transforms for a type. Read-only.</summary>
    public IReadOnlyList<Transform> GetAttached(DropTypeId type)
    {
        return attachedByType.TryGetValue(type, out var list) ? list : (IReadOnlyList<Transform>)new List<Transform>();
    }

    /// <summary>
    /// Removes and destroys all attached drops. Used when the ultimate ability is fired;
    /// the ability becomes available again once the player gathers enough drops again.
    /// </summary>
    public void ClearAllAttached()
    {
        foreach (var list in attachedByType.Values)
        {
            if (list == null) continue;
            foreach (Transform t in list)
            {
                if (t != null && t.gameObject != null)
                    Destroy(t.gameObject);
            }
            list.Clear();
        }
        attachedCountByType.Clear();
    }
}
