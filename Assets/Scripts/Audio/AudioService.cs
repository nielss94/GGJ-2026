using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;

/// <summary>
/// Single place to play sounds. Use FmodEventAsset (no string paths).
/// </summary>
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    public FmodEventAsset fmodMusic;
    [Tooltip("Optional FMOD event played when the level is cleared (all monsters dead, EventBus.LevelComplete). e.g. enemies_cleared.")]
    [SerializeField] private FmodEventAsset fmodLevelCleared;

    private const string PausedParam = "Paused";
    private const float MaxVibe = 4f;
    private const float VibeTransitionSpeed = 0.1f;

    private EventInstance musicInstance;

    public enum LevelState
    {
        Stop,
        Go
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        musicInstance = FMODUnity.RuntimeManager.CreateInstance(fmodMusic.EventReference);
        musicInstance.start();
    }

    private void OnEnable()
    {
        EventBus.LevelComplete += OnLevelComplete;
    }

    private void OnDisable()
    {
        EventBus.LevelComplete -= OnLevelComplete;
    }

    private void OnLevelComplete()
    {
        SetLevelState(LevelState.Stop);
        if (fmodLevelCleared != null && !fmodLevelCleared.IsNull)
            PlayOneShot(fmodLevelCleared);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void NextMusicVibe()
    {
        StartCoroutine(TransitionToNextVibe());
    }

    public void SetLevelState(LevelState levelState)
    {
        switch (levelState)
        {
            case LevelState.Stop:
                musicInstance.setParameterByNameWithLabel("state", "stop");
                break;
            case LevelState.Go:
                musicInstance.setParameterByNameWithLabel("state", "go");
                break;
        }
    }

    private IEnumerator TransitionToNextVibe()
    {
        musicInstance.getParameterByName("vibe", out float value);
        float targetValue = value + 1f;
        bool wrapToFirst = targetValue > MaxVibe;
        if (wrapToFirst)
            targetValue = 1f;

        if (wrapToFirst)
        {
            while (value > targetValue)
            {
                value -= Time.deltaTime * VibeTransitionSpeed;
                if (value < targetValue) value = targetValue;
                musicInstance.setParameterByName("vibe", value);
                yield return null;
            }
        }
        else
        {
            while (value < targetValue)
            {
                value += Time.deltaTime * VibeTransitionSpeed;
                if (value > targetValue) value = targetValue;
                musicInstance.setParameterByName("vibe", value);
                yield return null;
            }
        }
    }

    public void PlayOneShotWithParameter(FmodEventAsset fmodEvent, string parameterName, string parameterValue)
    {
        if (fmodEvent == null || fmodEvent.IsNull) return;
        var instance = FMODUnity.RuntimeManager.CreateInstance(fmodEvent.EventReference);
        instance.setParameterByNameWithLabel(parameterName, parameterValue.ToLower());
        instance.start();
        instance.release();
    }

    public void PlayOneShotWithParameters(FmodEventAsset fmodEvent, Dictionary<string, string> parameters)
    {
        if (fmodEvent == null || fmodEvent.IsNull) return;
        var instance = FMODUnity.RuntimeManager.CreateInstance(fmodEvent.EventReference);
        foreach (var parameter in parameters)
        {
            instance.setParameterByNameWithLabel(parameter.Key, parameter.Value.ToLower());
        }
        instance.start();
        instance.release();
    }


    public void PlayOneShotWithParametersInt(FmodEventAsset fmodEvent, Dictionary<string, int> parameters)
    {
        if (fmodEvent == null || fmodEvent.IsNull) return;
        var instance = FMODUnity.RuntimeManager.CreateInstance(fmodEvent.EventReference);
        foreach (var parameter in parameters)
        {
            instance.setParameterByName(parameter.Key, parameter.Value);
        }
        instance.start();
        instance.release();
    }

    public void PlayOneShot(FmodEventAsset fmodEvent)
    {
        if (fmodEvent == null || fmodEvent.IsNull) return;
        FMODUnity.RuntimeManager.PlayOneShot(fmodEvent.EventReference);
    }

    public void PlayOneShot(FmodEventAsset fmodEvent, Vector3 position)
    {
        if (fmodEvent == null || fmodEvent.IsNull) return;
        FMODUnity.RuntimeManager.PlayOneShot(fmodEvent.EventReference, position);
    }

    /// <summary>Plays a one-shot at a 3D position with label parameters (e.g. enemy_hittype) and int parameters (e.g. critical).</summary>
    public void PlayOneShotAtPositionWithParameters(FmodEventAsset fmodEvent, Vector3 position,
        Dictionary<string, string> labelParameters, Dictionary<string, int> intParameters)
    {
        if (fmodEvent == null || fmodEvent.IsNull) return;
        var instance = FMODUnity.RuntimeManager.CreateInstance(fmodEvent.EventReference);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
        if (labelParameters != null)
        {
            foreach (var p in labelParameters)
                instance.setParameterByNameWithLabel(p.Key, p.Value.ToLower());
        }
        if (intParameters != null)
        {
            foreach (var p in intParameters)
                instance.setParameterByName(p.Key, p.Value);
        }
        instance.start();
        instance.release();
    }

    public void SetGlobalParam(string name, float value)
    {
        if (!FMODUnity.RuntimeManager.IsInitialized) return;
        var result = FMODUnity.RuntimeManager.StudioSystem.setParameterByName(name, value);
        if (result != FMOD.RESULT.OK)
            Debug.LogWarning($"[AudioService] SetGlobalParam({name}, {value}) failed: {result}");
    }

    /// <summary>Convenience for Paused: 0 or 1.</summary>
    public void SetPaused(bool paused)
    {
        SetGlobalParam(PausedParam, paused ? 1f : 0f);
    }
}
