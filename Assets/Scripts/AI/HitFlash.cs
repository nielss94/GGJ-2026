using System.Collections;
using UnityEngine;

/// <summary>
/// Applies a brief emission flash to renderers when this object's Health takes damage.
/// Sets _EmissiveColor to white at the given intensity (nits), then back to black. Add to enemies
/// (with Health and Renderer(s)) for hit feedback. Uses MaterialPropertyBlock so materials
/// are not instantiated. HDRP Lit ignores _EmissiveIntensity at runtime; intensity must be
/// baked into _EmissiveColor. Ensure materials have Emission enabled (e.g. HDRP Lit emission checkbox).
/// </summary>
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [Header("Flash")]
    [Tooltip("Duration of the flash in seconds.")]
    [SerializeField] private float flashDuration = 0.08f;
    [Tooltip("Emission intensity in nits during the flash. Intensity is baked into _EmissiveColor (HDRP Lit uses only _EmissiveColor at runtime).")]
    [SerializeField] private float flashEmissionNits = 100f;
    [Tooltip("Optional: specific renderers. If empty, uses GetComponentsInChildren<Renderer>.")]
    [SerializeField] private Renderer[] renderers;

    private Health health;
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock block;
    private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
    private bool flashing;

    private void Awake()
    {
        health = GetComponent<Health>();
        block = new MaterialPropertyBlock();
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
        // HDRP Lit ignores _EmissiveIntensity at runtime; bake intensity into _EmissiveColor.
        Color flashColor = Color.white * flashEmissionNits;
        foreach (Renderer r in cachedRenderers)
        {
            if (r == null || !r.enabled) continue;
            r.GetPropertyBlock(block);
            block.SetColor(EmissiveColorId, flashColor);
            r.SetPropertyBlock(block);
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (Renderer r in cachedRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(EmissiveColorId, Color.black);
            r.SetPropertyBlock(block);
        }
        flashing = false;
    }
}
