// Script: MenuAnimator
// Path: Assets/Scripts/WelcomePage/MenuAnimator.cs
// Purpose: Animates welcome menu panels and transition states.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuAnimator : MonoBehaviour
{
    [Header("Fade In")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Button Entrance")]
    [SerializeField] private RectTransform[] menuButtons;
    [SerializeField] private float slideDistance = 300f;
    [SerializeField] private float slideDuration = 0.6f;
    [SerializeField] private float staggerDelay = 0.15f;

    [Header("Title")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private float titleFadeDuration = 1f;

    private void Start()
    {
        if (fadeOverlay == null)
        {
            var go = GameObject.Find("FadeOverlay");
            if (go != null) fadeOverlay = go.GetComponent<Image>();
        }
        StartCoroutine(PlayEntrance());
    }

    private IEnumerator PlayEntrance()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = Color.black;
        }

        if (titleGroup != null)
            titleGroup.alpha = 0f;

        foreach (var btn in menuButtons)
        {
            if (btn == null) continue;
            var group = btn.GetComponent<CanvasGroup>();
            if (group == null) group = btn.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        if (fadeOverlay != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, a);
                yield return null;
            }
            fadeOverlay.color = new Color(0, 0, 0, 0);
            fadeOverlay.gameObject.SetActive(false);
        }

        if (titleGroup != null)
        {
            float t = 0f;
            while (t < titleFadeDuration)
            {
                t += Time.deltaTime;
                titleGroup.alpha = Mathf.Lerp(0f, 1f, t / titleFadeDuration);
                yield return null;
            }
            titleGroup.alpha = 1f;
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            StartCoroutine(SlideIn(menuButtons[i], i * staggerDelay));
        }
    }

    private IEnumerator SlideIn(RectTransform target, float delay)
    {
        yield return new WaitForSeconds(delay);

        var group = target.GetComponent<CanvasGroup>();
        Vector2 startPos = target.anchoredPosition + Vector2.left * slideDistance;
        Vector2 endPos = target.anchoredPosition;

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float ease = EaseOutCubic(t / slideDuration);
            target.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            if (group != null) group.alpha = ease;
            yield return null;
        }

        target.anchoredPosition = endPos;
        if (group != null) group.alpha = 1f;
    }

    private static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}


