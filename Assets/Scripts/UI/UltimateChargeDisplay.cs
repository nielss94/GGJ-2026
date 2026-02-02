using UnityEngine;
using TMPro;

/// <summary>
/// Shows the ultimate charge as text: current drops / required drops (e.g. "3/5").
/// Binds to the designated player's UltimateAbility (from LevelProgressionManager or optional override).
/// Assign a TMP_Text; no EventBus provider required.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UltimateChargeDisplay : MonoBehaviour
{
    [Tooltip("Text to show current/required drops (e.g. \"3/5\").")]
    [SerializeField] private TMP_Text counterText;

    [Tooltip("Format string. {0} = current, {1} = required. Default \"{0}/{1}\".")]
    [SerializeField] private string format = "{0}/{1}";

    [Tooltip("Player to read ultimate charge from. If unset, uses LevelProgressionManager.DesignatedPlayer.")]
    [SerializeField] private Transform playerSource;

    [Tooltip("Shown when no designated player or no UltimateAbility (e.g. level has no player yet). Leave empty to hide.")]
    [SerializeField] private string noChargePlaceholder = "-/-";

    private void Update()
    {
        if (counterText == null) return;

        var ability = ResolveUltimateAbility();
        if (ability != null)
        {
            var (current, required) = ability.GetCharge();
            counterText.text = string.Format(format, current, required);
        }
        else
        {
            counterText.text = noChargePlaceholder;
        }
    }

    private UltimateAbility ResolveUltimateAbility()
    {
        Transform player = playerSource != null ? playerSource : LevelProgressionManager.Instance?.DesignatedPlayer;
        if (player == null) return null;
        return player.GetComponentInChildren<UltimateAbility>(true);
    }
}
