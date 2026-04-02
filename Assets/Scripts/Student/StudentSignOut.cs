using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SignOutManager : MonoBehaviour
{
    public void SignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();

        Debug.Log("User signed out");

        SceneManager.LoadScene("Login"); 
    }
}