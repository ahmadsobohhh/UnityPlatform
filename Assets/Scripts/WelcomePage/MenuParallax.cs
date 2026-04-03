using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    [Header("Mouse Parallax")]
    public float offsetMultiplier = 1f;
    public float smoothTime = 0.3f;

    [Header("Auto Drift")]
    public bool autoDrift = true;
    public float driftSpeed = 0.15f;
    public float driftAmount = 8f;

    private Vector2 startPosition;
    private Vector3 velocity;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        Vector2 mouseOffset = Vector2.zero;
        if (Camera.main != null)
            mouseOffset = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        Vector2 drift = Vector2.zero;
        if (autoDrift)
        {
            float t = Time.time * driftSpeed;
            drift = new Vector2(Mathf.Sin(t) * driftAmount, Mathf.Cos(t * 0.7f) * driftAmount * 0.5f);
        }

        Vector2 target = startPosition + (mouseOffset * offsetMultiplier) + drift;
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }
}
