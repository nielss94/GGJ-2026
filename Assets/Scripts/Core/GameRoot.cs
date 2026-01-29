using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single entry point when the game starts. Lives in Bootstrap (first scene).
/// Why GameRoot?
/// 1. Something must run first — the first scene needs one object that owns startup.
/// 2. DontDestroyOnLoad — services (e.g. AudioService) live on this object so they
///    survive when we load Game.unity; without a persistent root they would be destroyed.
/// 3. Init order — we do one-time setup (mark as persistent), then load the real game
///    scene in a controlled way instead of random Awake/Start order.
/// 4. One-time load — _done guards so we only load Game once (no double load / race).
/// </summary>
public class GameRoot : MonoBehaviour
{
    [SerializeField] private string _gameSceneName = "Game";

    private static bool _done;

    private void Awake()
    {
        if (_done) return;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_done) return;
        _done = true;
        SceneManager.LoadScene(_gameSceneName);
    }
}
