using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PrimeTween;
using TMPro;

public class Card : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    /// <summary>Fired when the card is clicked. Passes this card's upgrade offer.</summary>
    public event Action<UpgradeOffer> Clicked;

    [Header("Sounds")]
    [SerializeField] private FmodEventAsset selectSound;
    [SerializeField] private FmodEventAsset clickSound;

    [Header("Selection animation")]
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float animationDuration = 0.15f;

    [Header("Text (TextMeshPro)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Optional. If set, shows the upgrade's rarity (e.g. Common, Rare). Color is set from rarity's Display Color.")]
    [SerializeField] private TMP_Text rarityText;

    [Header("Rarity visualisation")]
    [Tooltip("Optional. If set, this image is tinted with the rarity's Display Color (e.g. card border or background).")]
    [SerializeField] private Image rarityBorderImage;

    private Button button;
    private Vector3 normalScale;
    private Tween scaleTween;
    private UpgradeOffer upgradeOffer;

    private void Awake()
    {
        button = GetComponent<Button>();
        normalScale = transform.localScale;

        if (button != null)
            button.onClick.AddListener(OnCardClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnCardClicked);
    }

    /// <summary>Initialize the card with an upgrade offer. Call after spawning.</summary>
    public void Initialize(UpgradeOffer offer)
    {
        upgradeOffer = offer;
        if (offer == null) return;

        if (titleText != null)
            titleText.text = offer.DisplayName;
        if (descriptionText != null)
            descriptionText.text = offer.Description;
        if (rarityText != null)
        {
            rarityText.text = offer.RarityName;
            if (offer.Rarity != null)
                rarityText.color = offer.Rarity.DisplayColor;
        }
        if (rarityBorderImage != null && offer.Rarity != null)
            rarityBorderImage.color = offer.Rarity.DisplayColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlaySelectSound();
        AnimateScale(selectedScale);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        AnimateScale(1f);
    }

    private void AnimateScale(float targetScale)
    {
        scaleTween.Stop();
        var target = normalScale * targetScale;
        scaleTween = Tween.Scale(transform, target, animationDuration, Ease.OutQuad);
    }

    private void PlaySelectSound()
    {
        if (selectSound != null && AudioService.Instance != null)
            AudioService.Instance.PlayOneShot(selectSound);
    }

    private void OnCardClicked()
    {
        if (clickSound != null && AudioService.Instance != null)
            AudioService.Instance.PlayOneShot(clickSound);
        Clicked?.Invoke(upgradeOffer);
    }
}
