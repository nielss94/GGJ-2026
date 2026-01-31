using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer database of all droppable item prefabs. Enemies use this to spawn drops on death.
/// Create via Assets > Create > GGJ-2026 > Drop Database.
/// </summary>
[CreateAssetMenu(fileName = "DropDatabase", menuName = "GGJ-2026/Drop Database")]
public class DropDatabase : ScriptableObject
{
    [Header("Drops")]
    [Tooltip("All droppable item definitions (prefab + type + weight).")]
    [SerializeField] private List<DropItemDefinition> drops = new List<DropItemDefinition>();

    /// <summary>Read-only list of drop definitions.</summary>
    public IReadOnlyList<DropItemDefinition> Drops => drops;

    /// <summary>
    /// Picks a random drop by weight. Returns null if no drops or all weights are zero.
    /// </summary>
    public DropItemDefinition GetRandomDrop()
    {
        if (drops == null || drops.Count == 0)
            return null;

        float total = 0f;
        foreach (var d in drops)
        {
            if (d != null)
                total += d.DropWeight;
        }

        if (total <= 0f)
            return drops[0];

        float roll = Random.Range(0f, total);
        foreach (var d in drops)
        {
            if (d == null) continue;
            roll -= d.DropWeight;
            if (roll <= 0f)
                return d;
        }

        return drops[drops.Count - 1];
    }
}
