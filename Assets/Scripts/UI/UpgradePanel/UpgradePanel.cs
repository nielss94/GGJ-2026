using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Upgrade panel UI. Open from anywhere via UpgradePanel.Instance.Open().
/// Spawns cards from available upgrades; subscribes to card click and closes on choice.
/// Attach to the root GameObject of the upgrade panel.
/// </summary>
public class UpgradePanel : MonoBehaviour
{
    private static UpgradePanel _instance;

    /// <summary>
    /// Singleton; resolved lazily so the panel can start disabled and still be found when opening.
    /// </summary>
    public static UpgradePanel Instance =>
        _instance != null ? _instance : (_instance = FindFirstObjectByType<UpgradePanel>(FindObjectsInactive.Include));

    [Header("Cards")]
    [SerializeField] private Card cardPrefab;
    [SerializeField] private RectTransform cardContainer;

    [Header("Sounds")]
    [SerializeField] private FmodEventAsset openSound;

    private readonly List<Card> spawnedCards = new List<Card>();
    private bool hasChosen;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[UpgradePanel] Duplicate instance on {gameObject.name}; keeping existing.");
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        UnsubscribeAndClearCards();
    }

    public void Open()
    {
        hasChosen = false;
        gameObject.SetActive(true);
        EventBus.RaisePlayerInputBlockRequested(this);
        PlayOpenSound();
        SpawnCards();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        EventBus.RaisePlayerInputUnblockRequested(this);
        UnsubscribeAndClearCards();
    }

    public void Toggle()
    {
        if (gameObject.activeSelf)
            Close();
        else
            Open();
    }

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>
    /// Returns the list of upgrades to offer. Replace with real logic later (e.g. from run state, unlocks).
    /// </summary>
    protected virtual IReadOnlyList<UpgradeType> GetAvailableUpgrades()
    {
        return new[] { UpgradeType.Damage, UpgradeType.Health, UpgradeType.Speed };
    }

    private void PlayOpenSound()
    {
        if (openSound != null && AudioService.Instance != null)
            AudioService.Instance.PlayOneShot(openSound);
    }

    private void SpawnCards()
    {
        if (cardPrefab == null || cardContainer == null)
            return;

        UnsubscribeAndClearCards();

        IReadOnlyList<UpgradeType> upgrades = GetAvailableUpgrades();
        for (int i = 0; i < upgrades.Count; i++)
        {
            UpgradeType type = upgrades[i];
            Card card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(type, title: type.ToString());
            card.Clicked += OnCardChosen;
            spawnedCards.Add(card);
        }

        SelectDefaultCard();
    }

    private void SelectDefaultCard()
    {
        if (spawnedCards.Count == 0)
            return;
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;
        GameObject toSelect = spawnedCards.Count % 2 == 1 && spawnedCards.Count > 1
            ? spawnedCards[spawnedCards.Count / 2].gameObject
            : spawnedCards[0].gameObject;
        eventSystem.SetSelectedGameObject(toSelect);
    }

    private void OnCardChosen(UpgradeType chosen)
    {
        if (hasChosen)
            return;
        hasChosen = true;
        EventBus.RaiseUpgradeChosen(chosen);
        Close();
    }

    private void UnsubscribeAndClearCards()
    {
        foreach (Card card in spawnedCards)
        {
            if (card != null)
                card.Clicked -= OnCardChosen;
            if (card != null)
                Destroy(card.gameObject);
        }
        spawnedCards.Clear();
    }
}
