using UnityEngine;
using UnityEngine.EventSystems;
using PrimeTween;

/// <summary>
/// Same selection animation as UpgradePanel cards: scale up when selected, optional select sound.
/// Add to any selectable (Button, etc.) used in Death screen, Main menu, or other menus.
/// </summary>
public class MenuButtonSelectionAnimator : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Selection animation")]
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float animationDuration = 0.15f;

    [Header("Sounds")]
    [Tooltip("Optional. Played when the button is selected (keyboard/gamepad or hover).")]
    [SerializeField] private FmodEventAsset selectSound;

    private Vector3 normalScale;
    private Tween scaleTween;

    private void Awake()
    {
        normalScale = transform.localScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (selectSound != null && AudioService.Instance != null)
            AudioService.Instance.PlayOneShot(selectSound);
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
}
