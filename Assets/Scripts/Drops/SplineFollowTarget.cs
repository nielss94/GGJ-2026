using UnityEngine;

/// <summary>
/// Makes this transform follow a target (e.g. an animated bone) in LateUpdate so it moves with
/// the character's animations. Add to the same GameObject as your SplineContainer. Assign the
/// bone that actually moves during mask/head animations (e.g. the mask bone or head bone).
/// </summary>
public class SplineFollowTarget : MonoBehaviour
{
    [Tooltip("Transform to follow (e.g. the animated mask/head bone). If empty, this component does nothing.")]
    [SerializeField] private Transform followTarget;

    private void LateUpdate()
    {
        if (followTarget == null)
            return;

        transform.SetPositionAndRotation(followTarget.position, followTarget.rotation);
    }
}
