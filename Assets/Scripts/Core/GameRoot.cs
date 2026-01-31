using UnityEngine;

/// <summary>
/// Persistent root object. Ensures only one exists and survives scene loads.
/// Children (e.g. AudioSource) stay alive across scenes via DontDestroyOnLoad.
/// </summary>
public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }

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
}
