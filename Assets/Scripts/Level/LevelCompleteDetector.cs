using System.Collections;
using UnityEngine;

/// <summary>
/// Listens to the Encounter; when it completes, runs: (pause) → UpgradePanel → (pause) → doors sink (Open).
/// Doors are assigned in the Inspector and are never disabled; they sink into the ground when opened.
/// </summary>
public class LevelCompleteDetector : MonoBehaviour
{
    [Tooltip("Leave empty to use the first Encounter in this scene.")]
    [SerializeField] private Encounter encounter;
    [Tooltip("Doors to open (sink) after the player chooses an upgrade. Assign in the Inspector.")]
    [SerializeField] private LevelDoor[] doors = System.Array.Empty<LevelDoor>();

    [Header("Delays")]
    [Tooltip("Seconds to wait after encounter completes before showing the upgrade panel.")]
    [SerializeField] private float delayBeforeUpgradePanel = 1f;
    [Tooltip("Seconds to wait after the player chooses an upgrade before doors start sinking.")]
    [SerializeField] private float delayBeforeDoorsOpen = 1f;

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
        EventBus.UpgradeChosen += OnUpgradeChosen;
    }

    private void OnDisable()
    {
        if (encounter != null)
            encounter.Complete -= OnEncounterComplete;
        EventBus.UpgradeChosen -= OnUpgradeChosen;
    }

    private void OnEncounterComplete()
    {
        EventBus.RaiseLevelComplete();
        StartCoroutine(ShowUpgradePanelAfterDelay());
    }

    private IEnumerator ShowUpgradePanelAfterDelay()
    {
        if (delayBeforeUpgradePanel > 0f)
            yield return new WaitForSeconds(delayBeforeUpgradePanel);
        if (UpgradePanel.Instance != null)
            UpgradePanel.Instance.Open();
    }

    private void OnUpgradeChosen(UpgradeOffer _)
    {
        StartCoroutine(OpenDoorsAfterDelay());
    }

    private IEnumerator OpenDoorsAfterDelay()
    {
        if (delayBeforeDoorsOpen > 0f)
            yield return new WaitForSeconds(delayBeforeDoorsOpen);
        if (doors == null) yield break;
        foreach (var door in doors)
        {
            if (door != null)
                door.Open();
        }
    }
}
