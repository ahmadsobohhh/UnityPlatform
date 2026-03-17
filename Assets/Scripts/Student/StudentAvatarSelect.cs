using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class StudentAvatarSelect : MonoBehaviour
{
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
    }

    public void SelectAvatar(string avatarId)
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

        db.Collection("users")
          .Document(user.UserId)
          .UpdateAsync(updates);

        SceneManager.LoadScene("StudentHub");
    }
}