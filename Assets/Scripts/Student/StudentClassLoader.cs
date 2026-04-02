using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

            foreach (var doc in snapshot.Documents)
            {
                string className = doc.ContainsField("name")
                    ? doc.GetValue<string>("name")
                    : "Unnamed Class";

                string classCode = doc.ContainsField("code")
                    ? doc.GetValue<string>("code")
                    : "";

                string classId = doc.Id;

                CreateClassButton(className, classId, classCode);
            }
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