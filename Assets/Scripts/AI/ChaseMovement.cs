using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Movement behaviour: chase the player using NavMeshAgent. Use with Enemy (for player ref) or assign target manually.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ChaseMovement : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to use Enemy.PlayerTarget on this GameObject.")]
    [SerializeField] private Transform target;

    private NavMeshAgent agent;
    private Enemy enemy;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
    }

    private void Update()
    {
        Transform t = target != null ? target : (enemy != null ? enemy.PlayerTarget : null);
        if (t == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        
        agent.SetDestination(t.position);
    }
}
