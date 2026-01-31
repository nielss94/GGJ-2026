using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple enemy: follows the player using a NavMeshAgent. For contact damage, add the ContactDamage component.
/// Requires: NavMeshAgent on this GameObject, and a NavMesh baked in the scene.
/// Assign the player Transform, or ensure the player GameObject has the "Player" tag.
/// For multiple enemy types, use composition: Enemy + ChaseMovement + ContactDamage (see AI folder).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to auto-find GameObject with tag \"Player\".")]
    [SerializeField] private Transform playerTarget;

    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
    }

    private void Update()
    {
        if (playerTarget == null || !_agent.enabled || !_agent.isOnNavMesh)
        {
            return;
        }

        _agent.SetDestination(playerTarget.position);
    }
}
