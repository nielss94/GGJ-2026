using UnityEngine;

[RequireComponent(typeof(Light))]
public class OilLampFlicker : MonoBehaviour
{
    // -------------------------
    // Flicker Settings
    // -------------------------
    [Header("Flicker Settings")]
    [Tooltip("How fast the light flickers.")]
    public float flickerSpeed = 2.0f;

    [Tooltip("How strong the flicker variation is (0 = none).")]
    [Range(0f, 1f)]
    public float flickerAmount = 0.25f;

    [Tooltip("Additional chaotic jitter on top of smooth flicker.")]
    [Range(0f, 0.5f)]
    public float randomJitter = 0.05f;


    // -------------------------
    // Sway Settings
    // -------------------------
    [Header("Flame Sway Settings")]
    [Tooltip("Maximum movement distance from the starting position.")]
    public float swayAmount = 0.00005f;

    [Tooltip("How fast the flame wobbles.")]
    public float swaySpeed = 2.2f;

    [Tooltip("Adds asymmetry and variation to motion.")]
    public float swayChaos = 0.3f;


    private Light _light;
    private float _baseIntensity;
    private float _noiseOffset;

    private Vector3 _startPos;
    private float _seedX;
    private float _seedY;


    void Awake()
    {
        _light = GetComponent<Light>();

        // Light flicker setup
        _baseIntensity = _light.intensity;
        _noiseOffset = Random.Range(0f, 100f);

        // Sway setup
        _startPos = transform.localPosition;
        _seedX = Random.Range(0f, 100f);
        _seedY = Random.Range(0f, 100f);
    }


    void Update()
    {
        float time = Time.time;

        // 1. Smooth flicker (Perlin noise)
        float flickerNoise = Mathf.PerlinNoise(_noiseOffset, time * flickerSpeed);
        float flickerValue = 1f - (flickerNoise * flickerAmount);

        // Add tiny random jitter
        float jitter = (Random.value - 0.5f) * randomJitter;

        // Final intensity (never above base value)
        _light.intensity = Mathf.Max(
            0f,
            _baseIntensity * flickerValue + jitter
        );

        // 2. Flame sway movement
        float sx = (Mathf.PerlinNoise(_seedX, time * swaySpeed) - 0.5f) * swayAmount;
        float sy = (Mathf.PerlinNoise(_seedY, time * swaySpeed * (1f + swayChaos)) - 0.5f) * swayAmount;

        transform.localPosition = _startPos + new Vector3(sx, sy, 0f);
    }
}
