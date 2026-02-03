using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Main menu panel in BaseGame. Start Game calls UIManager to show the game HUD and loads the first level.
/// Focuses the EventSystem on firstSelected when the main menu is shown (OnEnable).
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("Selectable to focus when the main menu is shown (e.g. Start Game button).")]
    [SerializeField] private GameObject firstSelected;

    private void OnEnable()
    {
        if (firstSelected != null)
            StartCoroutine(SelectNextFrame());
    }

    private IEnumerator SelectNextFrame()
    {
        yield return null;
        if (firstSelected != null && firstSelected.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || firstSelected == null || !firstSelected.activeInHierarchy)
            return;
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return;
        if (MenuKeyboardNavigation.IsSelectionInMenu(transform))
            return;
        if (!MenuKeyboardNavigation.WasNavigationOrSubmitPressed())
            return;
        eventSystem.SetSelectedGameObject(firstSelected);
    }

    public void OnStartGameClicked()
    {
        if (RunStats.Instance != null)
            RunStats.Instance.ResetRun();
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.ResetPlayerForNewRun();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameHUD();
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.LoadFirstLevel();
    }
}
