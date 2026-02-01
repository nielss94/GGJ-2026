using UnityEngine;

/// <summary>
/// Breakable pot: disables root collider/rigidbody/renderer and enables child fragment colliders with rigidbodies.
/// Assign root references in the inspector; call BreakPot() when the pot should break (e.g. from Health OnDeath).
/// </summary>
public class PotBreak : MonoBehaviour
{
    [Header("Root (assign in inspector)")]
    [SerializeField] private Collider rootCollider;
    [SerializeField] private Rigidbody rootRigidbody;
    [SerializeField] private Renderer potRenderer;

    [Header("Break")]
    [SerializeField] private float explosionForce = 5f;
    [Tooltip("Upward bias for explosion direction (0 = purely radial, 1 = more upward).")]
    [SerializeField][Range(0f, 1f)] private float upwardBias = 0.4f;
    [SerializeField] private float fragmentMass = 0.2f;

    [Header("Audio")]
    [Tooltip("Optional FMOD event played when the pot breaks.")]
    [SerializeField] private FmodEventAsset fmodPotBreak;

    public void BreakPot()
    {
        Vector3 center = transform.position;

        if (rootCollider != null)
            Destroy(rootCollider);
        if (rootRigidbody != null)
            Destroy(rootRigidbody);
        if (potRenderer != null)
            Destroy(potRenderer);

        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in allColliders)
        {
            if (c == rootCollider) continue;
            c.enabled = true;
            // MeshColliders must be convex when on a dynamic Rigidbody, or they can fall through the ground
            if (c is MeshCollider meshCol)
                meshCol.convex = true;
            Rigidbody rb = c.GetComponent<Rigidbody>();
            if (rb == null)
                rb = c.gameObject.AddComponent<Rigidbody>();
            rb.mass = fragmentMass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            rb.AddExplosionForce(explosionForce, center, 1f, upwardBias, ForceMode.Impulse);
        }

        if (fmodPotBreak != null && AudioService.Instance != null)
            AudioService.Instance.PlayOneShot(fmodPotBreak, transform.position);
    }
}
