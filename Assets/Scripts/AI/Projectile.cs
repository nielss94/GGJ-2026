using UnityEngine;

/// <summary>
/// Fireball-style projectile: moves in a straight line. Init(damage, speed, owner) is called by RangedAttack when spawned.
/// Moves with transform.position (not MovePosition) so the player is not pushed when hit. Rigidbody is kept for trigger detection only.
/// Uses layers: Projectile on spawn; ignores Enemy and owner.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    private float damage;
    private float speed;
    private GameObject owner;
    private Rigidbody rb;
    private bool initialized;
    private static int enemyLayer = -1;

    private static int EnemyLayer => enemyLayer >= 0 ? enemyLayer : enemyLayer = LayerMask.NameToLayer("Enemy");

    /// <summary>Call after spawning to set damage, speed, and owner (ignored for damage).</summary>
    public void Init(float damage, float speed, GameObject owner)
    {
        this.damage = damage;
        this.speed = speed;
        this.owner = owner;
        gameObject.layer = LayerMask.NameToLayer("Projectile");
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        initialized = true;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!initialized) return;
        Vector3 move = transform.forward;
        if (move.y < 0f) move.y = 0f;
        if (move.sqrMagnitude > 0.001f)
        {
            move.Normalize();
            transform.position += move * (speed * Time.deltaTime);
        }
    }

    private bool ShouldIgnore(GameObject other)
    {
        if (other == owner) return true;
        if (EnemyLayer >= 0 && other.layer == EnemyLayer) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;
        if (ShouldIgnore(other.gameObject)) return;

        var health = other.GetComponent<Health>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!initialized) return;
        if (ShouldIgnore(collision.gameObject)) return;

        var health = collision.gameObject.GetComponent<Health>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
