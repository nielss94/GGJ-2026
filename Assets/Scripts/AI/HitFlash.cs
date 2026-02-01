using System.Collections;
using UnityEngine;

/// <summary>
/// Applies a brief white flash to renderers when this object's Health takes damage.
/// Add to enemies (with Health and Renderer(s)) for hit feedback. Uses MaterialPropertyBlock
/// so materials are not instantiated. HDRP Lit uses _BaseColor in shader; we set both
/// _BaseColor and _Color so it works with HDRP Lit, URP, and Built-in.
/// </summary>
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [Header("Flash")]
    [Tooltip("Duration of the flash in seconds.")]
    [SerializeField] private float flashDuration = 0.08f;
    [Tooltip("Primary shader color property. HDRP Lit: _BaseColor. Built-in: _Color. We also set the other so both pipelines work.")]
    [SerializeField] private string colorPropertyName = "_BaseColor";
    [Tooltip("Optional: specific renderers. If empty, uses GetComponentsInChildren<Renderer>.")]
    [SerializeField] private Renderer[] renderers;

    private Health health;
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock block;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private int colorPropertyId = -1;
    private bool flashing;

    private void Awake()
    {
        health = GetComponent<Health>();
        block = new MaterialPropertyBlock();
        if (!string.IsNullOrEmpty(colorPropertyName))
            colorPropertyId = Shader.PropertyToID(colorPropertyName);
        if (colorPropertyId < 0)
            colorPropertyId = BaseColorId;
        if (renderers != null && renderers.Length > 0)
            cachedRenderers = renderers;
        else
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
    }

    private void OnDamaged(DamageInfo info)
    {
        if (flashing || cachedRenderers == null || cachedRenderers.Length == 0) return;
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        flashing = true;
        foreach (Renderer r in cachedRenderers)
        {
            if (r == null || !r.enabled) continue;
            r.GetPropertyBlock(block);
            block.SetColor(colorPropertyId, Color.white);
            block.SetColor(BaseColorId, Color.white);
            block.SetColor(ColorId, Color.white);
            r.SetPropertyBlock(block);
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (Renderer r in cachedRenderers)
        {
            if (r == null) continue;
            r.SetPropertyBlock(null);
        }
        flashing = false;
    }
}
