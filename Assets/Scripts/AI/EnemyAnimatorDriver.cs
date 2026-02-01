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
    [Tooltip("Animator trigger name fired when the enemy attacks (melee or ranged).")]
    [SerializeField] private string attackTriggerParam = "Attack";

    private Animator _animator;
    private NavMeshAgent _agent;
    private int _moveSpeedHash;
    private int _attackTriggerHash;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (transform.childCount > 0)
        {
            var firstChild = transform.GetChild(0);
            _animator = firstChild.GetComponent<Animator>();
        }
        _moveSpeedHash = Animator.StringToHash(moveSpeedParam);
        _attackTriggerHash = Animator.StringToHash(attackTriggerParam);
    }

    /// <summary>Sets the Attack trigger on the animator. Call from MeleeAttack/RangedAttack when the attack is released.</summary>
    public void SetAttackTrigger()
    {
        if (_animator != null && _animator.isActiveAndEnabled)
            _animator.SetTrigger(_attackTriggerHash);
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
