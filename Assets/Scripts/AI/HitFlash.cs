using System.Collections;
using UnityEngine;

/// <summary>
/// Applies a brief emission flash to renderers when this object's Health takes damage.
/// Sets emission intensity (nits) to a configurable value, then back to 0. Add to enemies
/// (with Health and Renderer(s)) for hit feedback. Uses MaterialPropertyBlock so materials
/// are not instantiated. Requires materials to support _EmissiveIntensity (e.g. HDRP Lit with Use Emission Intensity).
/// </summary>
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [Header("Flash")]
    [Tooltip("Duration of the flash in seconds.")]
    [SerializeField] private float flashDuration = 0.08f;
    [Tooltip("Emission intensity in nits during the flash. Set back to 0 after duration.")]
    [SerializeField] private float flashEmissionNits = 100f;
    [Tooltip("Shader property name for emission intensity (float). HDRP Lit: _EmissiveIntensity.")]
    [SerializeField] private string emissionIntensityPropertyName = "_EmissiveIntensity";
    [Tooltip("Optional: specific renderers. If empty, uses GetComponentsInChildren<Renderer>.")]
    [SerializeField] private Renderer[] renderers;

    private Health health;
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock block;
    private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
    private int emissionIntensityPropertyId = -1;
    private bool flashing;

    private void Awake()
    {
        health = GetComponent<Health>();
        block = new MaterialPropertyBlock();
        if (!string.IsNullOrEmpty(emissionIntensityPropertyName))
            emissionIntensityPropertyId = Shader.PropertyToID(emissionIntensityPropertyName);
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
        if (emissionIntensityPropertyId < 0) return;
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        flashing = true;
        foreach (Renderer r in cachedRenderers)
        {
            if (r == null || !r.enabled) continue;
            r.GetPropertyBlock(block);
            block.SetFloat(emissionIntensityPropertyId, flashEmissionNits);
            block.SetColor(EmissiveColorId, Color.white);
            r.SetPropertyBlock(block);
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (Renderer r in cachedRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetFloat(emissionIntensityPropertyId, 0f);
            block.SetColor(EmissiveColorId, Color.black);
            r.SetPropertyBlock(block);
        }
        flashing = false;
    }
}
