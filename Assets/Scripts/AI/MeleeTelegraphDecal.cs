using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Draws two telegraph decals where the melee hit will be. During the channel both are visible:
/// decal 1 at full size, decal 2 scaling from small to full (wind-up). Then both disappear.
/// Size is set via HDRP DecalProjector width/height only. Requires DecalProjector on each visual.
/// Wire MeleeAttack: OnTelegraphStarted -> ShowTelegraph, OnTelegraphEnded -> HideTelegraph.
/// ShowAttackWindow / HideAttackWindow can still be wired but do nothing (no attack window visual).
/// </summary>
[RequireComponent(typeof(MeleeAttack))]
public class MeleeTelegraphDecal : MonoBehaviour
{
    [Header("Telegraph visuals (warning phase – both active during channel)")]
    [Tooltip("First decal: circle at full size for the whole channel.")]
    [SerializeField] private GameObject telegraphVisual;
    [SerializeField] private Renderer telegraphRenderer;
    [Tooltip("Second decal: scales from small to full over the channel (wind-up). Leave empty to skip.")]
    [SerializeField] private GameObject telegraphWindUpVisual;
    [SerializeField] private Renderer telegraphWindUpRenderer;
    [Tooltip("Wind-up decal starts at this fraction of full size (0 = very small).")]
    [SerializeField][Range(0f, 1f)] private float windUpStartScale = 0.05f;

    [Header("Placement")]
    [Tooltip("Override decal size. Leave 0 to use MeleeAttack hit zone size (matches overlap).")]
    [SerializeField] private float decalSizeOverride = 0f;
    [Tooltip("Lay decal flat on ground (Y-up plane).")]
    [SerializeField] private bool flatOnGround = true;
    [Tooltip("Height above ground so decal sits on top (avoids z-fighting).")]
    [SerializeField] private float groundOffset = 0.02f;
    [Tooltip("Layers to raycast for ground. Leave Everything to use default.")]
    [SerializeField] private LayerMask groundLayers = -1;
    [Tooltip("DecalProjector projection depth (Z when flat). Applied to DecalProjector.size.z.")]
    [SerializeField] private float decalProjectionDepth = 1f;

    private MeleeAttack meleeAttack;
    private Coroutine windUpCoroutine;

    private void Awake()
    {
        meleeAttack = GetComponent<MeleeAttack>();
    }

    private void Start()
    {
        ResetAllDecals();
    }

    private void OnDisable()
    {
        ResetAllDecals();
    }

    /// <summary>Call from MeleeAttack.OnTelegraphStarted. Both decals are shown for the full channel; decal 1 is full size, decal 2 scales from small to full.</summary>
    public void ShowTelegraph()
    {
        if (meleeAttack == null) return;

        Vector2 fullSize = GetDecalSize();
        float duration = meleeAttack.TelegraphDuration;

        Transform decal1 = telegraphVisual != null ? telegraphVisual.transform : (telegraphRenderer != null ? telegraphRenderer.transform : null);
        Transform decal2 = telegraphWindUpVisual != null ? telegraphWindUpVisual.transform : (telegraphWindUpRenderer != null ? telegraphWindUpRenderer.transform : null);

        if (decal1 != null)
        {
            PlaceVisual(decal1, fullSize);
            SetVisible(telegraphVisual, telegraphRenderer, true);
        }

        if (decal2 != null)
        {
            Vector2 startSize = fullSize * Mathf.Max(0.001f, windUpStartScale);
            PlaceVisual(decal2, startSize);
            SetVisible(telegraphWindUpVisual, telegraphWindUpRenderer, true);
            if (windUpCoroutine != null)
                StopCoroutine(windUpCoroutine);
            windUpCoroutine = StartCoroutine(AnimateWindUp(decal2, startSize, fullSize, duration));
        }
    }

    /// <summary>Call from MeleeAttack.OnTelegraphEnded. Both decals disappear and are reset for the next attack.</summary>
    public void HideTelegraph()
    {
        if (windUpCoroutine != null)
        {
            StopCoroutine(windUpCoroutine);
            windUpCoroutine = null;
        }

        SetVisible(telegraphVisual, telegraphRenderer, false);
        SetVisible(telegraphWindUpVisual, telegraphWindUpRenderer, false);

        ResetWindUpDecalSize();
    }

    /// <summary>Call from MeleeAttack.OnAttackWindowOpened. No attack window visual; just ensures telegraph decals stay hidden.</summary>
    public void ShowAttackWindow()
    {
        SetVisible(telegraphVisual, telegraphRenderer, false);
        SetVisible(telegraphWindUpVisual, telegraphWindUpRenderer, false);
    }

    /// <summary>Call from MeleeAttack.OnAttackWindowEnded. No-op when there is no attack window visual.</summary>
    public void HideAttackWindow()
    {
    }

    private void ResetAllDecals()
    {
        if (windUpCoroutine != null)
        {
            StopCoroutine(windUpCoroutine);
            windUpCoroutine = null;
        }
        SetVisible(telegraphVisual, telegraphRenderer, false);
        SetVisible(telegraphWindUpVisual, telegraphWindUpRenderer, false);
        ResetWindUpDecalSize();
    }

    private void ResetWindUpDecalSize()
    {
        Transform windUpTransform = telegraphWindUpVisual != null ? telegraphWindUpVisual.transform : (telegraphWindUpRenderer != null ? telegraphWindUpRenderer.transform : null);
        if (windUpTransform == null) return;

        Vector2 fullSize = GetDecalSize();
        Vector2 startSize = fullSize * Mathf.Max(0.001f, windUpStartScale);
        SetDecalSize(windUpTransform, startSize);
    }

    private Vector2 GetDecalSize()
    {
        if (meleeAttack == null) return Vector2.one;
        return decalSizeOverride > 0f
            ? new Vector2(decalSizeOverride, decalSizeOverride)
            : meleeAttack.HitZoneDecalSize;
    }

    private void PlaceVisual(Transform visualTransform, Vector2 size)
    {
        if (visualTransform == null || meleeAttack == null) return;

        Vector3 center = meleeAttack.HitZoneCenterWorld;
        Vector3 forward = meleeAttack.HitZoneForward;

        Vector3 position = center;
        if (flatOnGround)
        {
            Vector3 rayOrigin = center + Vector3.up * 2f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 5f, groundLayers))
                position = hit.point + Vector3.up * groundOffset;
            else
                position = new Vector3(center.x, transform.position.y + groundOffset, center.z);
        }

        visualTransform.position = position;
        if (flatOnGround)
            visualTransform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(90f, 0f, 0f);
        else
            visualTransform.rotation = Quaternion.LookRotation(forward);

        SetDecalSize(visualTransform, size);
    }

    private void SetDecalSize(Transform visualTransform, Vector2 size)
    {
        if (visualTransform == null) return;

        var decalProjector = visualTransform.GetComponent<DecalProjector>();
        if (decalProjector == null) return;

        decalProjector.size = new Vector3(size.x, size.y, decalProjectionDepth);
    }

    private IEnumerator AnimateWindUp(Transform windUpTransform, Vector2 startSize, Vector2 endSize, float duration)
    {
        if (duration <= 0f)
        {
            SetDecalSize(windUpTransform, endSize);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector2 size = Vector2.Lerp(startSize, endSize, t);
            SetDecalSize(windUpTransform, size);
            yield return null;
        }

        SetDecalSize(windUpTransform, endSize);
        windUpCoroutine = null;
    }

    private static void SetVisible(GameObject go, Renderer r, bool visible)
    {
        if (r != null)
            r.enabled = visible;
        else if (go != null)
            go.SetActive(visible);
    }
}
