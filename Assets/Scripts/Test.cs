using System.Collections;
using UnityEngine;

public class Test : MonoBehaviour
{
    public FmodEventAsset testEvent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        EventBus.PlayerDied += OnPlayerDied;
    }

    void Start()
    {
        StartCoroutine(TestCoroutine());
    }
    IEnumerator TestCoroutine()
    {
        yield return new WaitForSeconds(1f);
        AudioService.Instance.PlayOneShot(testEvent);
    }

    void OnDisable()
    {
        EventBus.PlayerDied -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        AudioService.Instance.PlayOneShot(testEvent);
    }
}
