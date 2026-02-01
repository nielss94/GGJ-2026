using System;
using UnityEngine;

/// <summary>
/// Snapshot of run performance for the death recap screen.
/// </summary>
public struct RunRecap
{
    public int LevelsCompleted;
    public int EnemiesKilled;
    public float TimeSurvivedSeconds;

    public string FormatTime()
    {
        int minutes = Mathf.FloorToInt(TimeSurvivedSeconds / 60f);
        int seconds = Mathf.FloorToInt(TimeSurvivedSeconds % 60f);
        return $"{minutes}:{seconds:00}";
    }
}

/// <summary>
/// Tracks run stats (enemies killed, levels completed, time) for the current run.
/// Reset when starting a new run. Subscribe in OnEnable, unsubscribe in OnDisable.
/// Place on a persistent object (e.g. GameRoot or a dedicated RunStats object in BaseGame).
/// </summary>
public class RunStats : MonoBehaviour
{
    public static RunStats Instance { get; private set; }

    private int enemiesKilled;
    private int levelsCompleted;
    private float runStartTime;
    private bool runStarted;

    public int EnemiesKilled => enemiesKilled;
    public int LevelsCompleted => levelsCompleted;
    public float TimeSurvivedSeconds => runStarted ? Time.time - runStartTime : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        EventBus.EnemyDied += OnEnemyDied;
        EventBus.LevelComplete += OnLevelComplete;
    }

    private void OnDisable()
    {
        EventBus.EnemyDied -= OnEnemyDied;
        EventBus.LevelComplete -= OnLevelComplete;
    }

    private void OnEnemyDied()
    {
        enemiesKilled++;
    }

    private void OnLevelComplete()
    {
        levelsCompleted++;
    }

    /// <summary>
    /// Call when starting a new run (main menu Start Game or death screen Start New Run).
    /// Resets counts and run timer.
    /// </summary>
    public void ResetRun()
    {
        enemiesKilled = 0;
        levelsCompleted = 0;
        runStartTime = Time.time;
        runStarted = true;
    }

    /// <summary>
    /// Call once when the game starts so the timer begins on first run.
    /// Optional: if you only call ResetRun() when starting a run, the first run won't have a timer until LoadFirstLevel.
    /// </summary>
    public void MarkRunStarted()
    {
        if (!runStarted)
        {
            runStartTime = Time.time;
            runStarted = true;
        }
    }

    /// <summary>
    /// Get a snapshot of the current run for the death recap. Call when the player dies.
    /// </summary>
    public RunRecap GetRecap()
    {
        return new RunRecap
        {
            LevelsCompleted = levelsCompleted,
            EnemiesKilled = enemiesKilled,
            TimeSurvivedSeconds = TimeSurvivedSeconds
        };
    }
}
