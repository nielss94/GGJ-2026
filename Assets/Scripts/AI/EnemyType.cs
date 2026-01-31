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

    [Header("Ranged (RangedAttack)")]
    [Tooltip("Applied if the prefab has RangedAttack.")]
    [SerializeField] private float rangedDamage = 15f;
    [SerializeField] private float rangedFireInterval = 1.5f;
    [SerializeField] private float rangedProjectileSpeed = 12f;

    public string DisplayName => displayName;
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float ContactDamage => contactDamage;
    public float ContactCooldown => contactCooldown;
    public float RangedDamage => rangedDamage;
    public float RangedFireInterval => rangedFireInterval;
    public float RangedProjectileSpeed => rangedProjectileSpeed;
}
