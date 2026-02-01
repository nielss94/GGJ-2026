using UnityEngine;

/// <summary>
/// Drives a graphic to match the ultimate blast wave. Scale the assigned transform so it expands
/// from 0 to full blast radius over the blast duration. Add this to a GameObject with a mesh (e.g. Quad)
/// and a material (ring, soft circle, or full alpha) that looks good when scaled.
/// </summary>
public class UltimateBlastVisual : MonoBehaviour
{
    [Header("What to scale")]
    [Tooltip("Transform to scale (usually this transform). Should be a flat mesh (Quad/Plane) for a ground ring.")]
    [SerializeField] private Transform scaleTarget;

    [Header("Sizing")]
    [Tooltip("Scale = CurrentBlastRadius * this. Use 2 if your mesh is 1 unit radius (so diameter matches blast).")]
    [SerializeField] private float radiusToScale = 2f;
    [Tooltip("Height (Y scale) for a flat disc. Keeps the ring thin so it doesn't clip into the ground.")]
    [SerializeField] private float flatScaleY = 0.01f;

    [Header("Position")]
    [Tooltip("If true, set position to blast origin each frame (for world-space effects not under the player).")]
    [SerializeField] private bool followBlastOriginInWorld;

    [Header("Reference")]
    [Tooltip("Leave empty to find UltimateAbility in scene.")]
    [SerializeField] private UltimateAbility ultimateAbility;

    private Transform scaledTransform;
    private UltimateAbility cachedAbility;

    private void Awake()
    {
        scaledTransform = scaleTarget != null ? scaleTarget : transform;
    }

    private void Update()
    {
        if (cachedAbility == null)
            cachedAbility = ultimateAbility != null ? ultimateAbility : FindFirstObjectByType<UltimateAbility>();

        if (cachedAbility == null)
        {
            scaledTransform.gameObject.SetActive(false);
            return;
        }

        if (!cachedAbility.IsBlastActive)
        {
            scaledTransform.gameObject.SetActive(false);
            return;
        }

        scaledTransform.gameObject.SetActive(true);

        if (followBlastOriginInWorld)
            scaledTransform.position = cachedAbility.BlastOrigin;

        float r = cachedAbility.CurrentBlastRadius * radiusToScale;
        scaledTransform.localScale = new Vector3(r, flatScaleY, r);
    }
}
