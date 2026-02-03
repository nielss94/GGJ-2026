using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Upgrade panel UI. Open from anywhere via UpgradePanel.Instance.Open().
/// Spawns cards from the upgrade database (random offers); subscribes to card click and closes on choice.
/// Assign the Upgrade Database asset in the inspector.
/// </summary>
public class UpgradePanel : MonoBehaviour
{
    private static UpgradePanel instance;

    /// <summary>
    /// Singleton; resolved lazily so the panel can start disabled and still be found when opening.
    /// </summary>
    public static UpgradePanel Instance =>
        instance != null ? instance : (instance = FindFirstObjectByType<UpgradePanel>(FindObjectsInactive.Include));

    [Header("Database")]
    [Tooltip("Designer-created database of rarities and upgrades. Used to draw random offers when opening the panel.")]
    [SerializeField] private UpgradeDatabase upgradeDatabase;

    [Header("Cards")]
    [SerializeField] private Card cardPrefab;
    [SerializeField] private RectTransform cardContainer;

    [Header("Offers")]
    [Tooltip("Number of upgrade cards to show when opening the panel.")]
    [SerializeField] private int cardCount = 3;
    [Tooltip("Ratio of ability vs stat upgrades (0 = all stat, 1 = all ability).")]
    [SerializeField][Range(0f, 1f)] private float abilityRatio = 0.5f;

    [Header("Sounds")]
    [SerializeField] private FmodEventAsset openSound;

    private readonly List<Card> spawnedCards = new List<Card>();
    private bool hasChosen;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[UpgradePanel] Duplicate instance on {gameObject.name}; keeping existing.");
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
        UnsubscribeAndClearCards();
    }

    public void Open()
    {
        hasChosen = false;
        gameObject.SetActive(true);
        EventBus.RaiseGameplayPaused();
        EventBus.RaisePlayerInputBlockRequested(this);
        PlayOpenSound();
        SpawnCards();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        EventBus.RaiseGameplayResumed();
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
    /// Returns the list of upgrade offers to show (from database or override). Uses database random draw by default.
    /// </summary>
    protected virtual IReadOnlyList<UpgradeOffer> GetAvailableUpgrades()
    {
        if (upgradeDatabase != null)
            return upgradeDatabase.GetRandomUpgrades(cardCount, abilityRatio);
        return new List<UpgradeOffer>();
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

        IReadOnlyList<UpgradeOffer> offers = GetAvailableUpgrades();
        for (int i = 0; i < offers.Count; i++)
        {
            UpgradeOffer offer = offers[i];
            if (offer == null) continue;
            Card card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(offer);
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

    private void Update()
    {
        if (!gameObject.activeSelf || spawnedCards.Count == 0)
            return;
        if (MenuKeyboardNavigation.IsSelectionInMenu(transform))
            return;
        if (!MenuKeyboardNavigation.WasNavigationOrSubmitPressed())
            return;
        SelectDefaultCard();
    }

    private void OnCardChosen(UpgradeOffer chosen)
    {
        if (hasChosen || chosen == null)
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
