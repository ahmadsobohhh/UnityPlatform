using UnityEngine;

/*This script creates a parallax effect for the background. 
    In this case, the clouds move based on our mouse movements.*/
public class MenuParallax : MonoBehaviour
{
    public float offsetMultiplier = 1f; // Adjust this value to control the intensity of the parallax effect
    public float smoothTime = 0.3f; // Smoothing time for the movement

    private Vector2 startPosition; // Initial background position
    private Vector3 velocity; // Velocity reference for SmoothDamp

    private void Start()
    {
        startPosition = transform.position; // Store the initial position of the background on start
    }

    private void Update()
    {
        // Get mouse position in viewport coordinates (0 to 1)
        Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition); 
        
        // Smoothly move the background based on mouse position
        transform.position = Vector3.SmoothDamp(transform.position, startPosition + (offset * offsetMultiplier), ref velocity, smoothTime); 
    }
}
