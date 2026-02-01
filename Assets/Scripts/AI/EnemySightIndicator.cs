using UnityEngine;

/// <summary>
/// Optional: shows a decal or line when the enemy has line of sight to the player (e.g. thin line from enemy to player).
/// Updates every frame. Use a LineRenderer or a stretched quad; assign Sight Origin to match EnemySight for consistency.
/// </summary>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(EnemySight))]
public class EnemySightIndicator : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("LineRenderer for sight line. If set, positions are updated each frame when LOS is true.")]
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("Alternative: GameObject/Renderer to show when enemy has LOS.")]
    [SerializeField] private GameObject sightVisual;
    [SerializeField] private Renderer sightRenderer;

    [Header("Origin & target")]
    [Tooltip("Start of the line (e.g. enemy eyes). Leave empty to use this transform. Should match EnemySight origin.")]
    [SerializeField] private Transform sightOrigin;

    private Enemy enemy;
    private EnemySight sight;
    private bool hadLos;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        sight = GetComponent<EnemySight>();
    }

    private void Start()
    {
        hadLos = false;
        if (lineRenderer != null)
            lineRenderer.positionCount = 2;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        Transform target = enemy != null ? enemy.PlayerTarget : null;
        bool hasLos = target != null && sight != null && sight.HasLineOfSightTo(target);

        if (!hasLos)
        {
            if (hadLos)
                SetVisible(false);
            hadLos = false;
            return;
        }

        hadLos = true;
        SetVisible(true);

        Vector3 origin = sightOrigin != null ? sightOrigin.position : transform.position;
        Vector3 end = target.position;

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, end);
        }
        else if (sightVisual != null || sightRenderer != null)
        {
            Transform t = sightVisual != null ? sightVisual.transform : sightRenderer.transform;
            t.position = (origin + end) * 0.5f;
            Vector3 dir = (end - origin).normalized;
            if (dir.sqrMagnitude > 0.01f)
                t.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void SetVisible(bool visible)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = visible;
        else if (sightRenderer != null)
            sightRenderer.enabled = visible;
        else if (sightVisual != null)
            sightVisual.SetActive(visible);
    }
}
