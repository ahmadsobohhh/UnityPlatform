using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("Login");
    }
    
    public void Quit()
    {
        Application.Quit();
    }
}
