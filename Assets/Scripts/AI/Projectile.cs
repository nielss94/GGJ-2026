using UnityEngine;

/// <summary>
/// Fireball-style projectile: moves in a straight line, no physics. Init(damage, speed, owner) is called by RangedAttack when spawned.
/// Uses a kinematic Rigidbody so trigger overlap is detected; movement is done in Update. Set collider to Is Trigger.
/// Uses layers: set to Projectile on spawn; ignores Enemy layer and owner.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    private float _damage;
    private float _speed;
    private GameObject _owner;
    private Rigidbody _rb;
    private bool _initialized;
    private static int _enemyLayer = -1;

    private static int EnemyLayer => _enemyLayer >= 0 ? _enemyLayer : _enemyLayer = LayerMask.NameToLayer("Enemy");

    /// <summary>Call after spawning to set damage, speed, and owner (ignored for damage).</summary>
    public void Init(float damage, float speed, GameObject owner)
    {
        _damage = damage;
        _speed = speed;
        _owner = owner;
        gameObject.layer = LayerMask.NameToLayer("Projectile");
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _initialized = true;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!_initialized) return;
        Vector3 move = transform.forward;
        if (move.y < 0f) move.y = 0f;
        if (move.sqrMagnitude > 0.001f)
        {
            move.Normalize();
            transform.position += move * (_speed * Time.deltaTime);
        }
    }

    private bool ShouldIgnore(GameObject other)
    {
        if (other == _owner) return true;
        if (EnemyLayer >= 0 && other.layer == EnemyLayer) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;
        if (ShouldIgnore(other.gameObject)) return;

        var health = other.GetComponent<Health>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_initialized) return;
        if (ShouldIgnore(collision.gameObject)) return;

        var health = collision.gameObject.GetComponent<Health>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(_damage);
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
