// Script: ButtonPulseEffect
// Path: Assets/Scripts/WelcomePage/ButtonPulseEffect.cs
// Purpose: Adds pulsing visual emphasis to key menu buttons.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonPulseEffect : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField] private float scaleAmount = 0.03f;
    [SerializeField] private float glowAmount = 0.15f;

    private Vector3 baseScale;
    private Color baseColor;
    private Image bg;
    private Outline outline;

    private void Awake()
    {
        baseScale = transform.localScale;
        bg = GetComponent<Image>();
        if (bg != null)
            baseColor = bg.color;

        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.5f, 0f);
            outline.effectDistance = new Vector2(3f, 3f);
        }
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1

        transform.localScale = baseScale * (1f + t * scaleAmount);

        if (bg != null)
        {
            float brightness = 1f + t * glowAmount;
            bg.color = new Color(
                Mathf.Min(baseColor.r * brightness, 1f),
                Mathf.Min(baseColor.g * brightness, 1f),
                Mathf.Min(baseColor.b * brightness, 1f),
                baseColor.a
            );
        }

        if (outline != null)
        {
            float alpha = t * 0.6f;
            outline.effectColor = new Color(1f, 0.85f, 0.5f, alpha);
        }
    }

    public void ResetBase()
    {
        baseScale = transform.localScale;
        if (bg != null) baseColor = bg.color;
    }
}


