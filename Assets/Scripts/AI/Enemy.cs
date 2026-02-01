using UnityEngine;

/// <summary>
/// Core enemy component. Holds the shared player reference so movement and attack behaviours
/// don't each need to find the player. Add this when using composition (ChaseMovement + MeleeAttack, etc.).
/// Optional: add Health here for enemies that can take damage.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to auto-find GameObject with tag \"Player\".")]
    [SerializeField] private Transform playerTarget;

    /// <summary>Player transform to chase/attack. Null if player not found or not yet resolved.</summary>
    public Transform PlayerTarget => playerTarget;

    private void Start()
    {
        if (playerTarget == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }
    }
}
