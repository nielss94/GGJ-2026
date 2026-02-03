using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// When the menu is visible and no button is selected, pressing WASD, arrows, or Submit
/// selects the first button so the user can navigate without the mouse.
/// Add to the same GameObject as the menu panel (or its controller) and assign menu root and firstSelected.
/// </summary>
public class MenuKeyboardNavigation : MonoBehaviour
{
    [Tooltip("Panel/container that must be active for this menu. When active and nothing is selected, nav keys will select firstSelected.")]
    [SerializeField] private GameObject menuRoot;

    [Tooltip("Selectable to focus when the user presses a navigation key (WASD/arrows) or Submit with nothing selected.")]
    [SerializeField] private GameObject firstSelected;

    private void Update()
    {
        if (menuRoot == null || !menuRoot.activeInHierarchy || firstSelected == null || !firstSelected.activeInHierarchy)
            return;

        var eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        GameObject current = eventSystem.currentSelectedGameObject;
        bool selectionInMenu = current != null && IsDescendantOf(current.transform, menuRoot.transform);

        if (selectionInMenu)
            return;

        if (!WasNavigationOrSubmitPressed())
            return;

        eventSystem.SetSelectedGameObject(firstSelected);
    }

    private static bool IsDescendantOf(Transform t, Transform ancestor)
    {
        while (t != null)
        {
            if (t == ancestor) return true;
            t = t.parent;
        }
        return false;
    }

    /// <summary>Use from other scripts (e.g. DeathScreenController, MainMenu) when integrating nav without this component.</summary>
    public static bool WasNavigationOrSubmitPressed()
    {
        var k = Keyboard.current;
        if (k == null) return false;

        return k.wKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame
            || k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame
            || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
    }

    /// <summary>True if current EventSystem selection is a descendant of menuRoot.</summary>
    public static bool IsSelectionInMenu(Transform menuRoot)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null) return false;
        return IsDescendantOf(eventSystem.currentSelectedGameObject.transform, menuRoot);
    }
}
