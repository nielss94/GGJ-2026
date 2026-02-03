using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

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

    /// <summary>Player used for gameplay (UIManager enables and refreshes it when showing game HUD).</summary>
    public Transform DesignatedPlayer => playerTransform;

    [Header("Main menu (additive)")]
    [Tooltip("If set, this scene is unloaded when the game starts (LoadFirstLevel) and can be reloaded when returning to main menu. Leave empty if menu is a panel in BaseGame.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("Cinemachine: priority for the active virtual camera (menu or gameplay). The inactive one is set to 0. MainMenu scene should contain a CinemachineCamera for the menu view.")]
    [SerializeField] private int cinemachineActivePriority = 20;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        EventBus.LevelComplete -= AssignNextLevelsToDoors;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName) && scene.name == mainMenuSceneName)
            SetCinemachineMenuActive();
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

    /// <summary>
    /// Loads the main menu scene additively. Call when returning to main menu (e.g. from death screen).
    /// Uses Cinemachine: when loaded, raises the menu virtual camera(s) priority and lowers the gameplay VCam so the Brain uses the menu.
    /// Does nothing if mainMenuSceneName is empty.
    /// </summary>
    public void LoadMainMenuScene()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName)) return;
        var scene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (scene.isLoaded)
        {
            SetCinemachineMenuActive();
            return;
        }
        StartCoroutine(LoadMainMenuSceneRoutine());
    }

    private IEnumerator LoadMainMenuSceneRoutine()
    {
        var load = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
        if (load == null) yield break;
        while (!load.isDone)
            yield return null;
        SetCinemachineMenuActive();
    }

    /// <summary>Cinemachine workflow: one output Camera (with Brain) stays enabled. We switch by virtual camera priority: menu VCam(s) high, gameplay VCam low.</summary>
    private void SetCinemachineMenuActive()
    {
        SetGameplayCinemachinePriority(0);
        var menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (!menuScene.isLoaded) return;
        bool foundMenuVcam = false;
        foreach (var root in menuScene.GetRootGameObjects())
        {
            foreach (var vcam in root.GetComponentsInChildren<CinemachineCamera>(true))
            {
                SetCinemachinePriority(vcam, cinemachineActivePriority);
                foundMenuVcam = true;
            }
        }
        if (!foundMenuVcam)
            Debug.LogWarning($"LevelProgressionManager: No CinemachineCamera found in scene '{mainMenuSceneName}'. Add a CinemachineCamera to the main menu scene for the proper Cinemachine workflow.", this);
    }

    /// <summary>Cinemachine workflow: gameplay VCam high priority so the Brain uses it; menu VCam(s) low (or scene unloaded).</summary>
    private void SetCinemachineGameplayActive()
    {
        SetGameplayCinemachinePriority(cinemachineActivePriority);
        var menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (!menuScene.isLoaded) return;
        foreach (var root in menuScene.GetRootGameObjects())
        {
            foreach (var vcam in root.GetComponentsInChildren<CinemachineCamera>(true))
                SetCinemachinePriority(vcam, 0);
        }
    }

    private void SetGameplayCinemachinePriority(int priority)
    {
        if (playerTransform == null) return;
        var vcam = playerTransform.GetComponentInChildren<CinemachineCamera>(true);
        if (vcam != null)
            SetCinemachinePriority(vcam, priority);
    }

    private static void SetCinemachinePriority(CinemachineCamera vcam, int priority)
    {
        if (vcam == null) return;
        vcam.Priority = priority; // implicit PrioritySettings(int): enables and sets value
    }

    /// <summary>
    /// Unloads the current level (if any) and resets depth to 0. Call when returning to main menu or before starting a new run.
    /// When onComplete is provided, it is invoked after the unload finishes.
    /// </summary>
    public void UnloadCurrentLevel(Action onComplete = null)
    {
        StartCoroutine(UnloadCurrentLevelRoutine(onComplete));
    }

    private IEnumerator UnloadCurrentLevelRoutine(Action onComplete)
    {
        if (currentLevelScene.HasValue && currentLevelScene.Value.isLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(currentLevelScene.Value);
            while (unload != null && !unload.isDone)
                yield return null;
        }
        currentLevelScene = null;
        levelDepth = 0;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Resets the player for a new run: health to full, stats and ability upgrades to base, and clears collected drops.
    /// Call when starting a new run after death (before LoadFirstLevel).
    /// </summary>
    public void ResetPlayerForNewRun()
    {
        if (playerTransform == null) return;

        var health = playerTransform.GetComponent<Health>();
        if (health != null)
            health.ResetToFull();

        var stats = playerTransform.GetComponent<PlayerStats>();
        if (stats != null)
            stats.ResetToBase();

        var abilityManager = playerTransform.GetComponentInChildren<PlayerAbilityManager>(true);
        if (abilityManager != null)
            abilityManager.ResetAllAbilitiesToBase();

        var dropManager = playerTransform.GetComponentInChildren<PlayerDropManager>(true);
        if (dropManager != null)
            dropManager.ClearAllDrops();
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

        if (currentLevelScene.HasValue && currentLevelScene.Value.isLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(currentLevelScene.Value);
            while (unload != null && !unload.isDone)
                yield return null;
        }
        else if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            var menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
            if (menuScene.isLoaded)
            {
                var unloadMenu = SceneManager.UnloadSceneAsync(menuScene);
                while (unloadMenu != null && !unloadMenu.isDone)
                    yield return null;
                SetCinemachineGameplayActive();
            }
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

        DisablePlayerAndCameraInScene(currentLevelScene.Value);
        MovePlayerToSpawn(currentLevelScene.Value);
        PrepareDesignatedPlayerForGameplay();
        if (AudioService.Instance != null)
            AudioService.Instance.NextMusicVibe();
        isLoading = false;
    }

    /// <summary>Enables the designated player and its input. Call after level load or when showing game HUD.</summary>
    public void PrepareDesignatedPlayerForGameplay()
    {
        if (playerTransform == null) return;
        playerTransform.gameObject.SetActive(true);
        playerTransform.GetComponentInChildren<PlayerMovement>(true)?.EnsureInputEnabled();
        playerTransform.GetComponentInChildren<PlayerAbilityManager>(true)?.EnsureInputEnabled();
    }

    /// <summary>Disables Player and MainCamera in the given scene (keeps level testable standalone). Never disables playerTransform.</summary>
    private void DisablePlayerAndCameraInScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.CompareTag("Player") && t != playerTransform)
                    t.gameObject.SetActive(false);
            }
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam.CompareTag("MainCamera"))
                    cam.gameObject.SetActive(false);
            }
        }
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
