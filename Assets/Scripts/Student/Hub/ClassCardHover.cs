using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ClassCardHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressScale = 0.96f;
    [SerializeField] private float animSpeed = 10f;

    [Header("Card Colors")]
    [SerializeField] private Color normalBg = new Color(1f, 1f, 1f, 0.06f);
    [SerializeField] private Color hoverBg = new Color(1f, 1f, 1f, 0.14f);
    [SerializeField] private Color pressBg = new Color(1f, 1f, 1f, 0.03f);
    [SerializeField] private Color normalText = new Color(0.92f, 0.88f, 0.80f, 1f);
    [SerializeField] private Color hoverText = new Color(1f, 0.97f, 0.88f, 1f);

    [Header("Gold Glow")]
    [SerializeField] private Color glowColor = new Color(0.95f, 0.80f, 0.45f, 0f);
    [SerializeField] private float hoverGlowAlpha = 0.4f;

    private Vector3 baseScale;
    private float targetScaleMul = 1f;
    private Color targetBg;
    private Color targetText;
    private float targetGlowAlpha;

    private Image bgImage;
    private TMP_Text label;
    private Outline outline;

    private void Awake()
    {
        baseScale = transform.localScale;
        bgImage = GetComponent<Image>();
        label = GetComponentInChildren<TMP_Text>();
        outline = GetComponent<Outline>();

        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2f, 2f);
        }
        outline.effectColor = glowColor;

        targetBg = normalBg;
        targetText = normalText;

        if (bgImage != null) bgImage.color = normalBg;
        if (label != null) label.color = normalText;
    }

    private void Update()
    {
        float dt = Time.deltaTime * animSpeed;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScaleMul, dt);

        if (bgImage != null)
            bgImage.color = Color.Lerp(bgImage.color, targetBg, dt);
        if (label != null)
            label.color = Color.Lerp(label.color, targetText, dt);
        if (outline != null)
        {
            Color c = outline.effectColor;
            c.a = Mathf.Lerp(c.a, targetGlowAlpha, dt);
            outline.effectColor = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScaleMul = hoverScale;
        targetBg = hoverBg;
        targetText = hoverText;
        targetGlowAlpha = hoverGlowAlpha;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScaleMul = 1f;
        targetBg = normalBg;
        targetText = normalText;
        targetGlowAlpha = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScaleMul = pressScale;
        targetBg = pressBg;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScaleMul = hoverScale;
        targetBg = hoverBg;
    }
}
