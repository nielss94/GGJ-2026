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
        float padding = 18f;
        float x = padding;
        float y = Screen.height - padding;

        if (showDebugGui)
        {
            string content = cachedGuiText ?? "";
            var lightAttack = FindFirstObjectByType<LightAttackAbility>();
            if (lightAttack != null)
                content = string.IsNullOrEmpty(content) ? lightAttack.GetDebugStatus() : content + "\n\n" + lightAttack.GetDebugStatus();
            if (string.IsNullOrEmpty(content))
                content = "No debug info";

            float width = 420f;
            int lineCount = 1;
            foreach (char c in content)
                if (c == '\n') lineCount++;
            int fontSize = 16;
            float lineHeight = Mathf.Max(GUI.skin.box.lineHeight * 1.5f, fontSize * 1.6f);
            float buttonHeight = 32f;
            float contentHeight = lineCount * lineHeight + padding * 2f;
            float height = contentHeight + buttonHeight + padding;
            y -= height;

            var oldLabelFontSize = GUI.skin.label.fontSize;
            var oldBoxFontSize = GUI.skin.box.fontSize;
            var oldButtonFontSize = GUI.skin.button.fontSize;
            GUI.skin.label.fontSize = fontSize;
            GUI.skin.box.fontSize = fontSize;
            GUI.skin.button.fontSize = fontSize;

            GUI.Box(new Rect(x, y, width, height), "");
            GUI.Label(new Rect(x + padding, y + padding, width - padding * 2, contentHeight - padding), content);
            if (GUI.Button(new Rect(x + padding, y + contentHeight, width - padding * 2, buttonHeight), "Hide"))
                showDebugGui = false;

            GUI.skin.label.fontSize = oldLabelFontSize;
            GUI.skin.box.fontSize = oldBoxFontSize;
            GUI.skin.button.fontSize = oldButtonFontSize;
        }
        else
        {
            float buttonHeight = 30f;
            float buttonWidth = 100f;
            y -= buttonHeight;
            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Debug"))
                showDebugGui = true;
        }
    }
}
