// Script: StudentSignOut
// Path: Assets/Scripts/Student/StudentSignOut.cs
// Purpose: Signs out the current student and routes back to welcome.

using Firebase.Auth;
using UnityEngine;

public class SignOutManager : MonoBehaviour
{
    public void SignOut()
    {
        ClassSelection.ClearForSignOut();
        ImagineQuest.Gameplay.StudentAvatarIdentity.Clear();
        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("User signed out");
        SceneTransition.LoadScene("WelcomePage");
    }
}

