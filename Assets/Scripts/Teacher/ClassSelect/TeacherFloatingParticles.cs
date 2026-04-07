using UnityEngine;
using UnityEngine.UI;

public class TeacherFloatingParticles : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int particleCount = 30;
    [SerializeField] private float minSize = 3f;
    [SerializeField] private float maxSize = 9f;
    [SerializeField] private float minRiseSpeed = 10f;
    [SerializeField] private float maxRiseSpeed = 40f;
    [SerializeField] private float driftRange = 35f;
    [SerializeField] private Color colorA = new Color(1f, 0.85f, 0.4f, 0.5f);
    [SerializeField] private Color colorB = new Color(0.9f, 0.65f, 0.2f, 0.35f);

    private struct Particle
    {
        public float x, y;
        public float speed;
        public float size;
        public float driftPhase;
        public float driftSpeed;
        public float flickerPhase;
        public Color color;
    }

    private Particle[] particles;
    private Image[] particleImages;
    private float canvasW, canvasH;

    private void Awake()
    {
        CacheCanvasSize();
        RebuildParticles();
    }

    private void OnEnable()
    {
        EnsureValid();
    }

    private void Update()
    {
        if (particles == null || particleImages == null || particles.Length != particleImages.Length)
        {
            EnsureValid();
            if (particles == null || particleImages == null) return;
        }

        float halfH = canvasH * 0.55f;
        float halfW = canvasW * 0.55f;

        for (int i = 0; i < particles.Length; i++)
        {
            if (particleImages[i] == null)
            {
                EnsureValid();
                return;
            }

            var p = particles[i];

            p.y += p.speed * Time.deltaTime;
            float xDrift = Mathf.Sin(Time.time * p.driftSpeed + p.driftPhase) * driftRange;

            if (p.y > halfH)
            {
                p.y = -halfH;
                p.x = Random.Range(-halfW, halfW);
                p.driftPhase = Random.Range(0f, Mathf.PI * 2f);
            }

            particles[i] = p;

            var rect = particleImages[i].rectTransform;
            rect.anchoredPosition = new Vector2(p.x + xDrift, p.y);

            float flicker = (Mathf.Sin(Time.time * 3.5f + p.flickerPhase) + 1f) * 0.5f;
            Color c = p.color;
            c.a = p.color.a * (0.3f + flicker * 0.7f);
            particleImages[i].color = c;
        }
    }

    private void CacheCanvasSize()
    {
        var canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        canvasW = canvasRect != null ? Mathf.Max(1f, canvasRect.rect.width) : 1920f;
        canvasH = canvasRect != null ? Mathf.Max(1f, canvasRect.rect.height) : 1080f;
    }

    private void EnsureValid()
    {
        particleCount = Mathf.Clamp(particleCount, 0, 500);
        if (particles != null && particleImages != null
            && particles.Length == particleCount && particleImages.Length == particleCount)
            return;

        CacheCanvasSize();
        RebuildParticles();
    }

    private void DestroyChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null && c.name.StartsWith("Particle_", System.StringComparison.Ordinal))
                Destroy(c.gameObject);
        }

        particles = null;
        particleImages = null;
    }

    private void RebuildParticles()
    {
        DestroyChildren();

        particleCount = Mathf.Clamp(particleCount, 0, 500);
        particles = new Particle[particleCount];
        particleImages = new Image[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject($"Particle_{i}");
            go.transform.SetParent(transform, false);

            var rect = go.AddComponent<RectTransform>();
            float size = Random.Range(minSize, maxSize);
            rect.sizeDelta = new Vector2(size, size);

            var img = go.AddComponent<Image>();
            Color c = Color.Lerp(colorA, colorB, Random.value);
            img.color = c;
            img.raycastTarget = false;

            particleImages[i] = img;

            particles[i] = new Particle
            {
                x = Random.Range(-canvasW * 0.5f, canvasW * 0.5f),
                y = Random.Range(-canvasH * 0.6f, canvasH * 0.6f),
                speed = Random.Range(minRiseSpeed, maxRiseSpeed),
                size = size,
                driftPhase = Random.Range(0f, Mathf.PI * 2f),
                driftSpeed = Random.Range(0.3f, 1.0f),
                flickerPhase = Random.Range(0f, Mathf.PI * 2f),
                color = c
            };
        }
    }
}
