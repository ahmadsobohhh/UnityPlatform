using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using System.Collections.Generic;

public class JoinClassPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TMP_InputField codeInput;
    public TextMeshProUGUI statusLabel;
    public Button joinButton;
    public Button closeButton;
    public StudentClassLoader classLoader;

    private CanvasGroup panelGroup;
    private CanvasGroup dimmerGroup;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private bool isOpen;
    private bool isJoining;

    private void Awake()
    {
        if (popupPanel != null)
        {
            panelGroup = popupPanel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
                panelGroup = popupPanel.AddComponent<CanvasGroup>();

            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
            popupPanel.SetActive(true);
        }

        var dimmer = popupPanel?.transform.parent?.Find("JoinClassDimmer");
        if (dimmer != null)
        {
            dimmerGroup = dimmer.GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
    }

    public void Open()
    {
        isOpen = true;
        if (panelGroup != null)
        {
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }
        if (dimmerGroup != null)
        {
            dimmerGroup.interactable = true;
            dimmerGroup.blocksRaycasts = true;
        }
        if (codeInput != null) codeInput.text = "";
        if (statusLabel != null) statusLabel.text = "";
    }

    public void Close()
    {
        isOpen = false;
        if (panelGroup != null)
        {
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
        if (dimmerGroup != null)
        {
            dimmerGroup.interactable = false;
            dimmerGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (panelGroup == null) return;
        float target = isOpen ? 1f : 0f;
        panelGroup.alpha = Mathf.Lerp(panelGroup.alpha, target, Time.deltaTime * 10f);
        if (dimmerGroup != null)
            dimmerGroup.alpha = Mathf.Lerp(dimmerGroup.alpha, target, Time.deltaTime * 10f);
    }

    public async void JoinClass()
    {
        if (isJoining) return;

        string enteredCode = codeInput != null ? codeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(enteredCode))
        {
            SetStatus("Please enter a class code.", Color.yellow);
            return;
        }

        isJoining = true;
        SetStatus("Joining...", new Color(0.85f, 0.78f, 0.60f));

        try
        {
            var snapshot = await db.Collection("classes")
                .WhereEqualTo("code", enteredCode)
                .GetSnapshotAsync();

            if (snapshot.Count <= 0)
            {
                SetStatus("Invalid code. Try again.", new Color(1f, 0.5f, 0.4f));
                isJoining = false;
                return;
            }

            DocumentSnapshot classDoc = null;
            foreach (var doc in snapshot.Documents) { classDoc = doc; break; }

            if (classDoc == null)
            {
                SetStatus("No class found.", new Color(1f, 0.5f, 0.4f));
                isJoining = false;
                return;
            }

            string classId = classDoc.Id;
            string className = classDoc.GetValue<string>("name");
            string classCode = classDoc.GetValue<string>("code");

            var user = auth.CurrentUser;
            if (user == null)
            {
                SetStatus("Not signed in.", new Color(1f, 0.5f, 0.4f));
                isJoining = false;
                return;
            }

            string userId = user.UserId;
            string firstName = "";
            string lastName = "";

            var userDoc = await db.Collection("users").Document(userId).GetSnapshotAsync();
            if (userDoc.Exists)
            {
                if (userDoc.ContainsField("firstName")) firstName = userDoc.GetValue<string>("firstName");
                if (userDoc.ContainsField("lastName")) lastName = userDoc.GetValue<string>("lastName");
            }

            await db.Collection("users").Document(userId)
                .Collection("classes").Document(classId)
                .SetAsync(new Dictionary<string, object>
                {
                    { "name", className },
                    { "code", classCode }
                });

            await db.Collection("classes").Document(classId)
                .Collection("members").Document(userId)
                .SetAsync(new Dictionary<string, object>
                {
                    { "joinedAt", Timestamp.GetCurrentTimestamp() },
                    { "firstName", firstName ?? "" },
                    { "lastName", lastName ?? "" }
                });

            SetStatus("Joined " + className + "!", new Color(0.5f, 1f, 0.5f));

            PlayerPrefs.SetString("JoinedClassDocId", classId);
            PlayerPrefs.SetString("JoinedClassName", className);
            PlayerPrefs.SetString("JoinedClassCode", classCode);
            PlayerPrefs.Save();

            await System.Threading.Tasks.Task.Delay(600);

            Close();

            if (classLoader != null)
            {
                Debug.Log("[JoinClassPopup] Refreshing class list via direct reference...");
                classLoader.RefreshClasses();
            }
            else
            {
                Debug.LogWarning("[JoinClassPopup] classLoader is null, reloading scene...");
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error joining class: " + e);
            SetStatus("Error. Try again.", new Color(1f, 0.5f, 0.4f));
        }

        isJoining = false;
    }

    private void SetStatus(string text, Color color)
    {
        if (statusLabel == null) return;
        statusLabel.text = text;
        statusLabel.color = color;
    }
}
