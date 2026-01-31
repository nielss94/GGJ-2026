using UnityEngine;

/// <summary>
/// Attack behaviour: deal damage to anything with Health on collision. Use with any enemy type.
/// </summary>
public class ContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;

    /// <summary>Set damage and cooldown (e.g. from EnemyType at spawn).</summary>
    public void SetDamageAndCooldown(float dmg, float cd)
    {
        damage = dmg;
        cooldown = cd;
    }

    private float lastHitTime = float.NegativeInfinity;

    private void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryDamage(collision.gameObject);
    }

    private void TryDamage(GameObject other)
    {
        if (Time.time - lastHitTime < cooldown)
            return;

        var health = other.GetComponent<Health>();
        if (health == null || health.IsDead)
            return;

        health.TakeDamage(damage);
        lastHitTime = Time.time;
    }
}
