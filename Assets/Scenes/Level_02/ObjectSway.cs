using UnityEngine;

public class ObjectSway : MonoBehaviour
{
    // -------------------------
    // Sway Settings
    // -------------------------
    [Header("Sway Settings")]
    [Tooltip("Maximum rotation sway in degrees from the starting rotation.")]
    public float swayAmount = 0.5f;

    [Tooltip("How fast the object wobbles.")]
    public float swaySpeed = 2.2f;

    [Tooltip("Adds asymmetry and variation to motion.")]
    public float swayChaos = 0.3f;

    private Quaternion _startRot;
    private float _seedX;
    private float _seedY;
    private float _seedZ;

    void Awake()
    {
        _startRot = transform.localRotation;

        _seedX = Random.Range(0f, 100f);
        _seedY = Random.Range(0f, 100f);
        _seedZ = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = Time.time;

        float rx = (Mathf.PerlinNoise(_seedX, time * swaySpeed) - 0.5f) * swayAmount;
        float ry = (Mathf.PerlinNoise(_seedY, time * swaySpeed * (1f + swayChaos)) - 0.5f) * swayAmount;
        float rz = (Mathf.PerlinNoise(_seedZ, time * swaySpeed * (1f + swayChaos * 0.5f)) - 0.5f) * swayAmount;

        Quaternion swayRot = Quaternion.Euler(rx, ry, rz);
        transform.localRotation = _startRot * swayRot;
    }
}
