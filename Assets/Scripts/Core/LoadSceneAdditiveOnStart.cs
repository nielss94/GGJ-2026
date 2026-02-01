using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads a scene additively when this component starts. Use in your persistent (BaseGame) scene
/// to load the menu (or intro) on top without unloading the base scene.
/// When using this for the main menu, set LevelProgressionManager.mainMenuSceneName so the menu
/// is unloaded when the game starts and can be reloaded when returning to main menu. For Cinemachine:
/// keep one output Camera (with CinemachineBrain) in BaseGame always enabled; add a CinemachineCamera
/// (virtual) in the main menu scene for the menu view—LevelProgressionManager switches by VCam priority.
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
