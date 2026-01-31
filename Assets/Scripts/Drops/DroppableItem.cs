using UnityEngine;
using PrimeTween;

/// <summary>
/// Put on drop prefabs (any mesh, no colliders). After a short drop animation, flies towards the
/// assigned mask and then attaches via MaskAttachmentReceiver. Init() is called when spawned by EnemyDropper.
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
    [Tooltip("Duration of the flight towards the mask.")]
    [SerializeField] private float flyDuration = 0.5f;
    [SerializeField] private Ease flyEase = Ease.OutQuad;

    private DropTypeId dropType;
    private MaskAttachmentReceiver targetReceiver;
    private Sequence currentSequence;
    private Vector3 targetScale;

    /// <summary>Drop type for this item. Set by Init(); used by MaskAttachmentReceiver to choose strategy.</summary>
    public DropTypeId DropType => dropType;

    /// <summary>
    /// Call after instantiating the drop. Sets type and target; starts drop animation then flight to mask.
    /// </summary>
    public void Init(DropItemDefinition definition, MaskAttachmentReceiver receiver)
    {
        if (definition == null || receiver == null)
            return;

        dropType = definition.DropType;
        targetReceiver = receiver;
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

        seq.Chain(Tween.Position(transform, targetReceiver.FlyToPosition, flyDuration, flyEase))
          .ChainCallback(OnReachedMask);
        return seq;
    }

    private void OnReachedMask()
    {
        currentSequence = default;
        if (targetReceiver != null)
        {
            targetReceiver.Attach(this);
        }
    }

    private void OnDestroy()
    {
        if (currentSequence.isAlive)
            currentSequence.Stop();
    }
}
