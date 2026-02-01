using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Listens to EventBus.DamageNumbersRequested and spawns damage number popups on a world-space canvas.
/// Can spawn all at once or scatter spawn times for a staggered effect.
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private DamageNumberPopup prefab;
    [Tooltip("Parent for spawned popups (e.g. world-space canvas or a child of it).")]
    [SerializeField] private Transform container;
    [Tooltip("World-space height above the hit point where numbers spawn (e.g. 0.3 = slightly above enemy).")]
    [SerializeField] private float spawnHeightOffset = 0.4f;

    [Header("Scatter (optional)")]
    [Tooltip("If true, spawn each number with a random delay within the scatter window for a staggered look.")]
    [SerializeField] private bool scatterSpawn;
    [Tooltip("Max delay in seconds before each spawn when scatter is enabled.")]
    [SerializeField] private float scatterWindow = 0.12f;

    private void OnEnable()
    {
        EventBus.DamageNumbersRequested += OnDamageNumbersRequested;
    }

    private void OnDisable()
    {
        EventBus.DamageNumbersRequested -= OnDamageNumbersRequested;
    }

    private void OnDamageNumbersRequested(IReadOnlyList<EventBus.DamageNumberHit> hits)
    {
        if (hits == null || hits.Count == 0 || prefab == null)
            return;

        Transform parent = container != null ? container : transform;

        if (scatterSpawn && scatterWindow > 0f && hits.Count > 1)
            StartCoroutine(SpawnScattered(hits, parent));
        else
            SpawnAll(hits, parent);
    }

    private void SpawnAll(IReadOnlyList<EventBus.DamageNumberHit> hits, Transform parent)
    {
        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            SpawnOne(hit, parent);
        }
    }

    private IEnumerator SpawnScattered(IReadOnlyList<EventBus.DamageNumberHit> hits, Transform parent)
    {
        for (int i = 0; i < hits.Count; i++)
        {
            float delay = Random.Range(0f, scatterWindow);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            SpawnOne(hits[i], parent);
        }
    }

    private void SpawnOne(EventBus.DamageNumberHit hit, Transform parent)
    {
        Vector3 spawnPos = hit.WorldPosition + Vector3.up * spawnHeightOffset;
        DamageNumberPopup instance = Instantiate(prefab, parent);
        instance.gameObject.SetActive(true);
        instance.Initialize(spawnPos, hit.Damage, hit.IsCrit);
    }
}
