using UnityEngine;

/// <summary>
/// Shows a decal/line for ranged telegraph (aim direction toward player). Updates every frame while visible.
/// Wire RangedAttack: OnTelegraphStarted -> ShowTelegraph, OnTelegraphEnded -> HideTelegraph.
/// Provide a LineRenderer for the aim line, or a GameObject with a Renderer (e.g. stretched quad).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class RangedTelegraphDecal : MonoBehaviour
{
    [Header("Telegraph visual")]
    [Tooltip("LineRenderer for aim line. If set, positions are updated each frame toward player.")]
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("Alternative: GameObject/Renderer to show (e.g. quad). Will be rotated toward player each frame.")]
    [SerializeField] private GameObject telegraphVisual;
    [SerializeField] private Renderer telegraphRenderer;

    [Header("Line / placement")]
    [Tooltip("Origin of the line. Leave empty to use RangedAttack fire point or this transform.")]
    [SerializeField] private Transform lineOrigin;
    [Tooltip("Length of the aim line when no target. With target, line goes to target position.")]
    [SerializeField] private float lineLength = 12f;

    private Enemy enemy;
    private bool telegraphVisible;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }
        HideTelegraph();
    }

    private void LateUpdate()
    {
        if (!telegraphVisible) return;

        Transform target = enemy != null ? enemy.PlayerTarget : null;
        Vector3 origin = lineOrigin != null ? lineOrigin.position : transform.position;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            Vector3 end = target != null ? target.position : (origin + transform.forward * lineLength);
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, end);
        }
        else if (telegraphVisual != null || telegraphRenderer != null)
        {
            Transform t = telegraphVisual != null ? telegraphVisual.transform : telegraphRenderer.transform;
            t.position = origin;
            Vector3 dir = target != null ? (target.position - origin).normalized : transform.forward;
            if (dir.sqrMagnitude > 0.01f)
                t.rotation = Quaternion.LookRotation(dir);
        }
    }

    /// <summary>Call from RangedAttack.OnTelegraphStarted.</summary>
    public void ShowTelegraph()
    {
        telegraphVisible = true;
        if (lineRenderer != null)
            lineRenderer.enabled = true;
        else
            SetVisible(telegraphVisual, telegraphRenderer, true);
    }

    /// <summary>Call from RangedAttack.OnTelegraphEnded.</summary>
    public void HideTelegraph()
    {
        telegraphVisible = false;
        if (lineRenderer != null)
            lineRenderer.enabled = false;
        else
            SetVisible(telegraphVisual, telegraphRenderer, false);
    }

    private static void SetVisible(GameObject go, Renderer r, bool visible)
    {
        if (r != null)
            r.enabled = visible;
        else if (go != null)
            go.SetActive(visible);
    }
}
