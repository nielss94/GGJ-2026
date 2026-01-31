using UnityEngine;
using TMPro;

/// <summary>
/// Shows the player's current health as a number. Loosely coupled: subscribes to EventBus.PlayerHealthChanged
/// and optionally gets initial value from EventBus.GetPlayerHealth. No reference to the player required.
/// Assign a TextMeshPro - Text (UI) component to display the value.
/// </summary>
public class PlayerHealthDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private void OnEnable()
    {
        EventBus.PlayerHealthChanged += OnPlayerHealthChanged;
        var getHealth = EventBus.GetPlayerHealth;
        if (getHealth != null)
        {
            var (current, _) = getHealth();
            SetText(current);
        }
        else if (label != null)
            label.text = "—";
    }

    private void OnDisable()
    {
        EventBus.PlayerHealthChanged -= OnPlayerHealthChanged;
    }

    private void OnPlayerHealthChanged(float current, float max)
    {
        SetText(current);
    }

    private void SetText(float current)
    {
        if (label != null)
            label.text = Mathf.CeilToInt(current).ToString();
    }
}
