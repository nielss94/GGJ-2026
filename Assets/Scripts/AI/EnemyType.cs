using UnityEngine;

/// <summary>
/// ScriptableObject defining shared stats for an enemy type. Assign to prefabs for consistent tuning
/// (e.g. "Goblin", "Orc"). EnemyTypeApplier applies base stats and, if present, melee/ranged stats to
/// ContactDamage and RangedAttack. Attack style (hitscan vs projectile, prefab) stays on the component.
/// </summary>
[CreateAssetMenu(fileName = "EnemyType", menuName = "GGJ-2026/Enemy Type", order = 0)]
public class EnemyType : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string displayName = "Enemy";

    [Header("Base")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Melee (ContactDamage)")]
    [Tooltip("Applied if the prefab has ContactDamage.")]
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float contactCooldown = 1f;
    [Tooltip("Distance at which melee attack is valid. Enemy stops moving when in range with LOS.")]
    [SerializeField] private float meleeAttackRange = 1.8f;
    [Tooltip("Telegraph/channel duration before the hit (tells the player how long until attack).")]
    [SerializeField] private float meleeTelegraphDuration = 0.4f;
    [Tooltip("How long the hitbox is active after the telegraph (window to deal damage).")]
    [SerializeField] private float meleeAttackActiveDuration = 0.25f;

    [Header("Ranged (RangedAttack)")]
    [Tooltip("Applied if the prefab has RangedAttack.")]
    [SerializeField] private float rangedDamage = 15f;
    [SerializeField] private float rangedFireInterval = 1.5f;
    [SerializeField] private float rangedProjectileSpeed = 12f;
    [Tooltip("Distance at which ranged attack is valid. Enemy stops moving when in range with LOS.")]
    [SerializeField] private float rangedAttackRange = 15f;
    [Tooltip("Telegraph/channel duration before firing (no shooting through walls; requires LOS).")]
    [SerializeField] private float rangedTelegraphDuration = 0.3f;

    public string DisplayName => displayName;
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float ContactDamage => contactDamage;
    public float ContactCooldown => contactCooldown;
    public float MeleeAttackRange => meleeAttackRange;
    public float MeleeTelegraphDuration => meleeTelegraphDuration;
    public float MeleeAttackActiveDuration => meleeAttackActiveDuration;
    public float RangedDamage => rangedDamage;
    public float RangedFireInterval => rangedFireInterval;
    public float RangedProjectileSpeed => rangedProjectileSpeed;
    public float RangedAttackRange => rangedAttackRange;
    public float RangedTelegraphDuration => rangedTelegraphDuration;
}
