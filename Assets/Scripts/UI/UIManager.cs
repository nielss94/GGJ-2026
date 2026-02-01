using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Central UI controller in the BaseGame scene. Owns one EventSystem and Canvas; shows/hides panels
/// (main menu, pause, options, game HUD). Add to a GameObject in BaseGame and assign panel roots.
/// Ensures only one EventSystem is active (on Awake, Start, and when new scenes load).
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels (assign roots to show/hide)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject gameHudPanel;
    [Tooltip("Death panel root (has DeathScreenController). Visibility is toggled via DeathScreenController.SetVisible.")]
    [SerializeField] private GameObject deathPanel;

    [Header("Event system")]
    [Tooltip("EventSystem to keep active (disables all others). Assign the one in BaseGame if it is not a child of this object.")]
    [SerializeField] private EventSystem eventSystemToKeep;

    [Header("Start state")]
    [Tooltip("If true, shows main menu panel on Start.")]
    [SerializeField] private bool showMainMenuOnStart = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DisableExtraEventSystems();
    }

    private void Start()
    {
        DisableExtraEventSystems();

        if (showMainMenuOnStart && mainMenuPanel != null)
            ShowMainMenu();
        else if (gameHudPanel != null)
            ShowGameHUD();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DisableExtraEventSystems();
    }

    private void DisableExtraEventSystems()
    {
        EventSystem keep = eventSystemToKeep != null ? eventSystemToKeep : GetComponentInChildren<EventSystem>(true);
        var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (keep == null && all.Length > 0)
            keep = all[0];
        foreach (EventSystem es in all)
        {
            if (es != keep)
                es.enabled = false;
        }
    }

    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(pausePanel, false);
        SetPanelActive(optionsPanel, false);
        SetPanelActive(gameHudPanel, false);
        SetDeathPanelActive(false);
    }

    public void ShowGameHUD()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(optionsPanel, false);
        SetPanelActive(gameHudPanel, true);
        SetDeathPanelActive(false);

        LevelProgressionManager.Instance?.PrepareDesignatedPlayerForGameplay();
    }

    /// <summary>Hides other panels and shows the death screen (via DeathScreenController.SetVisible).</summary>
    public void ShowDeathScreen()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(optionsPanel, false);
        SetPanelActive(gameHudPanel, false);
        SetDeathPanelActive(true);
    }

    /// <summary>Hides the death screen. Call when Return to Main Menu or Start New Run is clicked.</summary>
    public void HideDeathScreen()
    {
        SetDeathPanelActive(false);
    }

    private void SetDeathPanelActive(bool active)
    {
        if (deathPanel != null && deathPanel.TryGetComponent(out DeathScreenController controller))
        {
            if (controller.IsVisible != active)
            {
                if (active)
                    EventBus.RaiseGameplayPaused();
                else
                    EventBus.RaiseGameplayResumed();
            }
            controller.SetVisible(active);
        }
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            EventBus.RaiseGameplayPaused();
            EventBus.RaisePlayerInputBlockRequested(this);
        }
    }

    public void HidePauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            EventBus.RaiseGameplayResumed();
            EventBus.RaisePlayerInputUnblockRequested(this);
        }
    }

    public void ShowOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    public void HideOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    public void TogglePause()
    {
        if (pausePanel == null) return;
        bool willShow = !pausePanel.activeSelf;
        pausePanel.SetActive(willShow);
        if (willShow)
        {
            EventBus.RaiseGameplayPaused();
            EventBus.RaisePlayerInputBlockRequested(this);
        }
        else
        {
            EventBus.RaiseGameplayResumed();
            EventBus.RaisePlayerInputUnblockRequested(this);
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
