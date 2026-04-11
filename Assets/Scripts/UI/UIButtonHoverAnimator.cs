// Script: UIButtonHoverAnimator
// Path: Assets/Scripts/UI/UIButtonHoverAnimator.cs
// Purpose: Animates button scale, tint, and audio on hover/click.

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonHoverAnimator : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Motion")]
    [SerializeField] private float hoverScale = 1.045f;
    [SerializeField] private float pressScale = 0.97f;
    [SerializeField] private float liftPixels = 4f;
    [SerializeField] private float animationSpeed = 14f;

    [Header("Colors")]
    [SerializeField] private bool tintBackground = true;
    [SerializeField] private Color hoverTint = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color pressTint = new Color(0.92f, 0.9f, 0.85f, 1f);
    [SerializeField] private bool tintLabels = true;
    [SerializeField] private Color hoverLabelColor = new Color(1f, 0.96f, 0.84f, 1f);
    [SerializeField] private Color pressLabelColor = new Color(0.88f, 0.82f, 0.7f, 1f);

    [Header("Audio")]
    [SerializeField] private bool playHoverSfx = true;
    [SerializeField] private bool playClickSfx = true;

    private Button button;
    private RectTransform rectTransform;
    private Image background;
    private TMP_Text[] labels;

    private Vector3 baseScale;
    private Vector2 baseAnchoredPos;
    private Color baseBgColor;
    private Color[] baseLabelColors;

    private bool isHovering;
    private bool isPressed;

    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = transform as RectTransform;
        background = GetComponent<Image>();
        labels = GetComponentsInChildren<TMP_Text>(true);

        CacheBaseVisuals();
    }

    private void OnEnable()
    {
        CacheBaseVisuals();
        isHovering = false;
        isPressed = false;
        ApplyImmediateState();
    }

    private void CacheBaseVisuals()
    {
        baseScale = transform.localScale;
        if (rectTransform != null)
            baseAnchoredPos = rectTransform.anchoredPosition;

        if (background != null)
            baseBgColor = background.color;

        if (labels == null)
            labels = GetComponentsInChildren<TMP_Text>(true);

        baseLabelColors = new Color[labels.Length];
        for (int i = 0; i < labels.Length; i++)
            baseLabelColors[i] = labels[i] != null ? labels[i].color : Color.white;
    }

    private void Update()
    {
        if (!IsInteractable())
        {
            isHovering = false;
            isPressed = false;
        }

        float dt = Time.unscaledDeltaTime;

        float targetScale = 1f;
        float targetLift = 0f;
        if (isPressed)
            targetScale = pressScale;
        else if (isHovering)
        {
            targetScale = hoverScale;
            targetLift = liftPixels;
        }

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            baseScale * targetScale,
            dt * animationSpeed);

        if (rectTransform != null)
        {
            Vector2 targetPos = baseAnchoredPos + new Vector2(0f, targetLift);
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                targetPos,
                dt * animationSpeed);
        }

        if (tintBackground && background != null)
        {
            Color targetBg = baseBgColor;
            if (isPressed)
                targetBg = pressTint;
            else if (isHovering)
                targetBg = hoverTint;

            background.color = Color.Lerp(background.color, targetBg, dt * animationSpeed);
        }

        if (tintLabels && labels != null)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null)
                    continue;

                Color target = baseLabelColors != null && i < baseLabelColors.Length
                    ? baseLabelColors[i]
                    : Color.white;

                if (isPressed)
                    target = pressLabelColor;
                else if (isHovering)
                    target = hoverLabelColor;

                labels[i].color = Color.Lerp(labels[i].color, target, dt * animationSpeed);
            }
        }
    }

    private bool IsInteractable()
    {
        return button != null && button.IsInteractable() && button.gameObject.activeInHierarchy;
    }

    private void ApplyImmediateState()
    {
        transform.localScale = baseScale;
        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPos;

        if (background != null)
            background.color = baseBgColor;

        if (labels != null && baseLabelColors != null)
        {
            for (int i = 0; i < labels.Length && i < baseLabelColors.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].color = baseLabelColors[i];
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        isHovering = true;
        if (playHoverSfx && AudioManager.Instance != null)
            AudioManager.Instance.PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable() || eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        isPressed = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable() || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (playClickSfx && AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();
    }
}


