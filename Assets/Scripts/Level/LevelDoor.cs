using UnityEngine;

/// <summary>
/// Place in a level scene on a GameObject (start disabled). LevelCompleteDetector enables it when the encounter completes.
/// Next scene is assigned by LevelProgressionManager when the level completes; call Choose() from a button or trigger to load it.
/// </summary>
public class LevelDoor : MonoBehaviour
{
    private string nextSceneName;

    /// <summary>Called by LevelProgressionManager when the level completes. Do not set manually.</summary>
    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
    }

    /// <summary>Loads the level assigned to this door. Call from UI button OnClick or from an in-world trigger.</summary>
    public void Choose()
    {
        if (LevelProgressionManager.Instance != null && !string.IsNullOrEmpty(nextSceneName))
            LevelProgressionManager.Instance.LoadLevel(nextSceneName);
    }
}
