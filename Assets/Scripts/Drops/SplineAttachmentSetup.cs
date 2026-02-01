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
    public enum RotationMode
    {
        Tangent,
        OutwardAndBackward
    }

    [Tooltip("Drop type this setup applies to (e.g. Feather).")]
    [SerializeField] private DropTypeId dropType;
    [Tooltip("Spline to place items along. Add a SplineContainer to your mask and draw the path in the scene view.")]
    [SerializeField] private SplineContainer splineContainer;
    [Tooltip("How to orient items: Tangent = along spline direction; OutwardAndBackward = point away from mask and backward.")]
    [SerializeField] private RotationMode rotationMode = RotationMode.OutwardAndBackward;
    [Tooltip("If true, item's forward is set by rotation mode. If false, rotation is left unchanged.")]
    [SerializeField] private bool applyRotation = true;
    [Tooltip("Which spline in the container to use (0 = first).")]
    [SerializeField] private int splineIndex;

    public DropTypeId DropType => dropType;

    /// <summary>Transform items should be parented to when using this spline (so they move with the spline).</summary>
    public Transform SplineTransform => splineContainer != null ? splineContainer.transform : null;

    /// <summary>Place one item at index i of total count (evenly distributed along the spline).</summary>
    public void PlaceAt(Transform item, int index, int total)
    {
        if (splineContainer == null || splineContainer.Splines == null || splineContainer.Splines.Count == 0)
            return;

        int idx = Mathf.Clamp(splineIndex, 0, splineContainer.Splines.Count - 1);
        float t = total > 1 ? (index + 1f) / (total + 1f) : 0.5f;
        splineContainer.Evaluate(idx, t, out float3 pos, out float3 tangent, out _);

        item.position = pos;

        if (!applyRotation)
            return;

        if (rotationMode == RotationMode.Tangent && math.lengthsq(tangent) > 0.0001f)
        {
            item.rotation = Quaternion.LookRotation(tangent);
            return;
        }

        if (rotationMode == RotationMode.OutwardAndBackward)
        {
            Vector3 position = pos;
            Transform refTransform = splineContainer.transform;
            Vector3 outward = (position - refTransform.position).normalized;
            Vector3 backward = -refTransform.forward;
            Vector3 direction = (outward + backward).normalized;
            if (direction.sqrMagnitude > 0.01f)
                item.rotation = Quaternion.LookRotation(direction);
        }
    }
}
