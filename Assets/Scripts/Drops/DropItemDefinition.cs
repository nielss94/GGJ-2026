using UnityEngine;

/// <summary>
/// Defines one droppable item: prefab and its drop type. Used in DropDatabase.
/// Create via Assets > Create > GGJ-2026 > Drop Item Definition.
/// </summary>
[CreateAssetMenu(fileName = "DropItem", menuName = "GGJ-2026/Drop Item Definition")]
public class DropItemDefinition : ScriptableObject
{
    [Header("Prefab")]
    [Tooltip("Prefab to spawn when this drop is chosen. Should have DroppableItem and a mesh; no colliders needed.")]
    [SerializeField] private GameObject prefab;

    [Header("Type & Weight")]
    [Tooltip("Determines which mask attachment strategy is used (spline vs slots).")]
    [SerializeField] private DropTypeId dropType;
    [Tooltip("Relative weight when picking a random drop. Higher = more likely.")]
    [SerializeField] private float dropWeight = 1f;

    public GameObject Prefab => prefab;
    public DropTypeId DropType => dropType;
    public float DropWeight => Mathf.Max(0.01f, dropWeight);
}
