using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

/// <summary>
/// Builds a half-circle spline in a SplineContainer (e.g. on top of the mask) so feathers
/// can be attached along it. Add to the same GameObject as the SplineContainer, set radius
/// and plane, then call Build Half Circle (context menu or at runtime).
/// </summary>
[ExecuteInEditMode]
public class HalfCircleSplineBuilder : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("SplineContainer to fill with a half circle. Leave empty to use on this GameObject.")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Half circle")]
    [Tooltip("Radius of the half circle in local space.")]
    [SerializeField] private float radius = 0.5f;
    [Tooltip("Plane of the arc: 0 = XZ (horizontal), 1 = XY (arc over top of mask), 2 = YZ.")]
    [SerializeField] private int planeIndex = 1;
    [Tooltip("Number of knots. More = smoother arc. 5–9 is usually enough.")]
    [SerializeField] private int knotCount = 7;
    [Tooltip("If true, arc is the top half (y >= 0 in XY). If false, bottom half.")]
    [SerializeField] private bool topHalf = true;

    private void Awake()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
    }

    private void OnValidate()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
        knotCount = Mathf.Clamp(knotCount, 3, 32);
        planeIndex = Mathf.Clamp(planeIndex, 0, 2);
    }

    /// <summary>Fills the assigned SplineContainer's first spline with a half circle. Call from context menu or at runtime.</summary>
    [ContextMenu("Build Half Circle")]
    public void BuildHalfCircle()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null || splineContainer.Splines == null || splineContainer.Splines.Count == 0)
        {
            Debug.LogWarning("HalfCircleSplineBuilder: No SplineContainer assigned or empty.", this);
            return;
        }

        Spline spline = splineContainer.Spline;
        spline.Clear();

        float angleStart = topHalf ? 0f : Mathf.PI;
        float angleEnd = topHalf ? Mathf.PI : 0f;
        for (int i = 0; i < knotCount; i++)
        {
            float t = knotCount > 1 ? (float)i / (knotCount - 1) : 0f;
            float angle = Mathf.Lerp(angleStart, angleEnd, t);
            float3 pos = GetPointOnHalfCircle(angle);
            spline.Add(new BezierKnot(pos), TangentMode.Mirrored);
        }

        spline.Closed = false;
    }

    private float3 GetPointOnHalfCircle(float angleRad)
    {
        float x = math.cos(angleRad) * radius;
        float y = math.sin(angleRad) * radius;
        // planeIndex: 0 = XZ (x, 0, z from circle), 1 = XY (x, y, 0), 2 = YZ (0, x, y)
        return planeIndex switch
        {
            0 => new float3(x, 0f, y),   // horizontal arc: XZ
            1 => new float3(x, y, 0f),   // vertical arc over top: XY
            _ => new float3(0f, x, y)    // YZ
        };
    }
}
