using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads a scene additively when this component starts. Use in your persistent (BaseGame) scene
/// to load the menu (or intro) on top without unloading the base scene.
/// </summary>
public class LoadSceneAdditiveOnStart : MonoBehaviour
{
    [Tooltip("Scene to load additively when this object starts (e.g. MainMenu). Must be in Build Settings.")]
    [SerializeField] private string sceneName = "";

    private void Start()
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }
}
