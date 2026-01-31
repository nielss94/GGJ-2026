using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Maps Xbox face buttons (A, B, X, Y) to ability slots. Assign abilities in the inspector to control
/// which ability is triggered by each button; swap assignments to change bindings without touching input.
/// </summary>
public class PlayerAbilityManager : MonoBehaviour
{
    /// <summary>
    /// Xbox face button slots: A (South), B (East), X (West), Y (North).
    /// </summary>
    public enum AbilitySlot
    {
        A,
        B,
        X,
        Y
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Ability slots")]
    [Tooltip("If set, auto-assign finds abilities in this transform's children (e.g. an 'Abilities' object with one child per ability). Leave empty to search this GameObject only.")]
    [SerializeField] private Transform abilitiesContainer;
    [Tooltip("If true, fills empty slots with abilities using each ability's Preferred Slot. Searches abilitiesContainer's children if set, otherwise this GameObject.")]
    [SerializeField] private bool autoAssignFromSameObject = true;
    [Tooltip("Assign manually, or leave empty and use Auto-Assign. Use the circle picker to select the component (e.g. Dash Ability (Script)).")]
    [SerializeField] private PlayerAbility abilitySlotA;
    [SerializeField] private PlayerAbility abilitySlotB;
    [SerializeField] private PlayerAbility abilitySlotX;
    [SerializeField] private PlayerAbility abilitySlotY;

    private InputAction abilityActionA;
    private InputAction abilityActionB;
    private InputAction abilityActionX;
    private InputAction abilityActionY;

    private void Awake()
    {
        if (inputActions == null) return;

        var playerMap = inputActions.FindActionMap("Player");
        if (playerMap != null)
        {
            abilityActionA = playerMap.FindAction("AbilityA");
            abilityActionB = playerMap.FindAction("AbilityB");
            abilityActionX = playerMap.FindAction("AbilityX");
            abilityActionY = playerMap.FindAction("AbilityY");
        }

        AssignAbilitiesToEmptySlots();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && autoAssignFromSameObject)
        {
            AssignAbilitiesToEmptySlots();
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private void AssignAbilitiesToEmptySlots()
    {
        if (!autoAssignFromSameObject) return;

        PlayerAbility[] abilities = abilitiesContainer != null
            ? abilitiesContainer.GetComponentsInChildren<PlayerAbility>(true)
            : GetComponents<PlayerAbility>();
        foreach (var ability in abilities)
        {
            if (ability == null) continue;
            switch (ability.PreferredSlot)
            {
                case AbilitySlot.A when abilitySlotA == null: abilitySlotA = ability; break;
                case AbilitySlot.B when abilitySlotB == null: abilitySlotB = ability; break;
                case AbilitySlot.X when abilitySlotX == null: abilitySlotX = ability; break;
                case AbilitySlot.Y when abilitySlotY == null: abilitySlotY = ability; break;
            }
        }
    }

    private void OnEnable()
    {
        Subscribe(abilityActionA, abilitySlotA);
        Subscribe(abilityActionB, abilitySlotB);
        Subscribe(abilityActionX, abilitySlotX);
        Subscribe(abilityActionY, abilitySlotY);
    }

    private void OnDisable()
    {
        Unsubscribe(abilityActionA, abilitySlotA);
        Unsubscribe(abilityActionB, abilitySlotB);
        Unsubscribe(abilityActionX, abilitySlotX);
        Unsubscribe(abilityActionY, abilitySlotY);
    }

    private void Subscribe(InputAction action, PlayerAbility ability)
    {
        if (action != null)
        {
            action.Enable();
            action.performed += OnAbilityPerformed;
        }
    }

    private void Unsubscribe(InputAction action, PlayerAbility ability)
    {
        if (action != null)
        {
            action.performed -= OnAbilityPerformed;
            action.Disable();
        }
    }

    private void OnAbilityPerformed(InputAction.CallbackContext context)
    {
        if (PlayerInputBlocker.IsInputBlocked)
            return;

        PlayerAbility ability = null;
        if (context.action == abilityActionA) ability = abilitySlotA;
        else if (context.action == abilityActionB) ability = abilitySlotB;
        else if (context.action == abilityActionX) ability = abilitySlotX;
        else if (context.action == abilityActionY) ability = abilitySlotY;

        if (ability != null && ability.CanPerform)
        {
            ability.TryPerform();
        }
    }

    /// <summary>
    /// Assign an ability to a slot at runtime (e.g. for loadouts or upgrades).
    /// </summary>
    public void SetAbility(AbilitySlot slot, PlayerAbility ability)
    {
        switch (slot)
        {
            case AbilitySlot.A: abilitySlotA = ability; break;
            case AbilitySlot.B: abilitySlotB = ability; break;
            case AbilitySlot.X: abilitySlotX = ability; break;
            case AbilitySlot.Y: abilitySlotY = ability; break;
        }
    }

    /// <summary>
    /// Get the ability currently assigned to a slot.
    /// </summary>
    public PlayerAbility GetAbility(AbilitySlot slot)
    {
        return slot switch
        {
            AbilitySlot.A => abilitySlotA,
            AbilitySlot.B => abilitySlotB,
            AbilitySlot.X => abilitySlotX,
            AbilitySlot.Y => abilitySlotY,
            _ => null
        };
    }
}
