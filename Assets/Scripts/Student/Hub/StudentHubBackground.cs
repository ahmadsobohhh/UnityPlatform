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

        var canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        canvasW = canvasRect != null ? canvasRect.rect.width : 1920f;
        canvasH = canvasRect != null ? canvasRect.rect.height : 1080f;

        SpawnEmbers();
    }

    private void Update()
    {
        UpdateParallax();
        UpdateEmbers();
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

    private void SpawnEmbers()
    {
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

    private void UpdateEmbers()
    {
        float halfH = canvasH * 0.55f;
        float halfW = canvasW * 0.55f;

        for (int i = 0; i < embers.Length; i++)
        {
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
