using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-local encounter: spawns enemies from a power budget. Majority spawn at start; more can spawn during the level.
/// Level completes when the budget is depleted and all spawned enemies are defeated.
/// </summary>
public class Encounter : MonoBehaviour
{
    [Header("Budget")]
    [Tooltip("Total power budget for this encounter. Spawning deducts each enemy's PowerCost (from EnemyType).")]
    [SerializeField] private float budget = 20f;
    [Tooltip("Fraction of budget to spend in the initial wave (e.g. 0.7 = 70% at start, 30% for during-level spawns).")]
    [SerializeField] [Range(0f, 1f)] private float initialSpendRatio = 0.7f;

    [Header("Spawning")]
    [Tooltip("Seconds to wait after the level loads before spawning the first wave. Gives the player a moment before enemies appear.")]
    [SerializeField] private float spawnStartDelay = 1f;
    [Tooltip("Prefabs to spawn (must have Health and EnemyTypeApplier for cost).")]
    [SerializeField] private GameObject[] enemyPrefabs = Array.Empty<GameObject>();
    [Tooltip("Where to spawn enemies. Uses this transform's position if empty.")]
    [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();
    [Tooltip("If true, attempt to spawn one enemy every spawn interval while budget allows.")]
    [SerializeField] private bool spawnDuringLevel = true;
    [Tooltip("Seconds between spawn attempts during the level.")]
    [SerializeField] private float spawnInterval = 5f;

    private float remainingBudget;
    private readonly HashSet<Health> tracked = new HashSet<Health>();
    private bool completed;

    /// <summary>Remaining power budget. Decremented when spawning; encounter completes when this is 0 and all tracked are dead.</summary>
    public float RemainingBudget => remainingBudget;

    /// <summary>Raised when budget is depleted and all spawned enemies are dead.</summary>
    public event Action Complete;

    /// <summary>Set budget at runtime (e.g. from LevelProgressionManager for linear scaling). Use before or after Start.</summary>
    public void SetBudget(float value)
    {
        budget = Mathf.Max(0f, value);
        remainingBudget = budget;
    }

    private void Start()
    {
        if (LevelProgressionManager.Instance != null)
            SetBudget(LevelProgressionManager.Instance.GetBudgetForCurrentLevel());
        remainingBudget = budget;
        if (spawnStartDelay > 0f)
            Invoke(nameof(StartSpawning), spawnStartDelay);
        else
            StartSpawning();
    }

    private void StartSpawning()
    {
        SpawnInitialWave();
        if (spawnDuringLevel && spawnInterval > 0f)
            InvokeRepeating(nameof(TrySpawnOne), spawnInterval, spawnInterval);
    }

    private void SpawnInitialWave()
    {
        float reserve = budget * (1f - initialSpendRatio);
        while (remainingBudget > reserve && TrySpawnOne()) { }
    }

    /// <summary>
    /// Try to spawn one enemy if budget and prefabs/spawn points allow. Returns true if one was spawned.
    /// </summary>
    public bool TrySpawnOne()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return false;
        if (spawnPoints != null && spawnPoints.Length == 0)
        {
            // Fallback: spawn at encounter position
        }

        GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
        if (prefab == null) return false;

        int cost = GetCost(prefab);
        if (cost <= 0) cost = 1;
        if (cost > remainingBudget) return false;

        Vector3 position;
        Quaternion rotation;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            position = point != null ? point.position : transform.position;
            rotation = point != null ? point.rotation : Quaternion.identity;
        }
        else
        {
            position = transform.position;
            rotation = Quaternion.identity;
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
        if (instance.TryGetComponent(out Health health))
        {
            Track(health);
            remainingBudget -= cost;
            return true;
        }

        Destroy(instance);
        return false;
    }

    private static int GetCost(GameObject prefab)
    {
        if (prefab == null) return 1;
        var applier = prefab.GetComponent<EnemyTypeApplier>();
        return applier != null ? applier.PowerCost : 1;
    }

    /// <summary>Start tracking this health. When it dies it is removed; when last one dies and budget depleted, Complete is raised.</summary>
    public void Track(Health health)
    {
        if (health == null || tracked.Contains(health)) return;
        tracked.Add(health);
        health.Died += OnTrackedDied;
    }

    private void OnTrackedDied()
    {
        Health toRemove = null;
        foreach (var health in tracked)
        {
            if (health.IsDead)
            {
                toRemove = health;
                break;
            }
        }
        if (toRemove != null)
        {
            toRemove.Died -= OnTrackedDied;
            tracked.Remove(toRemove);
        }
        if (!completed && tracked.Count == 0 && remainingBudget <= 0f)
        {
            completed = true;
            CancelInvoke(nameof(TrySpawnOne));
            Complete?.Invoke();
        }
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(TrySpawnOne));
        foreach (var health in tracked)
        {
            if (health != null)
                health.Died -= OnTrackedDied;
        }
        tracked.Clear();
    }
}
