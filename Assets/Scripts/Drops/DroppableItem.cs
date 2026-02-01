using UnityEngine;
using PrimeTween;

/// <summary>
/// Put on drop prefabs (any mesh, no colliders). After a short drop animation, flies towards the
/// player's drop manager target until it reaches it, then is added via <see cref="PlayerDropManager.AddDrop"/>.
/// </summary>
[RequireComponent(typeof(Transform))]
public class DroppableItem : MonoBehaviour
{
    [Header("Drop animation")]
    [Tooltip("Duration of the initial pop-in before flying to mask.")]
    [SerializeField] private float dropAnimDuration = 0.35f;
    [Tooltip("Scale at spawn (pop-in).")]
    [SerializeField] private float spawnScale = 0.3f;
    [Tooltip("Optional: small upward bounce (world up) during drop anim.")]
    [SerializeField] private float bounceHeight = 0.4f;

    [Header("Fly to mask")]
    [Tooltip("Max speed (units per second) when flying towards the mask. Slows down as it gets close for a soft arrival.")]
    [SerializeField] private float flySpeed = 4f;
    [Tooltip("When closer than this, speed eases out so the item glides in instead of snapping. Ghost-like.")]
    [SerializeField] private float slowDownDistance = 2f;
    [Tooltip("Distance to target at which the item is considered arrived and attaches.")]
    [SerializeField] private float attachDistance = 0.05f;
    [Tooltip("Peak height of the arc (world units). Height is computed from current distance so the path arcs over obstacles.")]
    [SerializeField] private float arcHeight = 2f;

    [Header("Settle")]
    [Tooltip("Duration to rotate from arrival rotation to final placement rotation after attaching.")]
    [SerializeField] private float settleRotationDuration = 0.6f;
    [SerializeField] private Ease settleRotationEase = Ease.OutQuad;

    private DropTypeId dropType;
    private PlayerDropManager targetManager;
    private Sequence currentSequence;
    private Vector3 targetScale;
    private bool isFlyingTowardsMask;
    private Vector3 flyStartPosition;
    private float flyTotalDistance;
    private float flyProgress;

    /// <summary>Drop type for this item. Set by Init(); used by mask placement to choose strategy.</summary>
    public DropTypeId DropType => dropType;

    /// <summary>Duration of the settle rotation after attach. Used by mask placement.</summary>
    public float SettleRotationDuration => settleRotationDuration;

    /// <summary>
    /// Call after instantiating the drop. Sets type and target; starts drop animation then flight to player drop manager.
    /// </summary>
    public void Init(DropItemDefinition definition, PlayerDropManager manager)
    {
        if (definition == null || manager == null)
            return;

        dropType = definition.DropType;
        targetManager = manager;
        targetScale = transform.localScale;
        transform.localScale = targetScale * spawnScale;
        currentSequence = RunSequence();
    }

    private Sequence RunSequence()
    {
        var seq = Sequence.Create()
            .Chain(Tween.Scale(transform, targetScale, dropAnimDuration * 0.5f, Ease.OutBack));

        if (bounceHeight > 0f)
        {
            Vector3 start = transform.position;
            Vector3 up = Vector3.up;
            seq.Chain(Tween.Position(transform, start + up * bounceHeight, dropAnimDuration * 0.25f, Ease.OutQuad))
              .Chain(Tween.Position(transform, start, dropAnimDuration * 0.25f, Ease.InQuad));
        }
        else
        {
            seq.ChainDelay(dropAnimDuration * 0.5f);
        }

        seq.ChainCallback(StartFlyingTowardsMask);
        return seq;
    }

    private void StartFlyingTowardsMask()
    {
        currentSequence = default;
        if (targetManager != null)
        {
            flyStartPosition = transform.position;
            flyTotalDistance = Vector3.Distance(flyStartPosition, targetManager.FlyToPosition);
            flyProgress = 0f;
            if (flyTotalDistance <= attachDistance)
            {
                transform.position = targetManager.FlyToPosition;
                OnReachedMask();
            }
            else
            {
                isFlyingTowardsMask = true;
            }
        }
        else
        {
            OnReachedMask();
        }
    }

    private void Update()
    {
        if (!isFlyingTowardsMask || targetManager == null)
            return;

        Vector3 target = targetManager.FlyToPosition;

        // Ease-out near target: speed scales down so it glides in softly (ghost-like)
        float distanceToTarget = flyTotalDistance * (1f - flyProgress);
        float speedMultiplier = 1f;
        if (slowDownDistance > 0f && distanceToTarget < slowDownDistance)
            speedMultiplier = Mathf.Clamp01(distanceToTarget / slowDownDistance);

        float step = (flySpeed * speedMultiplier * Time.deltaTime) / Mathf.Max(flyTotalDistance, 0.001f);
        flyProgress = Mathf.Clamp01(flyProgress + step);

        if (flyProgress >= 1f)
        {
            isFlyingTowardsMask = false;
            transform.position = target;
            OnReachedMask();
            return;
        }

        // Position on straight line from start to target
        Vector3 linearPosition = Vector3.Lerp(flyStartPosition, target, flyProgress);
        // Parabolic arc height: 0 at start and end, peak at progress 0.5 (ready for trail renderer later)
        float arcFactor = 4f * arcHeight * flyProgress * (1f - flyProgress);
        transform.position = linearPosition + Vector3.up * arcFactor;
    }

    /// <summary>Called by mask placement after attaching. Smoothly rotates from arrival rotation to final. Then done.</summary>
    public void AnimateSettleRotation(Quaternion fromRotation, float duration)
    {
        Quaternion to = transform.rotation;
        transform.rotation = fromRotation;
        Tween.Rotation(transform, to, duration, settleRotationEase);
    }

    private void OnReachedMask()
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (targetManager != null)
            targetManager.AddDrop(this);
    }

    private void OnDestroy()
    {
        if (currentSequence.isAlive)
            currentSequence.Stop();
    }
}
