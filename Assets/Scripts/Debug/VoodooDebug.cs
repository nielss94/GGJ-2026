using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug-only: opens the upgrade panel on U (keyboard) or LB (gamepad).
/// Add the VoodooDebug prefab to a scene to use. Assign the same Input Actions asset used by the player.
/// </summary>
public class VoodooDebug : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Debug controls")]
    [SerializeField]
    [TextArea(2, 8)]
    private string controlSummary = "Assign Input Actions to see bindings.";

    private InputAction openUpgradePanelAction;
    private string cachedGuiText;
    private bool showDebugGui = true;

    private void Awake()
    {
        if (inputActions != null)
        {
            var debugMap = inputActions.FindActionMap("Debug");
            if (debugMap != null)
            {
                openUpgradePanelAction = debugMap.FindAction("OpenUpgradePanel");
                cachedGuiText = BuildControlSummary(debugMap);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (inputActions != null)
        {
            var debugMap = inputActions.FindActionMap("Debug");
            if (debugMap != null)
                controlSummary = BuildControlSummary(debugMap);
        }
    }
#endif

    private static string BuildControlSummary(InputActionMap debugMap)
    {
        var sb = new StringBuilder();
        foreach (InputAction action in debugMap.actions)
        {
            sb.Append(action.name).Append(": ");
            var bindings = new List<string>();
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (string.IsNullOrEmpty(binding.path) || binding.path == "<Unknown>" || binding.isComposite)
                    continue;
                string display = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
                if (!string.IsNullOrEmpty(display))
                    bindings.Add(display);
            }
            sb.AppendLine(string.Join(", ", bindings));
        }
        return sb.Length > 0 ? sb.ToString().TrimEnd() : "No bindings.";
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            var debugMap = inputActions.FindActionMap("Debug");
            if (debugMap != null && string.IsNullOrEmpty(cachedGuiText))
                cachedGuiText = BuildControlSummary(debugMap);
        }
        if (openUpgradePanelAction != null)
        {
            openUpgradePanelAction.performed += OnOpenUpgradePanelPerformed;
            openUpgradePanelAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (openUpgradePanelAction != null)
        {
            openUpgradePanelAction.performed -= OnOpenUpgradePanelPerformed;
            openUpgradePanelAction.Disable();
        }
    }

    private void OnOpenUpgradePanelPerformed(InputAction.CallbackContext context)
    {
        if (UpgradePanel.Instance != null)
            UpgradePanel.Instance.Toggle();
    }

    private void OnGUI()
    {
        float padding = 10f;
        float x = padding;
        float y = Screen.height - padding;

        if (showDebugGui && !string.IsNullOrEmpty(cachedGuiText))
        {
            float width = 280f;
            int lineCount = 1;
            foreach (char c in cachedGuiText)
                if (c == '\n') lineCount++;
            float lineHeight = GUI.skin.box.lineHeight;
            float buttonHeight = 22f;
            float contentHeight = lineCount * lineHeight + padding * 2;
            float height = contentHeight + buttonHeight + padding;
            y -= height;
            GUI.Box(new Rect(x, y, width, height), "");
            GUI.Label(new Rect(x + padding, y + padding, width - padding * 2, contentHeight - padding), cachedGuiText);
            if (GUI.Button(new Rect(x + padding, y + contentHeight, width - padding * 2, buttonHeight), "Hide"))
                showDebugGui = false;
        }
        else
        {
            float buttonHeight = 22f;
            float buttonWidth = 80f;
            y -= buttonHeight;
            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Debug"))
                showDebugGui = true;
        }
    }
}
