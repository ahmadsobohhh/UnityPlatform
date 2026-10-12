using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>Wind movement affects sails, never the ship's solid collision surfaces.</summary>
    public sealed class AuroraSailWind : MonoBehaviour
    {
        private Quaternion restingRotation;
        private float phase;
        private void Awake()
        {
            restingRotation = transform.localRotation;
            phase = transform.position.z * 0.23f;
        }
        private void Update()
        {
            var sway = Mathf.Sin(Time.time * 0.55f + phase) * 0.6f;
            transform.localRotation = restingRotation * Quaternion.Euler(0f, sway, sway * 0.3f);
        }
    }

    public sealed class AuroraLanternFlicker : MonoBehaviour
    {
        private Light lantern;
        private float baseIntensity;
        private void Awake()
        {
            lantern = GetComponent<Light>();
            baseIntensity = lantern == null ? 1f : lantern.intensity;
        }
        private void Update()
        {
            if (lantern != null)
                lantern.intensity = baseIntensity * (0.93f + Mathf.PerlinNoise(transform.position.x + Time.time * 2f, 0.7f) * 0.14f);
        }
    }

    public sealed class AuroraGullFlight : MonoBehaviour
    {
        private Transform leftWing;
        private Transform rightWing;
        private float phase;
        private Vector3 centre;
        public void Configure(Transform left, Transform right, float offset)
        {
            leftWing = left;
            rightWing = right;
            phase = offset;
            centre = new Vector3(0f, 10f + offset * 0.5f, 8f);
        }
        private void Update()
        {
            var angle = Time.time * 0.12f + phase;
            var radius = 22f + phase * 2f;
            transform.position = centre + new Vector3(Mathf.Sin(angle) * radius,
                Mathf.Sin(angle * 2f) * 0.6f, Mathf.Cos(angle) * radius);
            transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle)));
            var flap = Mathf.Sin(Time.time * 3.2f + phase) * 16f;
            leftWing.localRotation = Quaternion.Euler(0f, 0f, 12f + flap);
            rightWing.localRotation = Quaternion.Euler(0f, 0f, -12f - flap);
        }
    }
}
