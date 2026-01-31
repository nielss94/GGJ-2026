using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

/// <summary>
/// Places items of one drop type along a Unity Spline. Assign a SplineContainer (edit the spline
/// in the scene view); feathers are spread evenly along it.
/// </summary>
[System.Serializable]
public class SplineAttachmentSetup
{
    [Tooltip("Drop type this setup applies to (e.g. Feather).")]
    [SerializeField] private DropTypeId dropType;
    [Tooltip("Spline to place items along. Add a SplineContainer to your mask and draw the path in the scene view.")]
    [SerializeField] private SplineContainer splineContainer;
    [Tooltip("If true, item's forward aligns with spline direction at that point.")]
    [SerializeField] private bool alignRotationToSpline = true;
    [Tooltip("Which spline in the container to use (0 = first).")]
    [SerializeField] private int splineIndex;

    public DropTypeId DropType => dropType;

    /// <summary>Place one item at index i of total count (evenly distributed along the spline).</summary>
    public void PlaceAt(Transform item, int index, int total)
    {
        if (splineContainer == null || splineContainer.Splines == null || splineContainer.Splines.Count == 0)
            return;

        int idx = Mathf.Clamp(splineIndex, 0, splineContainer.Splines.Count - 1);
        float t = total > 1 ? (index + 1f) / (total + 1f) : 0.5f;
        splineContainer.Evaluate(idx, t, out float3 pos, out float3 tangent, out _);

        item.position = pos;

        if (alignRotationToSpline && math.lengthsq(tangent) > 0.0001f)
            item.rotation = Quaternion.LookRotation(tangent);
    }
}
