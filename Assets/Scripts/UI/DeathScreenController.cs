using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Listens for player death, shows the death/recap panel with run stats, and handles Return to Main Menu / Start New Run.
/// Keeps the death panel root enabled (so it can listen to PlayerDied) and enables/disables the container child to show/hide the UI.
/// </summary>
public class DeathScreenController : MonoBehaviour
{
    [Header("Container")]
    [Tooltip("Child of this object to enable when showing the death screen and disable when hiding. Keep death panel root enabled.")]
    [SerializeField] private GameObject container;

    [Header("Recap display")]
    [Tooltip("Single label showing run recap (levels, kills, time). Leave empty to skip recap text.")]
    [SerializeField] private TMP_Text recapText;

    [Header("Focus")]
    [Tooltip("Selectable to focus when the death screen is shown (e.g. Start New Run button).")]
    [SerializeField] private GameObject firstSelected;

    private void OnEnable()
    {
        EventBus.PlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        EventBus.PlayerDied -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        // Optional: play death animation and camera effect here or elsewhere, then show recap after a delay.
        ShowDeathScreenWithRecap();
    }

    /// <summary>
    /// Fills recap from RunStats, shows the death panel, and blocks player input.
    /// Call this when you want to show the recap (e.g. immediately on death or after a death animation).
    /// </summary>
    public void ShowDeathScreenWithRecap()
    {
        var recap = RunStats.Instance != null ? RunStats.Instance.GetRecap() : default;

        if (recapText != null)
            recapText.text = FormatRecap(recap);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowDeathScreen();

        EventBus.RaisePlayerInputBlockRequested(this);

        if (firstSelected != null)
            StartCoroutine(SelectNextFrame());
    }

    /// <summary>True if the death screen container is currently visible.</summary>
    public bool IsVisible => container != null && container.activeSelf;

    /// <summary>Called by UIManager to show or hide the death screen container. Enables/disables the container child.</summary>
    public void SetVisible(bool visible)
    {
        if (container != null)
            container.SetActive(visible);
    }

    private void SetContainerActive(bool active)
    {
        SetVisible(active);
    }

    private static string FormatRecap(RunRecap recap)
    {
        return $"Levels completed: {recap.LevelsCompleted}\nEnemies defeated: {recap.EnemiesKilled}\nTime survived: {recap.FormatTime()}";
    }

    private System.Collections.IEnumerator SelectNextFrame()
    {
        yield return null;
        if (firstSelected != null && firstSelected.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    /// <summary>Call from Return to Main Menu button. Unloads current level and shows main menu.</summary>
    public void OnReturnToMainMenuClicked()
    {
        EventBus.RaisePlayerInputUnblockRequested(this);
        if (UIManager.Instance != null)
            UIManager.Instance.HideDeathScreen();

        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.UnloadCurrentLevel(() =>
            {
                LevelProgressionManager.Instance.LoadMainMenuScene();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowMainMenu();
            });
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenu();
        }
    }

    /// <summary>Call from Start New Run button. Unloads level, resets stats and player health, loads first level.</summary>
    public void OnStartNewRunClicked()
    {
        EventBus.RaisePlayerInputUnblockRequested(this);
        if (UIManager.Instance != null)
            UIManager.Instance.HideDeathScreen();

        if (LevelProgressionManager.Instance == null) return;

        LevelProgressionManager.Instance.UnloadCurrentLevel(() =>
        {
            if (RunStats.Instance != null)
                RunStats.Instance.ResetRun();
            LevelProgressionManager.Instance.ResetPlayerForNewRun();
            LevelProgressionManager.Instance.LoadFirstLevel();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowGameHUD();
        });
    }
}
