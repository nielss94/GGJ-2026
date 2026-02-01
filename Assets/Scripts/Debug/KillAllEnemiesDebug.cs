using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug helper: kill all enemies (non-player Health) so you can quickly test level complete / doors.
/// Add to any GameObject (e.g. GameRoot). Call KillAllEnemies() from a UI button, or set a key to trigger it.
/// </summary>
public class KillAllEnemiesDebug : MonoBehaviour
{
    [Tooltip("If true, pressing the trigger key will kill all enemies.")]
    [SerializeField] private bool triggerKeyEnabled = true;
    [Tooltip("Key to press to kill all enemies (new Input System).")]
    [SerializeField] private Key triggerKey = Key.K;

    private void Update()
    {
        if (!triggerKeyEnabled) return;
        if (Keyboard.current != null && Keyboard.current[triggerKey].wasPressedThisFrame)
            KillAllEnemies();
    }

    /// <summary>Call from a UI button or elsewhere. Kills all non-player Health in loaded scenes.</summary>
    public void KillAllEnemies()
    {
        foreach (var health in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (health.IsPlayer) continue;
            health.TakeDamage(health.CurrentHealth);
        }
    }
}
