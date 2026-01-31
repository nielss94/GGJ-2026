using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PrimeTween;
using TMPro;

public class Card : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    /// <summary>Fired when the card is clicked. Passes this card's upgrade type.</summary>
    public event Action<UpgradeType> Clicked;

    [Header("Sounds")]
    [SerializeField] private FmodEventAsset selectSound;
    [SerializeField] private FmodEventAsset clickSound;

    [Header("Selection animation")]
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float animationDuration = 0.15f;

    [Header("Text (TextMeshPro)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private Button button;
    private Vector3 normalScale;
    private Tween scaleTween;
    private UpgradeType upgradeType;

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

    /// <summary>Initialize the card with data for the given upgrade type. Call after spawning.</summary>
    public void Initialize(UpgradeType type, string title = null)
    {
        upgradeType = type;
        string displayTitle = !string.IsNullOrEmpty(title) ? title : GetTitleForType(type);
        string description = GetDescriptionForType(type);

        if (titleText != null)
            titleText.text = displayTitle;
        if (descriptionText != null)
            descriptionText.text = description;
    }

    private static string GetTitleForType(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.Damage => "Damage",
            UpgradeType.Health => "Health",
            UpgradeType.Speed => "Speed",
            _ => type.ToString()
        };
    }

    private static string GetDescriptionForType(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.Damage => "Increase your damage output.",
            UpgradeType.Health => "Increase your maximum health.",
            UpgradeType.Speed => "Move faster.",
            _ => string.Empty
        };
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
        Clicked?.Invoke(upgradeType);
    }
}
