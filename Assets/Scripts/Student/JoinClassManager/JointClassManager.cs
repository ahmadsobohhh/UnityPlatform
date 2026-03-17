using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Firestore;

public class JointClassManager : MonoBehaviour
{
    public TMP_InputField codeInput;
    public string sceneToLoad = "ClassroomScene";

    private FirebaseFirestore db;

    private bool shouldJoin = false;
    private string pendingClassDocId;
    private string pendingClassName;
    private string pendingClassCode;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        Debug.Log("Scene to load is: " + sceneToLoad);
    }

    void Update()
    {
        if (shouldJoin)
        {
            shouldJoin = false;

            Debug.Log("Now joining on main thread...");
            Debug.Log("Saving PlayerPrefs...");
            PlayerPrefs.SetString("JoinedClassDocId", pendingClassDocId);
            PlayerPrefs.SetString("JoinedClassName", pendingClassName);
            PlayerPrefs.SetString("JoinedClassCode", pendingClassCode);
            PlayerPrefs.Save();

            Debug.Log("Loading scene: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
    }
public void TestLoadScene()
{
    Debug.Log("TEST loading classroom");
    SceneManager.LoadScene("ClassroomScene");
}
    public void JoinClassByCode()
    {
        string enteredCode = codeInput.text.Trim();

        if (string.IsNullOrEmpty(enteredCode))
        {
            Debug.Log("Please enter a class code.");
            return;
        }

        Debug.Log("Entered code: " + enteredCode);

        db.Collection("classes")
          .WhereEqualTo("code", enteredCode)
          .GetSnapshotAsync()
          .ContinueWith(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("Error finding class: " + task.Exception);
                  return;
              }

              if (!task.IsCompleted)
              {
                  return;
              }

              QuerySnapshot snapshot = task.Result;

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

              string firestoreDocId = classDoc.Id;
              string className = classDoc.GetValue<string>("name");
              string classCode = classDoc.GetValue<string>("code");

              Debug.Log("Class found!");
              Debug.Log("Doc ID: " + firestoreDocId);
              Debug.Log("Class Name: " + className);
              Debug.Log("Class Code: " + classCode);

              pendingClassDocId = firestoreDocId;
              pendingClassName = className;
              pendingClassCode = classCode;

              shouldJoin = true;
          });
    }
}