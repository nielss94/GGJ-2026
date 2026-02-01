using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player's health as a bar. Current health updates immediately; when you take damage,
/// a "chunk" (damage bar) appears and gradually drains down to match current health.
/// Setup: Both bars use Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left.
/// Order: background (optional) → damage fill → current health fill (on top).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PlayerHealthDisplay : MonoBehaviour
{
    [Header("Bar fills")]
    [Tooltip("Image with Type = Filled, Fill Method = Horizontal, Fill Origin = Left. Shows current health.")]
    [SerializeField] private Image currentHealthFill;
    [Tooltip("Image with Type = Filled, Fill Method = Horizontal, Fill Origin = Left. Shows damage/loss chunk. Place under current fill.")]
    [SerializeField] private Image lostHealthFill;

    [Header("Drain")]
    [Tooltip("Approximate time in seconds for the loss bar to catch up to current health. Higher = slower, smoother.")]
    [SerializeField] private float drainSmoothTime = 1.2f;

    [Header("Optional colors")]
    [SerializeField] private bool overrideColors;
    [SerializeField] private Color currentColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color lostColor = new Color(0.8f, 0.15f, 0.15f, 0.9f);

    private float _currentHealth;
    private float _maxHealth = 1f;
    private float _trailingHealth; // top of the damage chunk; drains toward _currentHealth
    private float _drainVelocity;   // for SmoothDamp

    private void Awake()
    {
        if (currentHealthFill != null)
        {
            currentHealthFill.type = Image.Type.Filled;
            currentHealthFill.fillMethod = Image.FillMethod.Horizontal;
            currentHealthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        if (lostHealthFill != null)
        {
            lostHealthFill.type = Image.Type.Filled;
            lostHealthFill.fillMethod = Image.FillMethod.Horizontal;
            lostHealthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    private void OnEnable()
    {
        EventBus.PlayerHealthChanged += OnPlayerHealthChanged;
        var getHealth = EventBus.GetPlayerHealth;
        if (getHealth != null)
        {
            var (current, max) = getHealth();
            _currentHealth = current;
            _maxHealth = max > 0f ? max : 1f;
            _trailingHealth = _currentHealth;
            SetBarImmediate(_currentHealth, _maxHealth);
        }
        else
            SetBarImmediate(0f, 1f);
    }

    private void OnDisable()
    {
        EventBus.PlayerHealthChanged -= OnPlayerHealthChanged;
    }

    private void OnPlayerHealthChanged(float current, float max)
    {
        float prev = _currentHealth;
        _currentHealth = current;
        _maxHealth = max > 0f ? max : 1f;

        // On damage: trailing stays at previous value so the chunk appears and drains
        if (current < prev)
            _trailingHealth = prev;
        // On heal: snap trailing so we don't show a red chunk
        else if (current > prev)
            _trailingHealth = current;

        SetBarImmediate(_currentHealth, _maxHealth);
    }

    private void Update()
    {
        if (_maxHealth <= 0f) return;

        // Smoothly drain the loss bar toward current health (ease-out)
        float currentNorm = Mathf.Clamp01(_currentHealth / _maxHealth);
        float trailingNorm = Mathf.Clamp01(_trailingHealth / _maxHealth);
        float smoothTime = Mathf.Max(0.01f, drainSmoothTime);
        trailingNorm = Mathf.SmoothDamp(trailingNorm, currentNorm, ref _drainVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        _trailingHealth = trailingNorm * _maxHealth;
        UpdateDamageChunk();
    }

    private void SetBarImmediate(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (currentHealthFill != null)
        {
            currentHealthFill.fillAmount = ratio;
            if (overrideColors)
                currentHealthFill.color = currentColor;
        }

        UpdateDamageChunk();

        if (overrideColors && lostHealthFill != null)
            lostHealthFill.color = lostColor;
    }

    private void UpdateDamageChunk()
    {
        if (lostHealthFill == null || _maxHealth <= 0f) return;

        float currentNorm = Mathf.Clamp01(_currentHealth / _maxHealth);
        float trailingNorm = Mathf.Clamp01(_trailingHealth / _maxHealth);

        lostHealthFill.fillAmount = trailingNorm;
        lostHealthFill.gameObject.SetActive(trailingNorm > currentNorm + 0.001f);
    }
}
