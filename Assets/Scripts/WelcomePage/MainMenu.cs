// Script: MainMenu
// Path: Assets/Scripts/WelcomePage/MainMenu.cs
// Purpose: Routes top-level main menu button actions.

using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowLogin();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void ShowRegister()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowRegister();
    }

    public void ShowLogin()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowLogin();
    }

    public void BackToMenu()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowMainMenu();
    }
}


