using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using TMPro;

public class StudentClassLoader : MonoBehaviour
{
    public Transform classContainer;
    public GameObject classButtonPrefab;
    public GameObject noClassesText;

    FirebaseFirestore db;
    FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        LoadClasses();
    }

    async void LoadClasses()
    {
        var user = auth.CurrentUser;

        // ✅ Always assume no classes first (show text by default)
        if (noClassesText != null)
            noClassesText.SetActive(true);

        if (user == null)
        {
            Debug.LogError("No user logged in.");
            return;
        }

        try
        {
            var snapshot = await db.Collection("users")
                .Document(user.UserId)
                .Collection("classes")
                .GetSnapshotAsync();

            // Clear existing buttons
            foreach (Transform child in classContainer)
            {
                Destroy(child.gameObject);
            }

            if (snapshot.Count == 0)
            {
                Debug.Log("No classes found");
                if (noClassesText != null)
                    noClassesText.SetActive(true);
                return;
            }

            
            if (noClassesText != null)
                noClassesText.SetActive(false);

            foreach (var doc in snapshot.Documents)
            {
                CreateClassButton(doc.GetValue<string>("className"));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load classes: " + e.Message);

            // Keep text visible as fallback
            if (noClassesText != null)
                noClassesText.SetActive(true);

            // Clear UI just in case
            foreach (Transform child in classContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    void CreateClassButton(string className)
    {
        GameObject button = Instantiate(classButtonPrefab, classContainer);
        button.GetComponentInChildren<TMP_Text>().text = className;
    }
}