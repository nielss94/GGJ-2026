using UnityEngine;
using UnityEngine.Rendering;
using PrimeTween;
using TMPro;

/// <summary>
/// Single damage number instance on a world-space canvas. Call Initialize with position, damage, and crit;
/// animates (float up, fade, optional crit scale) then destroys itself.
/// Billboard Camera: which camera to face; unset = auto (Canvas.worldCamera or Camera.main).
/// Canvas Group: if set, fade uses CanvasGroup.alpha; otherwise fades TMP_Text color.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DamageNumberPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text text;
    [Tooltip("If set, fade-out uses CanvasGroup.alpha for the whole popup; otherwise fades text color.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Camera to face (billboard). If unset, uses Canvas.worldCamera or Camera.main.")]
    [SerializeField] private Camera billboardCamera;

    [Header("Animation")]
    [Tooltip("World-space height to float up during the popup.")]
    [SerializeField] private float floatHeight = 1.5f;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    [SerializeField] private Ease fadeEase = Ease.InQuad;

    [Header("Normal hit")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float normalScale = 1f;
    [Tooltip("HDR intensity multiplier for the face on TextMeshPro/Mobile/Distance Field. Values > 1 bloom in HDR.")]
    [SerializeField] [Min(0f)] private float faceHdrIntensity = 1f;

    [Header("Crit")]
    [SerializeField] private Color critColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private float critScale = 1.4f;
    [Tooltip("Optional: small scale punch at start for crits.")]
    [SerializeField] private float critPunchDuration = 0.15f;
    [SerializeField] private Ease critPunchEase = Ease.OutBack;

    private RectTransform _rectTransform;
    private Sequence _sequence;

    private void LateUpdate()
    {
        Camera cam = billboardCamera != null ? billboardCamera : (GetComponentInParent<Canvas>()?.worldCamera ?? Camera.main);
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (text == null) text = GetComponentInChildren<TMP_Text>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Initialize and start the popup animation. Call once after spawning.
    /// </summary>
    /// <param name="worldPosition">Position in world space (canvas is world space; local position will be set from this).</param>
    /// <param name="damage">Damage value to display (e.g. rounded integer).</param>
    /// <param name="isCrit">If true, uses crit color/scale and optional punch.</param>
    public void Initialize(Vector3 worldPosition, float damage, bool isCrit)
    {
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        if (text == null) text = GetComponentInChildren<TMP_Text>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        _rectTransform.position = worldPosition;

        int displayValue = Mathf.RoundToInt(damage);
        if (text != null)
        {
            text.text = displayValue.ToString();
            text.color = isCrit ? critColor : normalColor;
            SetRenderOnTop(text);
            SetFaceHdrIntensity(text, faceHdrIntensity);
        }

        float scale = isCrit ? critScale : normalScale;
        _rectTransform.localScale = Vector3.one * scale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        Vector3 endPos = worldPosition + Vector3.up * floatHeight;

        _sequence = Sequence.Create();

        if (isCrit && critPunchDuration > 0f)
        {
            float punchScale = scale * 1.2f;
            _sequence.Chain(Tween.Scale(transform, Vector3.one * punchScale, critPunchDuration, critPunchEase))
                .Chain(Tween.Scale(transform, Vector3.one * scale, duration * 0.2f, Ease.OutQuad));
        }

        _sequence.Group(Tween.Position(transform, endPos, duration, moveEase));

        if (canvasGroup != null)
            _sequence.Group(Tween.Alpha(canvasGroup, 0f, duration, fadeEase));
        else if (text != null)
            _sequence.Group(Tween.Custom(text.color.a, 0f, duration, t => { var c = text.color; c.a = t; text.color = c; }));

        _sequence.ChainCallback(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        _sequence.Stop();
    }

    /// <summary>Makes the graphic render on top (ignore depth). Uses unity_GUIZTestMode = Always when supported.</summary>
    private static void SetRenderOnTop(TMP_Text tmp)
    {
        if (tmp == null) return;
        Material mat = tmp.materialForRendering;
        if (mat != null && mat.HasProperty("unity_GUIZTestMode"))
            mat.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
    }

    /// <summary>Sets HDR intensity on the face color for TextMeshPro/Mobile/Distance Field by scaling _FaceColor RGB.</summary>
    private static void SetFaceHdrIntensity(TMP_Text tmp, float intensity)
    {
        if (tmp == null || intensity <= 0f) return;
        Material mat = tmp.fontMaterial;
        if (mat == null || !mat.HasProperty("_FaceColor")) return;
        Color face = mat.GetColor("_FaceColor");
        face.r *= intensity;
        face.g *= intensity;
        face.b *= intensity;
        mat.SetColor("_FaceColor", face);
    }
}
