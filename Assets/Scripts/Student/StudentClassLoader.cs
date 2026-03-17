using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using TMPro;

public class StudentClassLoader : MonoBehaviour
{
    public Transform classContainer;
    public GameObject classButtonPrefab;

    FirebaseFirestore db;
    FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        LoadClasses();
    }

    void LoadClasses()
    {
        var user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No user logged in.");
            return;
        }

        db.Collection("users")
          .Document(user.UserId)
          .Collection("classes")
          .GetSnapshotAsync()
          .ContinueWith(task =>
          {
              if (task.IsCompleted)
              {
                  foreach (var doc in task.Result.Documents)
                  {
                      CreateClassButton(doc.Id);
                  }
              }
          });
    }

    void CreateClassButton(string className)
    {
        GameObject button = Instantiate(classButtonPrefab, classContainer);
        button.GetComponentInChildren<TMP_Text>().text = className;
    }
}