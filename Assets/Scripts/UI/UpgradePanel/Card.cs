using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PrimeTween;

public class Card : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Sounds")]
    [SerializeField] private FmodEventAsset selectSound;
    [SerializeField] private FmodEventAsset clickSound;

    [Header("Selection animation")]
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float animationDuration = 0.15f;

    private Button button;
    private Vector3 normalScale;
    private Tween scaleTween;

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
    }
}
