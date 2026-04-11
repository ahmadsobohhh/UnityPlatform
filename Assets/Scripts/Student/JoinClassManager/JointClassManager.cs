// Script: JointClassManager
// Path: Assets/Scripts/Student/JoinClassManager/JointClassManager.cs
// Purpose: Validates class join input and enrolls students into classes.

using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Firebase.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class JointClassManager : MonoBehaviour
{
    public TMP_InputField codeInput;
    public string sceneToLoad = "StudentHub";
    [SerializeField] private GameObject joinPopup;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private bool stayInCurrentSceneWhenPossible = true;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private bool isJoining;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        ResolveJoinPopupReference();

        Debug.Log("Scene to load is: " + sceneToLoad);
    }

    private void ResolveJoinPopupReference()
    {
        if (joinPopup != null)
            return;

        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
            return;

        foreach (var root in activeScene.GetRootGameObjects())
        {
            var allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t != null && t.name == "JoinGUI")
                {
                    joinPopup = t.gameObject;
                    return;
                }
            }
        }
    }

    public async void JoinClassByCode()
    {
        if (isJoining)
            return;

        isJoining = true;

        string enteredCode = codeInput.text.Trim();

        if (string.IsNullOrEmpty(enteredCode))
        {
            Debug.Log("Please enter a class code.");
            SetFeedback("Enter a class code first.");
            isJoining = false;
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
                SetFeedback("No class found with that code.");
                isJoining = false;
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
                SetFeedback("Could not load class details.");
                isJoining = false;
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
                SetFeedback("You must be logged in to join.");
                isJoining = false;
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

            SetFeedback("Joined successfully!");

            bool alreadyInTargetScene = SceneManager.GetActiveScene().name == sceneToLoad;
            if (stayInCurrentSceneWhenPossible && alreadyInTargetScene)
            {
                if (codeInput != null)
                    codeInput.SetTextWithoutNotify("");

                var popupController = FindFirstObjectByType<LoadJoinClassScene>();
                if (popupController != null)
                    popupController.CloseJoinClassPopup();
                else if (joinPopup != null)
                    joinPopup.SetActive(false);

                var loader = FindFirstObjectByType<StudentClassLoader>();
                if (loader != null)
                    loader.ReloadClassesAfterJoin();

                isJoining = false;
                return;
            }

            // In non-hub scenes, keep old fallback behavior.
            if (!alreadyInTargetScene)
                SceneManager.LoadScene(sceneToLoad);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error joining class: " + e);
            SetFeedback("Join failed. Please try again.");
        }
        finally
        {
            isJoining = false;
        }
    }

    private void SetFeedback(string msg)
    {
        if (feedbackText != null)
            feedbackText.text = msg;
    }

    public void TestLoadScene()
    {
        Debug.Log("TEST loading classroom");
        SceneManager.LoadScene(sceneToLoad);
    }
}

