using UnityEngine;

/// <summary>
/// Base class for player abilities. Add ability components to the player or to children of an "Abilities" object
/// (assign that object to PlayerAbilityManager's Abilities Container for auto-assign).
/// Use PlayerTransform and PlayerRigidbody to access the player; they resolve to the root/parent when the ability is on a child.
/// </summary>
public abstract class PlayerAbility : MonoBehaviour
{
    [Header("Ability Info")]
    [SerializeField] protected string abilityName = "Ability";
    [SerializeField][TextArea(1, 3)] private string description = "";

    [Header("Slot (used when manager auto-assigns)")]
    [Tooltip("If PlayerAbilityManager has Auto-Assign enabled, this ability is assigned to this button slot.")]
    [SerializeField] protected PlayerAbilityManager.AbilitySlot preferredSlot = PlayerAbilityManager.AbilitySlot.A;

    private Rigidbody _playerRigidbody;

    /// <summary>
    /// Display name for UI or debugging.
    /// </summary>
    public string AbilityName => abilityName;

    /// <summary>
    /// Slot used when the manager auto-assigns abilities from the same GameObject.
    /// </summary>
    public PlayerAbilityManager.AbilitySlot PreferredSlot => preferredSlot;

    /// <summary>
    /// Whether the ability can be used right now (e.g. not on cooldown, not disabled).
    /// </summary>
    public virtual bool CanPerform => true;

    /// <summary>
    /// Transform of the player (this GameObject if ability is on the player, otherwise the parent with Rigidbody or root). Use for position/movement.
    /// </summary>
    protected Transform PlayerTransform => PlayerRigidbody != null ? PlayerRigidbody.transform : transform.root;

    /// <summary>
    /// Cached Rigidbody on the player (this GameObject or parent). Use for movement abilities (e.g. dash). May be null if player uses CharacterController.
    /// </summary>
    protected Rigidbody PlayerRigidbody => _playerRigidbody != null ? _playerRigidbody : _playerRigidbody = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();

    /// <summary>
    /// Called when the player triggers this ability (button pressed). Return true if the ability was performed.
    /// </summary>
    public abstract bool TryPerform();
}
