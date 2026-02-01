using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Put this on the blast wave prefab. After Init(duration, radius) is called, expands from 0 to full
/// radius over the duration, then destroys the GameObject. Use with UltimateAbility's Blast Wave Prefab.
/// If the prefab has a DecalProjector (HDRP), the blast is drawn as a decal on the floor (no clipping).
/// Otherwise scales the assigned transform (e.g. Quad).
/// </summary>
public class BlastWaveInstance : MonoBehaviour
{
    [Header("Decal (HDRP)")]
    [Tooltip("If a DecalProjector is present, use it so the blast projects onto the floor. Lay flat with small offset to avoid z-fighting.")]
    [SerializeField] private float decalProjectionDepth = 0.5f;
    [Tooltip("Height above ground when using decal (avoids z-fighting).")]
    [SerializeField] private float groundOffset = 0.02f;
    [Tooltip("Layers to raycast for ground.")]
    [SerializeField] private LayerMask groundLayers = -1;

    [Header("Fallback (Quad/mesh)")]
    [Tooltip("When not using DecalProjector: scale = currentRadius * this. Use 2 if mesh is 1 unit radius.")]
    [SerializeField] private float radiusToScale = 2f;
    [Tooltip("Y scale for a flat disc when not using decal.")]
    [SerializeField] private float flatScaleY = 0.01f;
    [Tooltip("Transform to scale when not using decal. Leave empty to use this transform.")]
    [SerializeField] private Transform scaleTarget;

    private Transform scaledTransform;
    private DecalProjector decalProjector;
    private float duration;
    private float radius;
    private float elapsed;
    private bool useDecal;

    /// <summary>Call after instantiating. Starts the expand animation; object destroys itself when done.</summary>
    public void Init(float durationSeconds, float blastRadius)
    {
        duration = Mathf.Max(0.001f, durationSeconds);
        radius = Mathf.Max(0f, blastRadius);
        elapsed = 0f;

        decalProjector = GetComponentInChildren<DecalProjector>();
        useDecal = decalProjector != null;

        if (useDecal)
        {
            PlaceDecalOnGround();
            decalProjector.size = new Vector3(0f, 0f, decalProjectionDepth);
        }
        else
        {
            scaledTransform = scaleTarget != null ? scaleTarget : transform;
            scaledTransform.localScale = Vector3.zero;
        }
    }

    private void PlaceDecalOnGround()
    {
        Vector3 origin = transform.position + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, groundLayers))
        {
            transform.position = hit.point + Vector3.up * groundOffset;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float currentRadius = t * radius;
        float diameter = currentRadius * 2f;

        if (useDecal && decalProjector != null)
        {
            decalProjector.size = new Vector3(diameter, diameter, decalProjectionDepth);
        }
        else if (scaledTransform != null)
        {
            float s = currentRadius * radiusToScale;
            scaledTransform.localScale = new Vector3(s, flatScaleY, s);
        }

        if (elapsed >= duration)
            Destroy(gameObject);
    }
}
