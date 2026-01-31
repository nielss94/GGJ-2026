using UnityEngine;

/// <summary>
/// Shared attack state for an enemy. When true, movement (e.g. ChaseMovement) should stop
/// so the enemy doesn't walk during the telegraph/channel. Set by ContactDamage and RangedAttack.
/// </summary>
public class EnemyAttackState : MonoBehaviour
{
    /// <summary>True while the enemy is telegraphing/channeling an attack (no movement).</summary>
    public bool IsChanneling { get; set; }
}
