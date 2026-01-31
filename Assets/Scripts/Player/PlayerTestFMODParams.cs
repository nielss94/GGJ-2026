using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTestFMODParams : MonoBehaviour
{
    [SerializeField] private StudioEventEmitter eventEmitter;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private UpgradeDatabase upgradeDatabase;

    private InputAction jumpAction;

    private void Awake()
    {
        if (inputActions != null)
        {
            jumpAction = inputActions.FindActionMap("Player").FindAction("Jump");
        }
    }

    private void Start()
    {
        if (eventEmitter != null)
        {
            var randomRarityFromDatabase = upgradeDatabase.GetRandomRarity();
            // eventEmitter.EventInstance.setParameterByID("rarity", randomRarityFromDatabase.DisplayName.ToLower());
        }
    }

    private void OnEnable()
    {
        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnSpacePressed;
        }
    }

    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= OnSpacePressed;
            jumpAction.Disable();
        }
    }

    private void OnSpacePressed(InputAction.CallbackContext context)
    {
        if (eventEmitter != null)
        {
            eventEmitter.Play();
        }
    }
}
