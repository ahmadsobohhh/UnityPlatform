using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Firebase.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

public class JointClassManager : MonoBehaviour
{
    public TMP_InputField codeInput;
    public string sceneToLoad = "StudentHub";

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        Debug.Log("Scene to load is: " + sceneToLoad);
    }

    public async void JoinClassByCode()
    {
        string enteredCode = codeInput.text.Trim();

        if (string.IsNullOrEmpty(enteredCode))
        {
            Debug.Log("Please enter a class code.");
            return;
        }

        Debug.Log("Entered code: " + enteredCode);

        try
        {
            var snapshot = await db.Collection("classes")
                .WhereEqualTo("code", enteredCode)
                .GetSnapshotAsync();

            if (snapshot.Count <= 0)
            {
                Debug.Log("Wrong code. No class found.");
                return;
            }

            DocumentSnapshot classDoc = null;

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                classDoc = doc;
                break;
            }

            if (classDoc == null)
            {
                Debug.Log("No class document found.");
                return;
            }

            string classId = classDoc.Id;
            string className = classDoc.GetValue<string>("name");
            string classCode = classDoc.GetValue<string>("code");

            Debug.Log("Class found!");
            Debug.Log("Doc ID: " + classId);
            Debug.Log("Class Name: " + className);
            Debug.Log("Class Code: " + classCode);

            var user = auth.CurrentUser;

            if (user == null)
            {
                Debug.LogError("No user logged in.");
                return;
            }

            string userId = user.UserId;

            string firstName = "";
            string lastName = "";

            // Read student profile so class members can store display-friendly names.
            var userDoc = await db.Collection("users").Document(userId).GetSnapshotAsync();
            if (userDoc.Exists)
            {
                if (userDoc.ContainsField("firstName")) firstName = userDoc.GetValue<string>("firstName");
                if (userDoc.ContainsField("lastName")) lastName = userDoc.GetValue<string>("lastName");
            }

            // ✅ Add class to user's classes
            await db.Collection("users")
                .Document(userId)
                .Collection("classes")
                .Document(classId)
                .SetAsync(new Dictionary<string, object>
                {
                    { "name", className },
                    { "code", classCode }
                });

            // ✅ Add user to class members
            await db.Collection("classes")
                .Document(classId)
                .Collection("members")
                .Document(userId)
                .SetAsync(new Dictionary<string, object>
                {
                    { "joinedAt", Timestamp.GetCurrentTimestamp() },
                    { "firstName", firstName ?? "" },
                    { "lastName", lastName ?? "" }
                });

            Debug.Log("Successfully joined class!");

            // (Optional but useful)
            PlayerPrefs.SetString("JoinedClassDocId", classId);
            PlayerPrefs.SetString("JoinedClassName", className);
            PlayerPrefs.SetString("JoinedClassCode", classCode);
            PlayerPrefs.Save();

            // ✅ Go back to Student Hub (it will reload classes)
            SceneManager.LoadScene(sceneToLoad);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error joining class: " + e);
        }
    }

    public void TestLoadScene()
    {
        Debug.Log("TEST loading classroom");
        SceneManager.LoadScene(sceneToLoad);
    }
}