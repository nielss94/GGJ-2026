using UnityEngine;

/// <summary>
/// Line-of-sight checks for enemies. Raycast to target; returns true only if the first hit is the target
/// (so we don't attack or shoot through walls). Use for melee and ranged attack eligibility.
/// </summary>
public class EnemySight : MonoBehaviour
{
    [Header("Origin")]
    [Tooltip("Raycast from here. Leave empty to use this transform's position.")]
    [SerializeField] private Transform sightOrigin;

    [Header("Raycast")]
    [Tooltip("Layers that block line of sight (e.g. Default, environment). Leave Everything to check hit only by identity.")]
    [SerializeField] private LayerMask blockLayers = -1;

    /// <summary>True if a clear ray from sight origin to target hits the target (or its children) first.</summary>
    public bool HasLineOfSightTo(Transform target)
    {
        if (target == null) return false;

        Vector3 origin = sightOrigin != null ? sightOrigin.position : transform.position;
        Vector3 toTarget = target.position - origin;
        float distance = toTarget.magnitude;
        if (distance < 0.01f) return true;

        Vector3 direction = toTarget / distance;
        if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, blockLayers))
            return true;

        Transform hitRoot = hit.collider.transform;
        while (hitRoot != null)
        {
            if (hitRoot == target) return true;
            hitRoot = hitRoot.parent;
        }

        return false;
    }
}
