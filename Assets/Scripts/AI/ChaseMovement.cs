using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Movement behaviour: chase the player using NavMeshAgent. Stops when in attack range (with line of sight)
/// or when telegraphing/channeling an attack. Use with Enemy, optional EnemySight and EnemyAttackState.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ChaseMovement : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to use Enemy.PlayerTarget on this GameObject.")]
    [SerializeField] private Transform target;

    [Header("Attack range")]
    [Tooltip("Stop moving when within this distance of the target and have LOS. 0 = never stop for range.")]
    [SerializeField] private float stopWhenInAttackRange = 0f;

    private NavMeshAgent agent;
    private Enemy enemy;
    private EnemySight sight;
    private EnemyAttackState attackState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
        sight = GetComponent<EnemySight>();
        attackState = GetComponent<EnemyAttackState>();
    }

    /// <summary>Set stop distance from attack range (e.g. from EnemyTypeApplier for melee or ranged).</summary>
    public void SetStopWhenInAttackRange(float range)
    {
        stopWhenInAttackRange = Mathf.Max(0f, range);
    }

    private void Update()
    {
        Transform t = target != null ? target : (enemy != null ? enemy.PlayerTarget : null);
        if (t == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        bool shouldStop = false;
        if (attackState != null && attackState.IsChanneling)
            shouldStop = true;
        else if (stopWhenInAttackRange > 0f)
        {
            float distSq = (t.position - transform.position).sqrMagnitude;
            bool inRange = distSq <= stopWhenInAttackRange * stopWhenInAttackRange;
            bool hasLos = sight == null || sight.HasLineOfSightTo(t);
            if (inRange && hasLos)
                shouldStop = true;
        }

        if (shouldStop)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(t.position);
        }
    }
}
