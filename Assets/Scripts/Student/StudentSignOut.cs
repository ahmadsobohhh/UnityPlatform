using Firebase.Auth;
using UnityEngine;

public class SignOutManager : MonoBehaviour
{
    public void SignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("User signed out");
        SceneTransition.LoadScene("WelcomePage");
    }
}