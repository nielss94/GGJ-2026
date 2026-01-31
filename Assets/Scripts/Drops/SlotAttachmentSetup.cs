using UnityEngine;

/// <summary>
/// Places items of one drop type in fixed slots. Fill one by one; when all slots are used,
/// extra items stack at the last slot (optional local offset so they "grow" one by one).
/// </summary>
[System.Serializable]
public class SlotAttachmentSetup
{
    [Tooltip("Drop type this setup applies to (e.g. Horn).")]
    [SerializeField] private DropTypeId dropType;
    [Tooltip("Slot transforms in order. Items fill these one by one.")]
    [SerializeField] private Transform[] slots;
    [Tooltip("When slots are full, extra items stack at the last slot with this local offset per extra (e.g. (0,1,0) to grow upward).")]
    [SerializeField] private Vector3 stackOffsetPerExtra = new Vector3(0f, 0.2f, 0f);

    public DropTypeId DropType => dropType;

    /// <summary>Place one item at slot index (or stacked on last slot if index >= slot count).</summary>
    public void PlaceAt(Transform item, int index)
    {
        if (slots == null || slots.Length == 0)
            return;

        Transform slot;
        Vector3 localOffset = Vector3.zero;
        if (index < slots.Length)
        {
            slot = slots[index];
        }
        else
        {
            slot = slots[slots.Length - 1];
            int extra = index - slots.Length + 1;
            localOffset = stackOffsetPerExtra * extra;
        }

        item.position = slot.TransformPoint(localOffset);
        item.rotation = slot.rotation;
    }
}
