using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Draws a decal where the melee hit zone will be (telegraph = warning, attack window = hit active).
/// Uses MeleeAttack hit zone center/size so the decal matches the sphere/box overlap exactly.
/// If the visual has a DecalProjector, sets its size (width/height); otherwise sets transform scale.
/// Wire MeleeAttack events: OnTelegraphStarted -> ShowTelegraph, OnTelegraphEnded -> HideTelegraph,
/// OnAttackWindowOpened -> ShowAttackWindow, OnAttackWindowEnded -> HideAttackWindow.
/// </summary>
[RequireComponent(typeof(MeleeAttack))]
public class MeleeTelegraphDecal : MonoBehaviour
{
    [Header("Telegraph visual (warning phase)")]
    [SerializeField] private GameObject telegraphVisual;
    [SerializeField] private Renderer telegraphRenderer;

    [Header("Attack window visual (hitbox active)")]
    [SerializeField] private GameObject attackWindowVisual;
    [SerializeField] private Renderer attackWindowRenderer;

    [Header("Placement")]
    [Tooltip("Override decal size. Leave 0 to use MeleeAttack hit zone size (matches overlap).")]
    [SerializeField] private float decalSizeOverride = 0f;
    [Tooltip("Lay decal flat on ground (Y-up plane).")]
    [SerializeField] private bool flatOnGround = true;
    [Tooltip("Height above ground so decal sits on top (avoids z-fighting).")]
    [SerializeField] private float groundOffset = 0.02f;
    [Tooltip("Layers to raycast for ground. Leave Everything to use default.")]
    [SerializeField] private LayerMask groundLayers = -1;
    [Tooltip("DecalProjector projection depth (Z when flat). Only used when visual has a DecalProjector.")]
    [SerializeField] private float decalProjectionDepth = 1f;

    private MeleeAttack meleeAttack;

    private void Awake()
    {
        meleeAttack = GetComponent<MeleeAttack>();
    }

    private void Start()
    {
        HideTelegraph();
        HideAttackWindow();
    }

    /// <summary>Call from MeleeAttack.OnTelegraphStarted.</summary>
    public void ShowTelegraph()
    {
        PlaceVisual(telegraphVisual != null ? telegraphVisual.transform : (telegraphRenderer != null ? telegraphRenderer.transform : null));
        SetVisible(telegraphVisual, telegraphRenderer, true);
        SetVisible(attackWindowVisual, attackWindowRenderer, false);
    }

    /// <summary>Call from MeleeAttack.OnTelegraphEnded.</summary>
    public void HideTelegraph()
    {
        SetVisible(telegraphVisual, telegraphRenderer, false);
    }

    /// <summary>Call from MeleeAttack.OnAttackWindowOpened.</summary>
    public void ShowAttackWindow()
    {
        PlaceVisual(attackWindowVisual != null ? attackWindowVisual.transform : (attackWindowRenderer != null ? attackWindowRenderer.transform : null));
        SetVisible(telegraphVisual, telegraphRenderer, false);
        SetVisible(attackWindowVisual, attackWindowRenderer, true);
    }

    /// <summary>Call from MeleeAttack.OnAttackWindowEnded.</summary>
    public void HideAttackWindow()
    {
        SetVisible(attackWindowVisual, attackWindowRenderer, false);
    }

    private void PlaceVisual(Transform visualTransform)
    {
        if (visualTransform == null || meleeAttack == null) return;

        Vector3 center = meleeAttack.HitZoneCenterWorld;
        Vector3 forward = meleeAttack.HitZoneForward;
        Vector2 size = decalSizeOverride > 0f
            ? new Vector2(decalSizeOverride, decalSizeOverride)
            : meleeAttack.HitZoneDecalSize;

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

        var decalProjector = visualTransform.GetComponent<DecalProjector>();
        if (decalProjector != null)
        {
            decalProjector.size = new Vector3(size.x, size.y, decalProjectionDepth);
        }
        else
        {
            Vector3 scale = visualTransform.localScale;
            scale.x = size.x;
            scale.z = size.y;
            scale.y = Mathf.Max(size.x, size.y);
            visualTransform.localScale = scale;
        }
    }

    private static void SetVisible(GameObject go, Renderer r, bool visible)
    {
        if (r != null)
            r.enabled = visible;
        else if (go != null)
            go.SetActive(visible);
    }
}
