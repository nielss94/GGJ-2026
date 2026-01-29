using UnityEngine;

public class Test : MonoBehaviour
{
    public FmodEventAsset testEvent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        AudioService.Instance.PlayOneShot(testEvent);
        EventBus.PlayerDied += OnPlayerDied;
    }

    void Start()
    {
        EventBus.RaisePlayerDied();
    }

    void OnDestroy()
    {
        EventBus.PlayerDied -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        AudioService.Instance.PlayOneShot(testEvent);
    }
}
