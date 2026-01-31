using UnityEngine;

/// <summary>
/// Listens to the Encounter in this scene; when it completes, enables LevelDoor(s).
/// Keeps door logic separate from Encounter so Encounter stays focused on tracking/spawning.
/// </summary>
public class LevelCompleteDetector : MonoBehaviour
{
    [Tooltip("Leave empty to use the first Encounter in this scene.")]
    [SerializeField] private Encounter encounter;

    private void OnEnable()
    {
        if (encounter == null)
        {
            var scene = gameObject.scene;
            foreach (var e in FindObjectsByType<Encounter>(FindObjectsSortMode.None))
            {
                if (e.gameObject.scene == scene)
                {
                    encounter = e;
                    break;
                }
            }
        }
        if (encounter != null)
            encounter.Complete += OnEncounterComplete;
    }

    private void OnDisable()
    {
        if (encounter != null)
            encounter.Complete -= OnEncounterComplete;
    }

    private void OnEncounterComplete()
    {
        EventBus.RaiseLevelComplete();
        foreach (var door in GetComponentsInChildren<LevelDoor>(true))
            door.gameObject.SetActive(true);
    }
}
