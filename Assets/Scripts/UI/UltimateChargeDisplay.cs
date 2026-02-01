using UnityEngine;
using TMPro;

/// <summary>
/// Shows the ultimate charge as text: current drops / required drops (e.g. "3/5").
/// Assign a TMP_Text; it updates from EventBus.GetUltimateCharge when the player has an UltimateAbility.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UltimateChargeDisplay : MonoBehaviour
{
    [Tooltip("Text to show current/required drops (e.g. \"3/5\").")]
    [SerializeField] private TMP_Text counterText;

    [Tooltip("Format string. {0} = current, {1} = required. Default \"{0}/{1}\".")]
    [SerializeField] private string format = "{0}/{1}";

    private void Update()
    {
        if (counterText == null) return;

        var getCharge = EventBus.GetUltimateCharge;
        if (getCharge != null)
        {
            var (current, required) = getCharge();
            counterText.text = string.Format(format, current, required);
        }
        else
        {
            counterText.text = string.Format(format, 0, 0);
        }
    }
}
