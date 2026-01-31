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

    private NavMeshAgent _agent;
    private Enemy _enemy;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemy = GetComponent<Enemy>();
    }

    private void Update()
    {
        Transform t = target != null ? target : (_enemy != null ? _enemy.PlayerTarget : null);
        if (t == null || !_agent.enabled || !_agent.isOnNavMesh)
            return;

        _agent.SetDestination(t.position);
    }
}
