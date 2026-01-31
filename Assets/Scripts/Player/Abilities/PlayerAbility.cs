using UnityEngine;

/// <summary>
/// Base class for player abilities. Add ability components to the player or to children of an "Abilities" object
/// (assign that object to PlayerAbilityManager's Abilities Container for auto-assign).
/// Use PlayerTransform and PlayerRigidbody to access the player; they resolve to the root/parent when the ability is on a child.
/// </summary>
public abstract class PlayerAbility : MonoBehaviour
{
    public int level = 1;
    [Header("Ability Info")]
    [SerializeField] protected string abilityName = "Ability";
    [SerializeField][TextArea(1, 3)] private string description = "";

    [Header("Slot (used when manager auto-assigns)")]
    [Tooltip("If PlayerAbilityManager has Auto-Assign enabled, this ability is assigned to this button slot.")]
    [SerializeField] protected PlayerAbilityManager.AbilitySlot preferredSlot = PlayerAbilityManager.AbilitySlot.A;

    private Rigidbody _playerRigidbody;

    private void Start()
    {
        ApplyLevel();
    }

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
    /// Evaluates an animation curve at the current level. Level is clamped to the curve's key range,
    /// so if level exceeds the curve's max time, the value at the max time is used.
    /// </summary>
    /// <param name="curve">The curve to evaluate (level on X, value on Y).</param>
    /// <returns>Curve value at the clamped level, or 0 if curve is null or has no keys.</returns>
    protected float EvaluateCurveAtLevel(AnimationCurve curve)
    {
        if (curve == null || curve.keys.Length == 0)
            return 0f;

        float minTime = curve.keys[0].time;
        float maxTime = curve.keys[curve.keys.Length - 1].time;
        float clampedLevel = Mathf.Clamp(level, minTime, maxTime);
        return curve.Evaluate(clampedLevel);
    }

    /// <summary>
    /// Called when level changes. Override to apply animation curves (via EvaluateCurveAtLevel) to this ability's parameters.
    /// </summary>
    public virtual void ApplyLevel() { }

    /// <summary>
    /// Sets the ability level and reapplies level-based parameters (calls ApplyLevel).
    /// </summary>
    public void SetLevel(int newLevel)
    {
        level = newLevel;
        ApplyLevel();
    }

    /// <summary>
    /// Increases level by one and reapplies level-based parameters.
    /// </summary>
    public void LevelUp()
    {
        SetLevel(level + 1);
    }

    /// <summary>
    /// Called when the player triggers this ability (button pressed). Return true if the ability was performed.
    /// </summary>
    public abstract bool TryPerform();
}
