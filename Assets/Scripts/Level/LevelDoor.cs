using System.Collections;
using UnityEngine;

/// <summary>
/// Door in a level scene. Always visible; when Open() is called the art sinks into the ground (trigger stays in place).
/// Next scene is assigned by LevelProgressionManager when the level completes.
/// Assign a child to Sink Visual so only the art moves; the trigger collider on this GameObject stays fixed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelDoor : MonoBehaviour
{
    [Header("Sink")]
    [Tooltip("Child transform to move down (door art). If empty, this GameObject moves instead (trigger moves with it).")]
    [SerializeField] private Transform sinkVisual;
    [Tooltip("Distance to move the sink visual down when opening.")]
    [SerializeField] private float sinkHeight = 2f;
    [Tooltip("Duration of the sink animation in seconds.")]
    [SerializeField] private float sinkDuration = 1f;
    [Tooltip("Optional: use an Animator on the sink visual with this trigger instead of the simple sink. If set, sinkHeight/sinkDuration are ignored.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [Tooltip("When using Animator: seconds to wait before the door trigger zone becomes active.")]
    [SerializeField] private float animatorOpenDelay = 1f;

    [Header("Audio")]
    [Tooltip("Optional FMOD event played when the door opens (sinks into ground).")]
    [SerializeField] private FmodEventAsset fmodDoorOpen;
    [Tooltip("Optional FMOD event played when the player goes through this door to the next level.")]
    [SerializeField] private FmodEventAsset fmodDoorTransition;

    private string nextSceneName;
    private bool isOpen;
    private Vector3 sinkVisualStartPosition;

    /// <summary>True after Open() has finished (door has sunk). Trigger zone only loads level when this is true.</summary>
    public bool IsOpen => isOpen;

    /// <summary>Called by LevelProgressionManager when the level completes. Do not set manually.</summary>
    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
    }

    private void Awake()
    {
        Transform target = sinkVisual != null ? sinkVisual : transform;
        sinkVisualStartPosition = target.position;
    }

    /// <summary>Called by LevelCompleteDetector after the player chooses an upgrade. Sinks the visual into the ground; trigger zone stays in place.</summary>
    public void Open()
    {
        if (isOpen) return;
        if (fmodDoorOpen != null && AudioService.Instance != null)
            AudioService.Instance.PlayOneShot(fmodDoorOpen, transform.position);
        if (animator != null)
        {
            animator.SetTrigger(openTrigger);
            StartCoroutine(SetOpenAfterDelay(animatorOpenDelay > 0f ? animatorOpenDelay : 1f));
        }
        else
        {
            StartCoroutine(SinkThenOpen());
        }
    }

    private IEnumerator SinkThenOpen()
    {
        Transform target = sinkVisual != null ? sinkVisual : transform;
        Vector3 endPos = sinkVisualStartPosition + Vector3.down * sinkHeight;
        float elapsed = 0f;
        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sinkDuration);
            target.position = Vector3.Lerp(sinkVisualStartPosition, endPos, t);
            yield return null;
        }
        target.position = endPos;
        isOpen = true;
    }

    private IEnumerator SetOpenAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isOpen = true;
    }

    /// <summary>Loads the level assigned to this door. Call from trigger zone (OnTriggerEnter) when IsOpen is true.</summary>
    public void Choose()
    {
        if (!isOpen) return;
        if (fmodDoorTransition != null && AudioService.Instance != null)
            AudioService.Instance.PlayOneShot(fmodDoorTransition, transform.position);
        if (LevelProgressionManager.Instance != null && !string.IsNullOrEmpty(nextSceneName))
            LevelProgressionManager.Instance.LoadLevel(nextSceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isOpen) return;
        if (other.CompareTag("Player"))
            Choose();
    }
}
