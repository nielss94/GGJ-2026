using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Central UI controller in the BaseGame scene. Owns one EventSystem and Canvas; shows/hides panels
/// (main menu, pause, options, game HUD). Add to a GameObject in BaseGame and assign panel roots.
/// Ensures only one EventSystem is active when the scene loads.
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

        var eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        for (int i = 1; i < eventSystems.Length; i++)
            eventSystems[i].gameObject.SetActive(false);
    }

    private void Start()
    {
        if (showMainMenuOnStart && mainMenuPanel != null)
            ShowMainMenu();
        else if (gameHudPanel != null)
            ShowGameHUD();
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
            controller.SetVisible(active);
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            EventBus.RaisePlayerInputBlockRequested(this);
        }
    }

    public void HidePauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
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
            EventBus.RaisePlayerInputBlockRequested(this);
        else
            EventBus.RaisePlayerInputUnblockRequested(this);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
