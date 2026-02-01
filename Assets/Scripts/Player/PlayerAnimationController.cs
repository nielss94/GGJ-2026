using UnityEngine;

/// <summary>
/// Drives the player Animator from PlayerMovement and PlayerAbilityManager.
/// Sets MoveSpeed (0 when idle, &gt; 0.1 when moving), Attack1/Attack2/Attack3 triggers for light combo, and IsDashing while dashing.
/// Must be on the same GameObject as the Animator so Animation Events (e.g. OnComboLinkReady) are received.
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    private const float MoveSpeedWhenMoving = 1f;
    private const float MoveInputThresholdSq = 0.01f * 0.01f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAbilityManager abilityManager;

    [Header("Animator parameters")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string isDashingParam = "IsDashing";
    [SerializeField] private string attack1Trigger = "Attack1";
    [SerializeField] private string attack2Trigger = "Attack2";
    [SerializeField] private string attack3Trigger = "Attack3";

    private DashAbility _dashAbility;
    private LightAttackAbility _lightAttackAbility;
    private int _moveSpeedHash;
    private int _isDashingHash;
    private int _attack1Hash;
    private int _attack2Hash;
    private int _attack3Hash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        _moveSpeedHash = Animator.StringToHash(moveSpeedParam);
        _isDashingHash = Animator.StringToHash(isDashingParam);
        _attack1Hash = Animator.StringToHash(attack1Trigger);
        _attack2Hash = Animator.StringToHash(attack2Trigger);
        _attack3Hash = Animator.StringToHash(attack3Trigger);

        if (abilityManager != null)
        {
            _dashAbility = abilityManager.GetAbility(PlayerAbilityManager.AbilitySlot.A) as DashAbility;
            _lightAttackAbility = abilityManager.GetAbility(PlayerAbilityManager.AbilitySlot.X) as LightAttackAbility;
        }
    }

    private void OnEnable()
    {
        if (_lightAttackAbility != null)
            _lightAttackAbility.OnSwingStarted += OnLightAttackSwingStarted;
    }

    private void OnDisable()
    {
        if (_lightAttackAbility != null)
            _lightAttackAbility.OnSwingStarted -= OnLightAttackSwingStarted;
    }

    private void Update()
    {
        if (animator == null) return;

        // MoveSpeed: 0 when not moving, > 0.1 when moving
        float moveSpeed = 0f;
        if (playerMovement != null && playerMovement.WorldMoveDirection.sqrMagnitude >= MoveInputThresholdSq)
            moveSpeed = MoveSpeedWhenMoving;
        animator.SetFloat(_moveSpeedHash, moveSpeed);

        // IsDashing: true while dashing
        bool isDashing = _dashAbility != null && _dashAbility.IsDashing;
        animator.SetBool(_isDashingHash, isDashing);
    }

    /// <summary>
    /// Call from Animation Event at the frame where the next attack can start.
    /// Add to each attack clip: function OnComboLinkReady, int parameter 0 (Attack1), 1 (Attack2), 2 (Attack3).
    /// Must be on the same GameObject as the Animator.
    /// </summary>
    public void OnComboLinkReady(int comboIndex)
    {
        _lightAttackAbility?.NotifySwingEndFromAnimation(comboIndex);
    }

    private void OnLightAttackSwingStarted(int comboIndex)
    {
        if (animator == null) return;

        switch (comboIndex)
        {
            case 0:
                animator.SetTrigger(_attack1Hash);
                break;
            case 1:
                animator.SetTrigger(_attack2Hash);
                break;
            case 2:
                animator.SetTrigger(_attack3Hash);
                break;
        }
    }
}
