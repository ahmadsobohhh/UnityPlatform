using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/* This script allows navigation between input fields using the Tab key*/
public class ChangeInput : MonoBehaviour
{
    EventSystem system;
    public Selectable firstInput; // First input field to select on start
    public Button submitButton; // Button to invoke on Enter key press
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        system = EventSystem.current;
        firstInput.Select(); // Select the first input field on start
    }

    // Update is called once per frame
    void Update()
    {
        // Navigate between input fields using Tab and Shift+Tav
        // Shift+Tab to go to the previous input field
        if (Input.GetKeyDown(KeyCode.Tab) && Input.GetKey(KeyCode.LeftShift))
        {
            Selectable previous = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp();
            if (previous != null)
            {
                previous.Select();
            }
        }
        // Tab to go to the next input field
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
            if (next != null)
            {
                next.Select();
            }
        }
    }
}
