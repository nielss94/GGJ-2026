using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug helper: fill the ultimate ability charge so you can test it without collecting drops.
/// Add to any GameObject (e.g. GameRoot). Call FillUltimate() from a UI button, or set a key to trigger it.
/// </summary>
public class FillUltimateDebug : MonoBehaviour
{
    [Tooltip("If true, pressing the trigger key will fill the ultimate charge.")]
    [SerializeField] private bool triggerKeyEnabled = true;
    [Tooltip("Key to press to fill ultimate (new Input System).")]
    [SerializeField] private Key triggerKey = Key.U;

    private void Update()
    {
        if (!triggerKeyEnabled) return;
        if (Keyboard.current != null && Keyboard.current[triggerKey].wasPressedThisFrame)
            FillUltimate();
    }

    /// <summary>Call from a UI button or elsewhere. Gives enough charge to use the ultimate once.</summary>
    public void FillUltimate()
    {
        var ability = FindFirstObjectByType<UltimateAbility>();
        if (ability != null)
            ability.DebugFillUltimateCharge();
    }
}
