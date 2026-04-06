using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine.SceneManagement;

public class TeacherClassManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject classListPanel;
    [SerializeField] private GameObject editPanel;
    [SerializeField] private GameObject createClassPanel;

    [Header("List UI")]
    [SerializeField] private Transform classListContainer;     // ClassList (Grid/Vertical container)
    [SerializeField] private GameObject classListItemPrefab;   // prefab with children: "ClassName" (TMP_Text), "ClassCode" (TMP_Text)
    [SerializeField] private GameObject emptyListGraphic;      // "You have no classes"

    [Header("Class Details UI")]
    [SerializeField] private TMP_Text classInviteCodeText;
    [SerializeField] private TMP_Text studentsListText;

    [Header("Pagination")]
    [SerializeField] private Button prevPageBtn;
    [SerializeField] private Button nextPageBtn;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private int pageSize = 6;

    [Header("Create Class Panel")]
    [SerializeField] private TMP_InputField createClassNameInput;    // input field in CreateClassPanel
    [SerializeField] private Button createConfirmBtn;                // CreateBtn in CreateClassPanel
    [SerializeField] private int codeLength = 6;

    [Header("Edit Panel")]
    [SerializeField] private TMP_Text editTitleLabel;       // "Edit class:" label
    [SerializeField] private TMP_InputField editNameInput;   // input field for class name
    [SerializeField] private Button editConfirmBtn;          // confirm edit
    [SerializeField] private Button deleteConfirmBtn;        // delete class

    [Header("List Panel Buttons")]
    [SerializeField] private Button joinBtn;     // JoinBtn
    [SerializeField] private Button createBtn;   // createBtn (shows create panel)
    [SerializeField] private Button editBtn;     // editBtn (shows edit panel)

    [Header("Selection Visuals")]
    [SerializeField] private Color unselectedColor = new Color(0.22f, 0.18f, 0.12f, 0.90f);
    [SerializeField] private Color selectedColor   = new Color(0.45f, 0.35f, 0.15f, 0.95f);

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private struct ClassRow
    {
        public string id;
        public string name;
        public string code;
        public long createdAtSeconds; // for sorting
    }

    private readonly List<ClassRow> _all = new();
    private int _pageIndex = 0;

    // --- selection state ---
    private string _selectedClassId = null;
    private int _selectedIndex = -1;

    void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        db   = FirebaseFirestore.DefaultInstance;
    }

    void Start()
    {
        // Wire up buttons
        if (joinBtn) joinBtn.onClick.AddListener(JoinSelected);
        if (createBtn) createBtn.onClick.AddListener(ShowCreatePanel);
        if (editBtn) editBtn.onClick.AddListener(ShowEditPanel);
        if (createConfirmBtn) createConfirmBtn.onClick.AddListener(OnConfirmCreate);
        if (editConfirmBtn) editConfirmBtn.onClick.AddListener(OnConfirmEdit);
        if (deleteConfirmBtn) deleteConfirmBtn.onClick.AddListener(OnConfirmDelete);
        if (prevPageBtn) prevPageBtn.onClick.AddListener(PrevPage);
        if (nextPageBtn) nextPageBtn.onClick.AddListener(NextPage);

        bool isClassSelectMode = classListContainer != null;
        if (isClassSelectMode)
        {
            ShowListPanel();
            StartCoroutine(LoadClasses());
        }
        else
        {
            EnsureBackButtonWiring();
            EnsureEditListButton();
            StartCoroutine(LoadSelectedClassDetailsRoutine());
        }
    }

    private void EnsureBackButtonWiring()
    {
        GameObject backObj = GameObject.Find("BackBtn");
        if (backObj == null)
            return;

        var btn = backObj.GetComponent<Button>();
        if (btn == null)
            return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(BackToClassSelect);
    }

    private void EnsureEditListButton()
    {
        if (GameObject.Find("EditListBtn") != null)
            return;

        GameObject backObj = GameObject.Find("BackBtn");
        if (backObj == null)
            return;

        Transform parent = backObj.transform.parent;
        if (parent == null)
            return;

        GameObject editListObj = Instantiate(backObj, parent);
        editListObj.name = "EditListBtn";

        var rect = editListObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(-329f, 65f);
        }

        var text = editListObj.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = "edit list";

        var btn = editListObj.GetComponent<Button>();
        if (btn != null)
        {
            // Disable inherited persistent listeners (e.g. BackToClassSelect from BackBtn clone)
            for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                btn.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(EditList_NoOp);
        }
    }

    private IEnumerator LoadSelectedClassDetailsRoutine()
    {
        string classId = ClassSelection.CurrentClassId;
        string classCode = ClassSelection.CurrentClassCode;

        if (classInviteCodeText)
            classInviteCodeText.text = string.IsNullOrEmpty(classCode) ? "Class: [Class Code]" : $"Class: {classCode}";

        if (studentsListText)
            studentsListText.text = "Loading students...";

        if (string.IsNullOrEmpty(classId))
        {
            if (studentsListText) studentsListText.text = "No class selected.";
            yield break;
        }

        // Refresh class code from Firestore in case static selection was lost or stale.
        var classTask = db.Collection("classes").Document(classId).GetSnapshotAsync();
        yield return new WaitUntil(() => classTask.IsCompleted);

        if (!classTask.IsFaulted && !classTask.IsCanceled && classTask.Result.Exists && classTask.Result.ContainsField("code"))
        {
            classCode = classTask.Result.GetValue<string>("code");
            ClassSelection.CurrentClassCode = classCode;
        }

        if (classInviteCodeText)
            classInviteCodeText.text = string.IsNullOrEmpty(classCode) ? "Class: [No Code]" : $"Class: {classCode}";

        var membersTask = db.Collection("classes").Document(classId).Collection("members").GetSnapshotAsync();
        yield return new WaitUntil(() => membersTask.IsCompleted);

        if (membersTask.IsFaulted || membersTask.IsCanceled)
        {
            Debug.LogError("Failed to load class members: " + membersTask.Exception);
            if (studentsListText) studentsListText.text = "Failed to load students.";
            yield break;
        }

        var memberNames = new List<string>();
        foreach (var memberDoc in membersTask.Result.Documents)
        {
            string uid = memberDoc.Id;

            string memberFirst = memberDoc.ContainsField("firstName") ? memberDoc.GetValue<string>("firstName") : "";
            string memberLast = memberDoc.ContainsField("lastName") ? memberDoc.GetValue<string>("lastName") : "";
            string memberFull = NormalizeHumanName(($"{memberFirst} {memberLast}").Trim());
            if (!string.IsNullOrEmpty(memberFull))
            {
                memberNames.Add(memberFull);
                continue;
            }

            var userTask = db.Collection("users").Document(uid).GetSnapshotAsync();
            yield return new WaitUntil(() => userTask.IsCompleted);

            if (userTask.IsFaulted || userTask.IsCanceled)
            {
                memberNames.Add("Unknown Student");
                continue;
            }

            if (!userTask.Result.Exists)
            {
                memberNames.Add("Unknown Student");
                continue;
            }

            string displayName = FormatDisplayName(userTask.Result, uid);
            memberNames.Add(displayName);
        }

        if (studentsListText)
        {
            if (memberNames.Count == 0)
            {
                studentsListText.text = "No students have joined yet.";
            }
            else
            {
                var sb = new StringBuilder();
                for (int i = 0; i < memberNames.Count; i++)
                {
                    sb.Append(i + 1).Append(". ").Append(memberNames[i]);
                    if (i < memberNames.Count - 1) sb.AppendLine();
                }
                studentsListText.text = sb.ToString();
            }
        }
    }

    private string FormatDisplayName(DocumentSnapshot userDoc, string fallbackUid)
    {
        string firstName = NormalizeHumanName(GetFirstNonEmptyField(userDoc, "firstName", "firstname", "first_name"));
        string lastName = NormalizeHumanName(GetFirstNonEmptyField(userDoc, "lastName", "lastname", "last_name"));
        string username = NormalizeHumanName(GetFirstNonEmptyField(userDoc, "username", "displayName", "name"));
        string fullNameField = NormalizeHumanName(GetFirstNonEmptyField(userDoc, "fullName", "full_name"));

        string fullName = ($"{firstName} {lastName}").Trim();
        if (!string.IsNullOrEmpty(fullName)) return fullName;
        if (!string.IsNullOrEmpty(fullNameField)) return fullNameField;
        if (!string.IsNullOrEmpty(username)) return username;
        return "Unknown Student";
    }

    private string GetFirstNonEmptyField(DocumentSnapshot doc, params string[] fieldNames)
    {
        for (int i = 0; i < fieldNames.Length; i++)
        {
            string field = fieldNames[i];
            if (!doc.ContainsField(field)) continue;

            string value = doc.GetValue<string>(field);
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return "";
    }

    private string NormalizeHumanName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string trimmed = value.Trim();
        if (LooksLikeIdentifier(trimmed))
            return "";

        return trimmed;
    }

    private bool LooksLikeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Firebase-like IDs/user IDs are usually long, compact, and have no spaces.
        if (value.Length < 16 || value.Contains(" "))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool isAlphaNum = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
            bool isAllowed = isAlphaNum || c == '_' || c == '-';
            if (!isAllowed)
                return false;
        }

        return true;
    }

    // Panel Management

    public void ShowListPanel()
    {
        if (classListPanel) classListPanel.SetActive(true);
        if (editPanel) editPanel.SetActive(false);
        if (createClassPanel) createClassPanel.SetActive(false);
    }

    // Show edit panel for selected class
    private void ShowEditPanel()
    {
        if (string.IsNullOrEmpty(_selectedClassId))
        {
            Debug.LogWarning("No class selected to edit.");
            return;
        }

        var row = GetSelectedRow();
        if (editTitleLabel) editTitleLabel.text = $"Edit class: {row.name}";
        if (editNameInput) editNameInput.text = row.name;

        if (classListPanel) classListPanel.SetActive(false);
        if (editPanel) editPanel.SetActive(true);
        if (createClassPanel) createClassPanel.SetActive(false);
    }

    // Show create class panel
    private void ShowCreatePanel()
    {
        if (createClassNameInput) createClassNameInput.text = "";

        if (classListPanel) classListPanel.SetActive(false);
        if (editPanel) editPanel.SetActive(false);
        if (createClassPanel) createClassPanel.SetActive(true);
    }

    // Button Callbacks
    public void NextPage()
    {
        _pageIndex++;
        RenderPage();
    }

    public void PrevPage()
    {
        _pageIndex = Mathf.Max(0, _pageIndex - 1);
        RenderPage();
    }

    // Join selected class
    public void JoinSelected()
    {
        if (string.IsNullOrEmpty(_selectedClassId))
        {
            Debug.LogWarning("No class selected.");
            return;
        }

        var row = GetSelectedRow();
        ClassSelection.CurrentClassId = row.id;
        ClassSelection.CurrentClassName = row.name;
        ClassSelection.CurrentClassCode = row.code;

        SceneManager.LoadScene("TeacherClass");
    }

    // Confirm create/edit/delete
    private void OnConfirmCreate()
    {
        StartCoroutine(CreateClassRoutine(createClassNameInput?.text));
    }

    private void OnConfirmEdit()
    {
        if (string.IsNullOrEmpty(_selectedClassId))
        {
            Debug.LogWarning("No class selected.");
            return;
        }
        StartCoroutine(RenameRoutine(_selectedClassId, editNameInput?.text));
    }

    private void OnConfirmDelete()
    {
        if (string.IsNullOrEmpty(_selectedClassId))
        {
            Debug.LogWarning("No class selected.");
            return;
        }
        StartCoroutine(DeleteRoutine(_selectedClassId));
    }

    // Create class
    private IEnumerator CreateClassRoutine(string classNameRaw)
    {
        // Get current user
        var user = auth.CurrentUser;
        if (user == null) { Debug.LogError("No signed-in user."); yield break; }

        // Validate class name
        string className = (classNameRaw ?? "").Trim();
        if (string.IsNullOrEmpty(className)) { Debug.LogWarning("Class name empty."); yield break; }

        // Ensure unique join code
        string code = null;
        bool unique = false;
        // Try generating codes until we find a unique one
        while (!unique)
        {
            code = GenerateCode(codeLength);
            var checkTask = db.Collection("classes").WhereEqualTo("code", code).Limit(1).GetSnapshotAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);
            if (checkTask.IsFaulted || checkTask.IsCanceled) { Debug.LogError(checkTask.Exception); yield break; }
            unique = checkTask.Result.Count == 0;
        }

        // Create global class doc
        var classRef = db.Collection("classes").Document();
        var now = Timestamp.GetCurrentTimestamp();
        var classData = new Dictionary<string, object> {
            { "id", classRef.Id }, { "name", className }, { "code", code },
            { "ownerUid", user.UserId }, { "createdAt", now }, { "updatedAt", now }
        };
        var createTask = classRef.SetAsync(classData);
        yield return new WaitUntil(() => createTask.IsCompleted);
        if (createTask.IsFaulted || createTask.IsCanceled) { Debug.LogError(createTask.Exception); yield break; }

        // Index under teacher for quick listing
        var idxRef  = db.Collection("users").Document(user.UserId).Collection("classes").Document(classRef.Id);
        var idxData = new Dictionary<string, object> {
            { "id", classRef.Id }, { "name", className }, { "code", code }, { "createdAt", now }
        };
        var mapTask = idxRef.SetAsync(idxData);
        yield return new WaitUntil(() => mapTask.IsCompleted);
        if (mapTask.IsFaulted || mapTask.IsCanceled) { Debug.LogError(mapTask.Exception); yield break; }

        ShowListPanel();
        yield return StartCoroutine(LoadClasses());
    }

    // Generate random join code
    private string GenerateCode(int len)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new System.Random();
        var sb = new System.Text.StringBuilder(len);
        for (int i = 0; i < len; i++) sb.Append(alphabet[rng.Next(alphabet.Length)]);
        return sb.ToString();
    }

    // Load + render 
    private IEnumerator LoadClasses()
    {
        var user = auth.CurrentUser;
        if (user == null) yield break;

        var q = db.Collection("users").Document(user.UserId).Collection("classes");
        var task = q.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted || task.IsCanceled) { Debug.LogError(task.Exception); yield break; }

        _all.Clear();
        foreach (var d in task.Result)
        {
            string id   = d.ContainsField("id")   ? d.GetValue<string>("id")   : d.Id;
            string name = d.ContainsField("name") ? d.GetValue<string>("name") : "(Unnamed)";
            string code = d.ContainsField("code") ? d.GetValue<string>("code") : "—";

            long createdAtSec = 0;
            if (d.ContainsField("createdAt"))
            {
                var ts = d.GetValue<Firebase.Firestore.Timestamp>("createdAt");
                createdAtSec = (long)(ts.ToDateTime().ToUniversalTime() - System.DateTime.UnixEpoch).TotalSeconds;
            }

            _all.Add(new ClassRow { id = id, name = name, code = code, createdAtSeconds = createdAtSec });
        }

        // Sort newest → oldest
        _all.Sort((a, b) => b.createdAtSeconds.CompareTo(a.createdAtSeconds));

        // Keep selection if it still exists
        if (!string.IsNullOrEmpty(_selectedClassId))
        {
            bool stillThere = _all.Exists(r => r.id == _selectedClassId);
            if (!stillThere) { _selectedClassId = null; _selectedIndex = -1; }
        }

        // Update button visibility based on selection
        UpdateButtonVisibility();

        _pageIndex = Mathf.Clamp(_pageIndex, 0, Mathf.Max(0, Mathf.CeilToInt(_all.Count / (float)pageSize) - 1));
        RenderPage();
    }

    // Update Join/Edit button visibility based on selection
    private void UpdateButtonVisibility()
    {
        bool hasSelection = !string.IsNullOrEmpty(_selectedClassId);
        
        if (joinBtn) joinBtn.gameObject.SetActive(hasSelection);
        if (createBtn) createBtn.gameObject.SetActive(true); // Always visible
        if (editBtn) editBtn.gameObject.SetActive(hasSelection);
    }

    private void RenderPage()
    {
        Debug.Log($"[TCM] RenderPage — container={classListContainer}, " +
                  $"parent={classListContainer?.parent?.name}, " +
                  $"count={_all.Count}");

        foreach (Transform c in classListContainer) Destroy(c.gameObject);

        bool hasAny = _all.Count > 0;
        if (emptyListGraphic) emptyListGraphic.SetActive(!hasAny);

        int pageCount = Mathf.Max(1, Mathf.CeilToInt(_all.Count / (float)pageSize));
        _pageIndex = Mathf.Clamp(_pageIndex, 0, pageCount - 1);

        int start = _pageIndex * pageSize;
        int end   = Mathf.Min(start + pageSize, _all.Count);

        for (int i = start; i < end; i++)
        {
            var row = _all[i];
            bool isSelected = (row.id == _selectedClassId);

            var go = CreateClassCard(row.name, row.code, isSelected);
            go.transform.SetParent(classListContainer, false);

            int capturedI = i;
            var capturedId = row.id;
            var capturedName = row.name;
            var capturedCode = row.code;

            var btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (_selectedClassId == capturedId)
                {
                    _selectedClassId = null;
                    _selectedIndex   = -1;
                    ClassSelection.CurrentClassId   = null;
                    ClassSelection.CurrentClassName = null;
                    ClassSelection.CurrentClassCode = null;
                }
                else
                {
                    _selectedClassId = capturedId;
                    _selectedIndex   = capturedI;
                    ClassSelection.CurrentClassId   = capturedId;
                    ClassSelection.CurrentClassName = capturedName;
                    ClassSelection.CurrentClassCode = capturedCode;
                }

                UpdateButtonVisibility();
                RenderPage();
            });

            Debug.Log($"[TCM] Card created: '{row.name}' active={go.activeSelf} " +
                      $"parentActive={go.transform.parent?.gameObject.activeInHierarchy}");
        }

        if (prevPageBtn) prevPageBtn.interactable = (_pageIndex > 0);
        if (nextPageBtn) nextPageBtn.interactable = (_pageIndex < pageCount - 1);
        if (pageLabel) pageLabel.text = $"Page {_pageIndex + 1} of {pageCount}";
    }

    private GameObject CreateClassCard(string className, string code, bool selected)
    {
        var card = new GameObject("ClassCard", typeof(RectTransform));
        card.layer = 5;

        var rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);

        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 80;
        le.flexibleWidth = 1;

        var bg = card.AddComponent<Image>();
        bg.color = selected
            ? new Color(0.85f, 0.68f, 0.30f, 1f)
            : new Color(1f, 1f, 1f, 0.95f);
        bg.raycastTarget = true;

        var outline = card.AddComponent<Outline>();
        outline.effectColor = selected ? Color.yellow : new Color(0.6f, 0.5f, 0.3f, 1f);
        outline.effectDistance = new Vector2(3, -3);

        var btn = card.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.9f, 0.85f, 0.7f, 1f);
        colors.pressedColor = new Color(0.7f, 0.6f, 0.4f, 1f);
        btn.colors = colors;

        // Class name — black text, large, left-aligned
        var nameGO = new GameObject("ClassName", typeof(RectTransform));
        nameGO.layer = 5;
        nameGO.transform.SetParent(card.transform, false);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0);
        nameRT.anchorMax = new Vector2(0.65f, 1);
        nameRT.offsetMin = new Vector2(20, 5);
        nameRT.offsetMax = new Vector2(0, -5);

        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = className;
        nameTMP.fontSize = 30;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = selected ? Color.white : Color.black;
        nameTMP.alignment = TextAlignmentOptions.Left;
        nameTMP.raycastTarget = false;
        nameTMP.enableWordWrapping = false;
        nameTMP.overflowMode = TextOverflowModes.Ellipsis;

        // Code — dark gray, right-aligned
        var codeGO = new GameObject("ClassCode", typeof(RectTransform));
        codeGO.layer = 5;
        codeGO.transform.SetParent(card.transform, false);
        var codeRT = codeGO.GetComponent<RectTransform>();
        codeRT.anchorMin = new Vector2(0.65f, 0);
        codeRT.anchorMax = new Vector2(1, 1);
        codeRT.offsetMin = new Vector2(0, 5);
        codeRT.offsetMax = new Vector2(-20, -5);

        var codeTMP = codeGO.AddComponent<TextMeshProUGUI>();
        codeTMP.text = $"Code: {code}";
        codeTMP.fontSize = 24;
        codeTMP.color = selected ? new Color(1f, 1f, 1f, 0.85f) : new Color(0.3f, 0.3f, 0.3f, 1f);
        codeTMP.alignment = TextAlignmentOptions.Right;
        codeTMP.raycastTarget = false;

        return card;
    }

    // Rename class
    private IEnumerator RenameRoutine(string classId, string newNameRaw)
    {
        var user = auth.CurrentUser;
        if (user == null) yield break;

        string newName = (newNameRaw ?? "").Trim();
        if (string.IsNullOrEmpty(newName)) { Debug.LogWarning("New name empty."); yield break; }

        var globalRef = db.Collection("classes").Document(classId);
        var idxRef = db.Collection("users").Document(user.UserId).Collection("classes").Document(classId);

        var t1 = globalRef.UpdateAsync(new Dictionary<string, object> {
            { "name", newName },
            { "updatedAt", Firebase.Firestore.Timestamp.GetCurrentTimestamp() }
        });
        var t2 = idxRef.UpdateAsync(new Dictionary<string, object> {
            { "name", newName }
        });

        yield return new WaitUntil(() => t1.IsCompleted && t2.IsCompleted);

        if (t1.IsFaulted || t2.IsFaulted) 
        { 
            Debug.LogError("Rename failed: " + (t1.Exception ?? t2.Exception)); 
            yield break; 
        }

        Debug.Log("Class renamed successfully.");
        
        // Go back to list and refresh
        ShowListPanel();
        yield return StartCoroutine(LoadClasses());
    }

    // Delete class
    private IEnumerator DeleteRoutine(string classId)
    {
        var user = auth.CurrentUser;
        if (user == null) yield break;

        var globalRef = db.Collection("classes").Document(classId);
        var idxRef = db.Collection("users").Document(user.UserId).Collection("classes").Document(classId);

        var del1 = globalRef.DeleteAsync();
        var del2 = idxRef.DeleteAsync();

        yield return new WaitUntil(() => del1.IsCompleted && del2.IsCompleted);

        if (del1.IsFaulted || del2.IsFaulted)
        {
            Debug.LogError("Delete failed: " + (del1.Exception ?? del2.Exception));
            yield break;
        }

        Debug.Log("Class deleted successfully.");

        _selectedClassId = null;
        _selectedIndex = -1;

        // Go back to list and refresh
        ShowListPanel();
        yield return StartCoroutine(LoadClasses());
    }
    
    // Sign out and return to WelcomePage
    public void SignOut()
    {
        auth.SignOut();
        SceneManager.LoadScene("WelcomePage");
    }

    public void BackToClassSelect()
    {
        SceneManager.LoadScene("TeacherClassSelect");
    }

    public void EditList_NoOp()
    {
        // Intentionally does nothing for now.
    }

    // Placeholder for Create Game button until gameplay creation flow is implemented.
    public void CreateGame_NoOp()
    {
        // Intentionally left blank.
    }

    // helpers
    private ClassRow GetSelectedRow()
    {
        if (string.IsNullOrEmpty(_selectedClassId)) return default;
        return _all.Find(r => r.id == _selectedClassId);
    }

}