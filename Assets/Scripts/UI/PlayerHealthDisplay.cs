using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player's health as a bar. Current health updates immediately; when you take damage,
/// a "chunk" (damage bar) appears and gradually drains down to match current health.
/// Setup: Current fill — Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left.
/// Damage chunk — Image (Simple, full sprite); script positions it via RectTransform anchors.
/// Order: background (optional) → damage chunk → current health fill (on top).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PlayerHealthDisplay : MonoBehaviour
{
    [Header("Bar fills")]
    [Tooltip("Image with Type = Filled, Fill Method = Horizontal, Fill Origin = Left. Shows current health.")]
    [SerializeField] private Image currentHealthFill;
    [Tooltip("Image (Simple) that shows the damage chunk; script positions it. Place under current fill.")]
    [SerializeField] private RectTransform damageChunkRect;

    [Header("Drain")]
    [Tooltip("How much health (in units) the damage chunk drains per second toward current health.")]
    [SerializeField] private float drainSpeed = 25f;

    [Header("Optional colors")]
    [SerializeField] private bool overrideColors;
    [SerializeField] private Color currentColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color damageChunkColor = new Color(0.8f, 0.15f, 0.15f, 0.9f);

    private float _currentHealth;
    private float _maxHealth = 1f;
    private float _trailingHealth; // top of the damage chunk; drains toward _currentHealth
    private Image _damageChunkImage;

    private void Awake()
    {
        if (damageChunkRect != null)
            _damageChunkImage = damageChunkRect.GetComponent<Image>();
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

        // Drain the damage chunk toward current health
        float step = drainSpeed * Time.deltaTime;
        _trailingHealth = Mathf.MoveTowards(_trailingHealth, _currentHealth, step);
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

        if (overrideColors && _damageChunkImage != null)
            _damageChunkImage.color = damageChunkColor;
    }

    private void UpdateDamageChunk()
    {
        if (damageChunkRect == null || _maxHealth <= 0f) return;

        float currentNorm = Mathf.Clamp01(_currentHealth / _maxHealth);
        float trailingNorm = Mathf.Clamp01(_trailingHealth / _maxHealth);

        // Position the damage chunk between current and trailing (empty if no chunk)
        float minX = currentNorm;
        float maxX = trailingNorm;

        damageChunkRect.anchorMin = new Vector2(minX, 0f);
        damageChunkRect.anchorMax = new Vector2(maxX, 1f);
        damageChunkRect.offsetMin = Vector2.zero;
        damageChunkRect.offsetMax = Vector2.zero;

        damageChunkRect.gameObject.SetActive(maxX > minX + 0.001f);
    }
}
