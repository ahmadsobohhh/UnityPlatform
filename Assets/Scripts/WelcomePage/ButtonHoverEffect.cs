// Script: ButtonHoverEffect
// Path: Assets/Scripts/WelcomePage/ButtonHoverEffect.cs
// Purpose: Applies hover color and visual feedback on menu buttons.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float animSpeed = 10f;
    [SerializeField] private Color normalTextColor = new Color(0.85f, 0.82f, 0.75f, 1f);
    [SerializeField] private Color hoverTextColor = new Color(1f, 0.95f, 0.8f, 1f);
    [SerializeField] private Color pressTextColor = new Color(0.7f, 0.65f, 0.55f, 1f);

    private Vector3 baseScale;
    private float targetScale;
    private Color targetColor;
    private TMP_Text label;

    private void Awake()
    {
        baseScale = transform.localScale;
        targetScale = 1f;
        targetColor = normalTextColor;
        label = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        transform.localScale = baseScale;
        targetScale = 1f;
        targetColor = normalTextColor;
        if (label != null) label.color = normalTextColor;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScale, Time.deltaTime * animSpeed);
        if (label != null)
            label.color = Color.Lerp(label.color, targetColor, Time.deltaTime * animSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoverScale;
        targetColor = hoverTextColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = 1f;
        targetColor = normalTextColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressScale;
        targetColor = pressTextColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = hoverScale;
        targetColor = hoverTextColor;
    }
}


