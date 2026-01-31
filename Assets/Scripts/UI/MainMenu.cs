using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void OnStartGameClicked()
    {
        var menuScene = gameObject.scene;
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.LoadFirstLevel();
        SceneManager.UnloadSceneAsync(menuScene);
    }
}
