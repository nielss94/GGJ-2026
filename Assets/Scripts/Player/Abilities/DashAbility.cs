using UnityEngine;

/// <summary>
/// Dash ability (boilerplate). Preferred slot A; use PlayerRigidbody or PlayerTransform to move the player.
/// Implement dash logic in TryPerform() when ready.
/// </summary>
public class DashAbility : PlayerAbility
{
    [Header("Dash (level curves)")]
    [Tooltip("Distance per level (X = level, Y = distance). Evaluated at current level and added to dash distance each time level is applied. If level exceeds curve max time, the max value is used.")]
    [SerializeField] private AnimationCurve dashDistanceCurve;

    [Header("Dash (placeholder)")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;

    /// <summary>
    /// Current dash distance (initial value plus curve evaluation added each time level is applied).
    /// </summary>
    public float DashDistance => dashDistance;

    private void Reset()
    {
        preferredSlot = PlayerAbilityManager.AbilitySlot.A;
        abilityName = "Dash";
    }

    public override void ApplyLevel()
    {
        if (dashDistanceCurve != null && dashDistanceCurve.keys.Length > 0)
            dashDistance += EvaluateCurveAtLevel(dashDistanceCurve);
    }

    public override bool TryPerform()
    {
        if (!CanPerform) return false;

        Debug.Log("Dash performed");
        // TODO: Implement dash (e.g. move player forward, disable input briefly, add i-frames).
        return true;
    }
}
