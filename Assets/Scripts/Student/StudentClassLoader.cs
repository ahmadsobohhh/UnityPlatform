using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

public class StudentClassLoader : MonoBehaviour
{
    public Transform classContainer;
    public GameObject classButtonPrefab;
    public GameObject noClassesText;

    public string classSceneName = "StudentClass";

    FirebaseFirestore db;
    FirebaseAuth auth;

    async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        await Task.Delay(100);
        LoadClasses();
    }

    async void LoadClasses()
    {
        if (classContainer == null)
        {
            Debug.LogError("classContainer NOT assigned");
            return;
        }

        if (classButtonPrefab == null)
        {
            Debug.LogError("classButtonPrefab NOT assigned");
            return;
        }

        var user = auth.CurrentUser;

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

            // Clear old buttons
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

            var classIds = new List<string>();
            foreach (var doc in snapshot.Documents)
            {
                string className = doc.ContainsField("name")
                    ? doc.GetValue<string>("name")
                    : "Unnamed Class";

                string classCode = doc.ContainsField("code")
                    ? doc.GetValue<string>("code")
                    : "";

                string classId = doc.Id;
                classIds.Add(classId);

                CreateClassButton(className, classId, classCode);
            }

            _ = RepairMemberNames(user.UserId, classIds);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load classes: " + e.Message);

            if (noClassesText != null)
                noClassesText.SetActive(true);

            foreach (Transform child in classContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    async Task RepairMemberNames(string userId, List<string> classIds)
    {
        try
        {
            var userDoc = await db.Collection("users").Document(userId).GetSnapshotAsync();
            if (!userDoc.Exists) return;

            string firstName = userDoc.ContainsField("firstName") ? userDoc.GetValue<string>("firstName") : "";
            string lastName = userDoc.ContainsField("lastName") ? userDoc.GetValue<string>("lastName") : "";

            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)) return;

            var nameData = new Dictionary<string, object>
            {
                { "firstName", firstName },
                { "lastName", lastName }
            };

            foreach (string classId in classIds)
            {
                var memberRef = db.Collection("classes").Document(classId).Collection("members").Document(userId);
                var memberDoc = await memberRef.GetSnapshotAsync();

                if (!memberDoc.Exists) continue;

                bool hasFirst = memberDoc.ContainsField("firstName") && !string.IsNullOrEmpty(memberDoc.GetValue<string>("firstName"));
                bool hasLast = memberDoc.ContainsField("lastName") && !string.IsNullOrEmpty(memberDoc.GetValue<string>("lastName"));

                if (hasFirst && hasLast) continue;

                await memberRef.UpdateAsync(nameData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("RepairMemberNames failed (non-critical): " + e.Message);
        }
    }

    void CreateClassButton(string className, string classId, string classCode)
    {
        GameObject buttonObj = Instantiate(classButtonPrefab, classContainer);

        TMP_Text text = buttonObj.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = className;
        else
            Debug.LogError("TMP_Text missing on prefab");

        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners(); // 🔥 prevents duplicate listeners

            btn.onClick.AddListener(() =>
            {
                Debug.Log("Opening class: " + className);

                PlayerPrefs.SetString("SelectedClassId", classId);
                PlayerPrefs.SetString("SelectedClassName", className);
                PlayerPrefs.SetString("SelectedClassCode", classCode);
                PlayerPrefs.Save();

                SceneManager.LoadScene(classSceneName);
            });
        }
        else
        {
            Debug.LogError("Button component missing on prefab");
        }
    }
}