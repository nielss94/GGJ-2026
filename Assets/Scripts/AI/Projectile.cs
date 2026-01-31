using UnityEngine;

/// <summary>
/// Attach to a projectile prefab. Init(damage, speed, owner) is called by RangedAttack when spawned.
/// Moves forward and applies damage to the first Health it hits (ignores owner).
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

    /// <summary>Call after spawning to set damage, speed, and owner (ignored for damage).</summary>
    public void Init(float damage, float speed, GameObject owner)
    {
        _damage = damage;
        _speed = speed;
        _owner = owner;
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody>();
        _rb.isKinematic = false;
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.linearVelocity = transform.forward * _speed;
        _initialized = true;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;
        if (other.gameObject == _owner) return;

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
        if (collision.gameObject == _owner) return;

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
