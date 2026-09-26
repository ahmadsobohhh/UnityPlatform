// Script: ClassroomManager
// Path: Assets/Scripts/Student/Classroom/ClassroomManager.cs
// Purpose: Controls student classroom scene setup and runtime interactions.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;

public class ClassroomManager : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TMP_Text classNameLabel;
    [SerializeField] private TMP_Text classCodeLabel;

    [Header("Student List")]
    [SerializeField] private Transform studentListContainer;
    [SerializeField] private TMP_Text studentCountLabel;
    [SerializeField] private GameObject noStudentsText;

    [Header("Panel Reveal")]
    [SerializeField] private CanvasGroup contentPanelGroup;
    [SerializeField] private float revealDelay = 0.8f;
    [SerializeField] private float revealDuration = 0.4f;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string classId;
    private string className;
    private string classCode;

    private struct MemberInfo
    {
        public string uid;
        public string displayName;
    }

    private readonly List<MemberInfo> members = new List<MemberInfo>();

    void Awake()
    {
        ApplyClassroomPolish();
        HideContentPanel();
    }

    private void ApplyClassroomPolish()
    {
        var background = FindCanvasBackground();
        if (background != null)
        {
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.anchoredPosition = Vector2.zero;
            background.sizeDelta = Vector2.zero;
        }

        var contentPanel = contentPanelGroup != null
            ? contentPanelGroup.GetComponent<RectTransform>()
            : FindSceneRect("ContentPanel");
        if (contentPanel != null)
        {
            contentPanel.anchorMin = new Vector2(0.09f, 0.17f);
            contentPanel.anchorMax = new Vector2(0.91f, 0.82f);
            contentPanel.pivot = new Vector2(0.5f, 0.5f);
            contentPanel.anchoredPosition = Vector2.zero;
            contentPanel.sizeDelta = Vector2.zero;

            var image = contentPanel.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.035f, 0.026f, 0.018f, 0.91f);
        }

        if (classNameLabel != null)
        {
            classNameLabel.fontSize = 48f;
            classNameLabel.fontStyle = FontStyles.Bold;
            classNameLabel.color = new Color(1f, 0.93f, 0.72f, 1f);
            classNameLabel.alignment = TextAlignmentOptions.Center;
        }

        if (studentCountLabel != null)
        {
            var rect = studentCountLabel.rectTransform;
            rect.anchorMin = new Vector2(0.04f, 0.84f);
            rect.anchorMax = new Vector2(0.62f, 0.96f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            studentCountLabel.fontSize = 25f;
            studentCountLabel.fontStyle = FontStyles.Bold;
            studentCountLabel.color = new Color(1f, 0.91f, 0.67f, 1f);
            studentCountLabel.alignment = TextAlignmentOptions.MidlineLeft;
        }

        if (classCodeLabel != null)
        {
            var rect = classCodeLabel.rectTransform;
            rect.anchorMin = new Vector2(0.66f, 0.84f);
            rect.anchorMax = new Vector2(0.96f, 0.96f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            classCodeLabel.fontSize = 24f;
            classCodeLabel.fontStyle = FontStyles.Bold;
            classCodeLabel.color = new Color(1f, 0.91f, 0.67f, 1f);
            classCodeLabel.alignment = TextAlignmentOptions.MidlineRight;
        }

        var scroll = FindSceneRect("Scroll");
        if (scroll != null && contentPanel != null && scroll.IsChildOf(contentPanel))
        {
            scroll.anchorMin = new Vector2(0.035f, 0.08f);
            scroll.anchorMax = new Vector2(0.62f, 0.78f);
            scroll.pivot = new Vector2(0.5f, 0.5f);
            scroll.anchoredPosition = Vector2.zero;
            scroll.sizeDelta = Vector2.zero;
        }

        if (noStudentsText != null)
        {
            var text = noStudentsText.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = "No classmates have joined this crew yet.";
                text.fontSize = 28f;
                text.color = new Color(0.92f, 0.84f, 0.68f, 0.9f);
            }
        }
    }

    private RectTransform FindSceneRect(string objectName)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect != null && rect.name == objectName)
                    return rect;
            }
        }

        return null;
    }

    private RectTransform FindCanvasBackground()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect != null && rect.name == "Background" && rect.parent != null &&
                    rect.parent.name == "Canvas")
                    return rect;
            }
        }

        return null;
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        classId = ClassSelection.CurrentClassId;
        className = ClassSelection.CurrentClassName;
        classCode = ClassSelection.CurrentClassCode;

        if (string.IsNullOrEmpty(classId))
        {
            classId = PlayerPrefs.GetString("SelectedClassId", "");
            className = PlayerPrefs.GetString("SelectedClassName", "");
            classCode = PlayerPrefs.GetString("SelectedClassCode", "");
        }

        if (classNameLabel)
            classNameLabel.text = string.IsNullOrEmpty(className) ? "Classroom" : className;
        if (classCodeLabel)
            classCodeLabel.text = string.IsNullOrEmpty(classCode) ? "" : "Code: " + classCode;
        if (noStudentsText)
            noStudentsText.SetActive(false);

        if (!string.IsNullOrEmpty(classId))
            StartCoroutine(LoadMembers());
    }

    private void HideContentPanel()
    {
        if (contentPanelGroup == null) return;
        contentPanelGroup.alpha = 0f;
        contentPanelGroup.interactable = false;
        contentPanelGroup.blocksRaycasts = false;
    }

    private IEnumerator RevealContentPanel()
    {
        yield return new WaitForSeconds(revealDelay);

        if (contentPanelGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / revealDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            contentPanelGroup.alpha = ease;
            yield return null;
        }

        contentPanelGroup.alpha = 1f;
        contentPanelGroup.interactable = true;
        contentPanelGroup.blocksRaycasts = true;
    }

    // ─────────────── STUDENT LIST ───────────────

    private IEnumerator LoadMembers()
    {
        var task = db.Collection("classes").Document(classId)
            .Collection("members").GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("[ClassroomManager] Failed to load members: " + task.Exception);
            yield break;
        }

        members.Clear();

        foreach (var doc in task.Result.Documents)
        {
            string uid = doc.Id;
            string first = doc.ContainsField("firstName") ? doc.GetValue<string>("firstName") : "";
            string last = doc.ContainsField("lastName") ? doc.GetValue<string>("lastName") : "";
            string full = (first + " " + last).Trim();

            if (string.IsNullOrEmpty(full))
            {
                var userTask = db.Collection("users").Document(uid).GetSnapshotAsync();
                yield return new WaitUntil(() => userTask.IsCompleted);

                if (!userTask.IsFaulted && !userTask.IsCanceled && userTask.Result.Exists)
                {
                    var u = userTask.Result;
                    first = u.ContainsField("firstName") ? u.GetValue<string>("firstName") : "";
                    last = u.ContainsField("lastName") ? u.GetValue<string>("lastName") : "";
                    full = (first + " " + last).Trim();
                    if (string.IsNullOrEmpty(full))
                        full = u.ContainsField("username") ? u.GetValue<string>("username") : "Student";
                }
                else
                {
                    full = "Unknown Student";
                }
            }

            members.Add(new MemberInfo { uid = uid, displayName = full });
        }

        if (noStudentsText)
            noStudentsText.SetActive(members.Count == 0);

        if (studentCountLabel)
        {
            if (members.Count == 0)
            {
                studentCountLabel.text = "";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.Append(members.Count).Append(members.Count == 1 ? " Student" : " Students").Append("  —  ");
                for (int i = 0; i < members.Count; i++)
                {
                    if (i > 0) sb.Append(",  ");
                    sb.Append(members[i].displayName);
                }
                studentCountLabel.text = sb.ToString();
            }
        }

        RebuildStudentList();
        StartCoroutine(RevealContentPanel());
    }

    private void RebuildStudentList()
    {
        if (studentListContainer == null) return;

        foreach (Transform child in studentListContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < members.Count; i++)
            CreateStudentRow(members[i].displayName, i + 1);
    }

    private void CreateStudentRow(string name, int number)
    {
        var row = new GameObject("StudentRow_" + number);
        row.transform.SetParent(studentListContainer, false);

        var rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 76);

        var layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 76;
        layout.flexibleWidth = 1;

        var img = row.AddComponent<Image>();
        img.color = (number % 2 == 0)
            ? new Color(0.10f, 0.08f, 0.05f, 0.75f)
            : new Color(0.14f, 0.11f, 0.07f, 0.85f);

        var shadow = row.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        shadow.effectDistance = new Vector2(0f, -3f);

        var outline = row.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.95f, 0.82f, 0.18f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Gold accent bar
        var accentGO = new GameObject("Accent");
        accentGO.transform.SetParent(row.transform, false);
        var accentRt = accentGO.AddComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.offsetMin = new Vector2(4f, 8f);
        accentRt.offsetMax = new Vector2(12f, -8f);
        var accentImg = accentGO.AddComponent<Image>();
        accentImg.color = new Color(0.98f, 0.86f, 0.58f, 0.9f);
        accentImg.raycastTarget = false;

        var textGO = new GameObject("Name");
        textGO.transform.SetParent(row.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28, 4);
        textRect.offsetMax = new Vector2(-16, -4);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = number + ".  " + name;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.97f, 0.88f, 1f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    // ─────────────── NAVIGATION ───────────────

    public void GoBack()
    {
        SceneTransition.LoadScene("StudentHub");
    }
}


