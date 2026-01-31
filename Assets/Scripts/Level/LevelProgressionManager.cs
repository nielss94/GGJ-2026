using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Additive level loading: keeps persistent scene, loads/unloads level scenes only.
/// Holds level variations and assigns two random next levels to doors when a level completes.
/// Place in persistent scene (e.g. on GameRoot). Call LoadFirstLevel() when the player presses Start Game (e.g. from main menu).
/// </summary>
public class LevelProgressionManager : MonoBehaviour
{
    public static LevelProgressionManager Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("Player transform to move to each level's PlayerSpawn. Leave empty to skip.")]
    [SerializeField] private Transform playerTransform;

    [Header("Level variations")]
    [Tooltip("Scene names for levels. First level is a random pick from this list; after that two are picked for doors.")]
    [SerializeField] private string[] levelVariations = Array.Empty<string>();

    [Header("Power budget")]
    [Tooltip("Base power budget for the first level. Each Encounter in the loaded scene gets this, then base + 1*increment, etc.")]
    [SerializeField] private float baseBudget = 20f;
    [Tooltip("Added to the budget for each level after the first (linear scaling).")]
    [SerializeField] private float budgetIncrementPerLevel = 5f;

    private Scene? currentLevelScene;
    private bool isLoading;
    private int levelDepth;
    private float currentLevelBudget;

    /// <summary>Budget for the level currently being loaded. Encounter reads this in Start().</summary>
    public float GetBudgetForCurrentLevel() => currentLevelBudget;

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
        EventBus.LevelComplete += AssignNextLevelsToDoors;
    }

    private void OnDisable()
    {
        EventBus.LevelComplete -= AssignNextLevelsToDoors;
    }

    /// <summary>
    /// Loads the first level (a random pick from level variations). Call when the player presses Start Game (e.g. from your main menu).
    /// Does nothing if level variations is empty or a load is already in progress.
    /// </summary>
    public void LoadFirstLevel()
    {
        var chosen = PickRandomVariations(1);
        if (chosen.Count == 0) return;
        LoadLevel(chosen[0]);
    }

    private void AssignNextLevelsToDoors()
    {
        if (!currentLevelScene.HasValue || !currentLevelScene.Value.isLoaded) return;
        if (levelVariations == null || levelVariations.Length == 0) return;

        var doors = GetDoorsInScene(currentLevelScene.Value);
        if (doors.Count == 0) return;

        var chosen = PickRandomVariations(2);
        for (int i = 0; i < chosen.Count && i < doors.Count; i++)
            doors[i].SetNextScene(chosen[i]);
    }

    private List<LevelDoor> GetDoorsInScene(Scene scene)
    {
        var list = new List<LevelDoor>();
        foreach (var root in scene.GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<LevelDoor>(true));
        return list;
    }

    private List<string> PickRandomVariations(int count)
    {
        var result = new List<string>(count);
        if (levelVariations == null || levelVariations.Length == 0) return result;

        var indices = new List<int>(levelVariations.Length);
        for (int i = 0; i < levelVariations.Length; i++)
        {
            if (!string.IsNullOrEmpty(levelVariations[i]))
                indices.Add(i);
        }

        for (int i = 0; i < count && indices.Count > 0; i++)
        {
            int pick = UnityEngine.Random.Range(0, indices.Count);
            result.Add(levelVariations[indices[pick]]);
            indices.RemoveAt(pick);
        }
        return result;
    }

    /// <summary>
    /// Unloads current level (if any), loads the given scene additively, then moves player to PlayerSpawn in that scene.
    /// </summary>
    public void LoadLevel(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        isLoading = true;
        currentLevelBudget = baseBudget + levelDepth * budgetIncrementPerLevel;

        if (playerTransform != null)
            playerTransform.gameObject.SetActive(true);

        if (currentLevelScene.HasValue && currentLevelScene.Value.isLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(currentLevelScene.Value);
            while (unload != null && !unload.isDone)
                yield return null;
        }

        var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            isLoading = false;
            yield break;
        }
        while (!load.isDone)
            yield return null;

        currentLevelScene = SceneManager.GetSceneByName(sceneName);
        levelDepth++;

        MovePlayerToSpawn(currentLevelScene.Value);
        isLoading = false;
    }

    private void MovePlayerToSpawn(Scene scene)
    {
        if (playerTransform == null) return;

        foreach (var root in scene.GetRootGameObjects())
        {
            var spawn = root.GetComponentInChildren<PlayerSpawn>(true);
            if (spawn != null)
            {
                playerTransform.position = spawn.transform.position;
                playerTransform.rotation = spawn.transform.rotation;
                return;
            }
        }
    }
}
