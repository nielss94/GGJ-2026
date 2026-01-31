using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single place to play sounds. Use FmodEventAsset (no string paths).
/// </summary>
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    private const string PausedParam = "Paused";

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
