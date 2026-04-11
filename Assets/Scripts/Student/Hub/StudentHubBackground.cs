// Script: StudentHubBackground
// Path: Assets/Scripts/Student/Hub/StudentHubBackground.cs
// Purpose: Updates hub background visuals, motion, and ambience effects.

using UnityEngine;
using UnityEngine.UI;

public class StudentHubBackground : MonoBehaviour
{
    [Header("Parallax (Background Image)")]
    [SerializeField] private RectTransform backgroundRect;
    [SerializeField] private float parallaxStrength = 20f;
    [SerializeField] private float parallaxSmooth = 3f;

    [Header("Auto Drift")]
    [SerializeField] private bool autoDrift = true;
    [SerializeField] private float driftSpeed = 0.08f;
    [SerializeField] private float driftAmount = 12f;

    [Header("Floating Embers")]
    [SerializeField] private int emberCount = 25;
    [SerializeField] private float minEmberSize = 3f;
    [SerializeField] private float maxEmberSize = 8f;
    [SerializeField] private float minRiseSpeed = 15f;
    [SerializeField] private float maxRiseSpeed = 50f;
    [SerializeField] private float emberDriftRange = 40f;
    [SerializeField] private Color emberColorWarm = new Color(1f, 0.7f, 0.2f, 0.7f);
    [SerializeField] private Color emberColorHot = new Color(1f, 0.4f, 0.1f, 0.5f);

    private Vector2 bgStartPos;
    private Vector2 parallaxOffset;
    private Vector2 parallaxVelocity;

    private struct EmberData
    {
        public float x, y;
        public float speed;
        public float size;
        public float driftPhase;
        public float driftSpeed;
        public float flickerPhase;
        public Color color;
    }

    private EmberData[] embers;
    private Image[] emberImages;
    private float canvasW, canvasH;

    private void Awake()
    {
        if (backgroundRect != null)
            bgStartPos = backgroundRect.anchoredPosition;

        CacheCanvasSize();
        RebuildEmbers();
    }

    private void OnEnable()
    {
        // After a script recompile in Play Mode, instance fields can reset while Update still runs.
        EnsureEmbersValid();
    }

    private void Update()
    {
        UpdateParallax();
        UpdateEmbers();
    }

    void CacheCanvasSize()
    {
        var canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        canvasW = canvasRect != null ? Mathf.Max(1f, canvasRect.rect.width) : 1920f;
        canvasH = canvasRect != null ? Mathf.Max(1f, canvasRect.rect.height) : 1080f;
    }

    void EnsureEmbersValid()
    {
        emberCount = Mathf.Clamp(emberCount, 0, 500);
        if (embers != null && emberImages != null && embers.Length == emberCount && emberImages.Length == emberCount)
            return;

        CacheCanvasSize();
        RebuildEmbers();
    }

    void DestroyEmberChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null && c.name.StartsWith("Ember_", System.StringComparison.Ordinal))
                Destroy(c.gameObject);
        }

        embers = null;
        emberImages = null;
    }

    void RebuildEmbers()
    {
        DestroyEmberChildren();

        emberCount = Mathf.Clamp(emberCount, 0, 500);
        embers = new EmberData[emberCount];
        emberImages = new Image[emberCount];

        for (int i = 0; i < emberCount; i++)
        {
            var go = new GameObject($"Ember_{i}");
            go.transform.SetParent(transform, false);

            var rect = go.AddComponent<RectTransform>();
            float size = Random.Range(minEmberSize, maxEmberSize);
            rect.sizeDelta = new Vector2(size, size);

            var img = go.AddComponent<Image>();
            Color c = Color.Lerp(emberColorWarm, emberColorHot, Random.value);
            img.color = c;
            img.raycastTarget = false;

            emberImages[i] = img;

            embers[i] = new EmberData
            {
                x = Random.Range(-canvasW * 0.5f, canvasW * 0.5f),
                y = Random.Range(-canvasH * 0.6f, canvasH * 0.6f),
                speed = Random.Range(minRiseSpeed, maxRiseSpeed),
                size = size,
                driftPhase = Random.Range(0f, Mathf.PI * 2f),
                driftSpeed = Random.Range(0.3f, 1.2f),
                flickerPhase = Random.Range(0f, Mathf.PI * 2f),
                color = c
            };
        }
    }

    private void UpdateParallax()
    {
        if (backgroundRect == null) return;

        Vector2 mouseTarget = Vector2.zero;
        if (Camera.main != null)
        {
            Vector2 viewport = Camera.main.ScreenToViewportPoint(Input.mousePosition);
            mouseTarget = (viewport - new Vector2(0.5f, 0.5f)) * parallaxStrength;
        }

        Vector2 drift = Vector2.zero;
        if (autoDrift)
        {
            float t = Time.time * driftSpeed;
            drift = new Vector2(
                Mathf.Sin(t) * driftAmount,
                Mathf.Cos(t * 0.6f) * driftAmount * 0.4f);
        }

        Vector2 target = bgStartPos + mouseTarget + drift;
        parallaxOffset = Vector2.SmoothDamp(parallaxOffset, target - bgStartPos, ref parallaxVelocity, 1f / parallaxSmooth);
        backgroundRect.anchoredPosition = bgStartPos + parallaxOffset;
    }

    private void UpdateEmbers()
    {
        if (embers == null || emberImages == null || embers.Length != emberImages.Length)
        {
            EnsureEmbersValid();
            if (embers == null || emberImages == null)
                return;
        }

        float halfH = canvasH * 0.55f;
        float halfW = canvasW * 0.55f;

        for (int i = 0; i < embers.Length; i++)
        {
            if (emberImages[i] == null)
            {
                EnsureEmbersValid();
                return;
            }

            var e = embers[i];

            e.y += e.speed * Time.deltaTime;
            float xDrift = Mathf.Sin(Time.time * e.driftSpeed + e.driftPhase) * emberDriftRange;

            if (e.y > halfH)
            {
                e.y = -halfH;
                e.x = Random.Range(-halfW, halfW);
                e.driftPhase = Random.Range(0f, Mathf.PI * 2f);
            }

            embers[i] = e;

            var rect = emberImages[i].rectTransform;
            rect.anchoredPosition = new Vector2(e.x + xDrift, e.y);

            float flicker = (Mathf.Sin(Time.time * 4f + e.flickerPhase) + 1f) * 0.5f;
            Color c = e.color;
            c.a = e.color.a * (0.4f + flicker * 0.6f);
            emberImages[i].color = c;
        }
    }
}


