using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StudentHubAnimator : MonoBehaviour
{
    [Header("Scene Fade")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("Title")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private float titleFadeDuration = 0.8f;

    [Header("Parchment")]
    [SerializeField] private RectTransform parchmentRect;
    [SerializeField] private float parchmentScaleFrom = 0.85f;
    [SerializeField] private float parchmentDuration = 0.7f;

    [Header("Buttons")]
    [SerializeField] private RectTransform[] buttons;
    [SerializeField] private float btnSlideDuration = 0.5f;
    [SerializeField] private float btnSlideDistance = 60f;
    [SerializeField] private float btnStagger = 0.1f;

    [Header("Class Cards")]
    [SerializeField] private Transform classContainer;
    [SerializeField] private float cardStartDelay = 0.5f;
    [SerializeField] private float cardFadeDuration = 0.4f;
    [SerializeField] private float cardSlideUp = 40f;
    [SerializeField] private float cardStagger = 0.07f;

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

        if (titleGroup != null) titleGroup.alpha = 0f;

        if (parchmentRect != null)
        {
            var pg = parchmentRect.GetComponent<CanvasGroup>();
            if (pg == null) pg = parchmentRect.gameObject.AddComponent<CanvasGroup>();
            pg.alpha = 0f;
        }

        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            var g = btn.GetComponent<CanvasGroup>();
            if (g == null) g = btn.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
        }

        HideCards();

        // Fade from black
        if (fadeOverlay != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeOverlay.color = new Color(0, 0, 0, Mathf.Lerp(1f, 0f, t / fadeDuration));
                yield return null;
            }
            fadeOverlay.color = new Color(0, 0, 0, 0);
            fadeOverlay.gameObject.SetActive(false);
        }

        // Title fade in
        if (titleGroup != null)
        {
            float t = 0f;
            while (t < titleFadeDuration)
            {
                t += Time.deltaTime;
                titleGroup.alpha = EaseOutCubic(t / titleFadeDuration);
                yield return null;
            }
            titleGroup.alpha = 1f;
        }

        // Parchment scale + fade
        if (parchmentRect != null)
        {
            var pg = parchmentRect.GetComponent<CanvasGroup>();
            Vector3 endScale = parchmentRect.localScale;
            Vector3 startScale = endScale * parchmentScaleFrom;

            float t = 0f;
            while (t < parchmentDuration)
            {
                t += Time.deltaTime;
                float ease = EaseOutCubic(t / parchmentDuration);
                parchmentRect.localScale = Vector3.Lerp(startScale, endScale, ease);
                if (pg != null) pg.alpha = ease;
                yield return null;
            }
            parchmentRect.localScale = endScale;
            if (pg != null) pg.alpha = 1f;
        }

        // Buttons slide up
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                StartCoroutine(SlideUp(buttons[i], i * btnStagger, btnSlideDistance, btnSlideDuration));
        }

        yield return new WaitForSeconds(cardStartDelay);
        AnimateCards();
    }

    private void HideCards()
    {
        if (classContainer == null) return;
        foreach (Transform child in classContainer)
        {
            var g = child.GetComponent<CanvasGroup>();
            if (g == null) g = child.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
        }
    }

    public void AnimateCards()
    {
        if (classContainer == null) return;
        int i = 0;
        foreach (Transform child in classContainer)
        {
            StartCoroutine(SlideUp(child.GetComponent<RectTransform>(), i * cardStagger, cardSlideUp, cardFadeDuration));
            i++;
        }
    }

    private IEnumerator SlideUp(RectTransform target, float delay, float dist, float dur)
    {
        yield return new WaitForSeconds(delay);

        var group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();

        Vector2 startPos = target.anchoredPosition + Vector2.down * dist;
        Vector2 endPos = target.anchoredPosition;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ease = EaseOutCubic(t / dur);
            target.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            group.alpha = ease;
            yield return null;
        }

        target.anchoredPosition = endPos;
        group.alpha = 1f;
    }

    private static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
