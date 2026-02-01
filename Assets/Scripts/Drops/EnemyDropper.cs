using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add to an enemy with Health. On death, spawns a random drop from DropDatabase at the enemy position
/// and sends it to the player's <see cref="PlayerDropManager"/>. Subscribes to Health.onDeath automatically.
/// </summary>
public class EnemyDropper : MonoBehaviour
{
    [Header("Database & Target")]
    [Tooltip("Database of droppable prefabs. If empty, no drops spawn.")]
    [SerializeField] private DropDatabase dropDatabase;
    [Tooltip("Player drop manager that receives drops. Leave empty to find by tag 'Player' and get PlayerDropManager on it or its children.")]
    [SerializeField] private PlayerDropManager dropManager;

    [Header("Spawn")]
    [Tooltip("Offset from enemy position when spawning (e.g. slightly above).")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);

    private bool dropping;

    private void Start()
    {
        if (dropManager == null)
            ResolveDropManager();
        var health = GetComponent<Health>();
        if (health != null)
            health.AddOnDeathListener(Drop);
    }

    /// <summary>Call from Health's OnDeath (wire in inspector) or from your own logic.</summary>
    public void Drop()
    {
        if (dropping || dropDatabase == null)
            return;
        if (dropManager == null)
            ResolveDropManager();
        if (dropManager == null)
            return;

        DropItemDefinition def = dropDatabase.GetRandomDrop();
        if (def == null || def.Prefab == null)
            return;

        dropping = true;
        Vector3 spawnPos = transform.position + spawnOffset;
        GameObject go = Instantiate(def.Prefab, spawnPos, Quaternion.identity);
        if (go.TryGetComponent(out DroppableItem item))
        {
            item.Init(def, dropManager);
        }

        dropping = false;
    }

    private void ResolveDropManager()
    {
        if (dropManager != null) return;
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            dropManager = player.GetComponentInChildren<PlayerDropManager>(true);
    }
}
