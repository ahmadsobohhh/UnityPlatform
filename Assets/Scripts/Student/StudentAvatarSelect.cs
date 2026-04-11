// Script: StudentAvatarSelect
// Path: Assets/Scripts/Student/StudentAvatarSelect.cs
// Purpose: Saves the selected student avatar to the user profile.

using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public class StudentAvatarSelect : MonoBehaviour
{
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
    }

    public async void SelectAvatar(string avatarId)
    {
        var user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No logged in user.");
            return;
        }

        var updates = new Dictionary<string, object>
        {
            { "avatarId", avatarId },
            { "avatarChosen", true }
        };

        try
        {
            await db.Collection("users")
                .Document(user.UserId)
                .UpdateAsync(updates);

            Debug.Log("Avatar saved successfully.");

            SceneTransition.LoadScene("StudentHub");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save avatar: " + e);
        }
    }
}

