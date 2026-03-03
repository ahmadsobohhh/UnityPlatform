using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Clicking "Begin" takes the user to the Login scene
    public void Play()
    {
        SceneManager.LoadScene("Login");
    }
    
    // Clicking "Exit" quits the app
    public void Quit()
    {
        Application.Quit();
    }
}
