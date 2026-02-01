using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sets the first child's Animator MoveSpeed parameter from this NavMeshAgent's velocity magnitude.
/// Add to the enemy root (same GameObject as NavMeshAgent). Animator must be on the first child.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAnimatorDriver : MonoBehaviour
{
    [Tooltip("Animator parameter name for movement speed (e.g. for blend trees).")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";

    private Animator _animator;
    private NavMeshAgent _agent;
    private int _moveSpeedHash;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (transform.childCount > 0)
        {
            var firstChild = transform.GetChild(0);
            _animator = firstChild.GetComponent<Animator>();
        }
        _moveSpeedHash = Animator.StringToHash(moveSpeedParam);
    }

    private void Update()
    {
        if (_animator == null || !_animator.isActiveAndEnabled) return;

        float speed = _agent != null && _agent.isOnNavMesh
            ? _agent.velocity.magnitude
            : 0f;
        _animator.SetFloat(_moveSpeedHash, speed);
    }
}
