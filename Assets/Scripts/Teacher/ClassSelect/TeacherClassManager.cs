using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color selectedColor   = new Color(0.85f, 0.92f, 1f, 1f); // light blue

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

        // Start with list panel visible
        ShowListPanel();
        StartCoroutine(LoadClasses());
    }

    // Panel Management

    // Show class list panel
    private void ShowListPanel()
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

        // Go back to list and refresh
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

        // Get class index for current user
        var q = db.Collection("users").Document(user.UserId).Collection("classes");
        var task = q.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted || task.IsCanceled) { Debug.LogError(task.Exception); yield break; }

        _all.Clear();
        // Populate from index
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

    // Render current page of class list
    private void RenderPage()
    {
        foreach (Transform c in classListContainer) Destroy(c.gameObject); // clear existing

        // Show "no classes" graphic if needed
        bool hasAny = _all.Count > 0;
        if (emptyListGraphic) emptyListGraphic.SetActive(!hasAny);

        int pageCount = Mathf.Max(1, Mathf.CeilToInt(_all.Count / (float)pageSize)); // at least 1 page 
        _pageIndex = Mathf.Clamp(_pageIndex, 0, pageCount - 1); // clamp page index

        // Render items for current page
        int start = _pageIndex * pageSize;
        int end   = Mathf.Min(start + pageSize, _all.Count);

        // Instantiate class items
        for (int i = start; i < end; i++)
        {
            var row = _all[i];
            var go = Instantiate(classListItemPrefab, classListContainer); // create item

            // Bind texts
            var nameTxt = go.transform.Find("ClassName")?.GetComponent<TMP_Text>();
            var codeTxt = go.transform.Find("ClassCode")?.GetComponent<TMP_Text>();
            if (nameTxt) nameTxt.text = row.name;
            if (codeTxt) codeTxt.text = $"Code: {row.code}";

            // Ensure an Image to tint for highlight
            var bg = go.GetComponent<Image>();
            if (!bg) bg = go.AddComponent<Image>();
            bg.raycastTarget = true;

            // Ensure a Button to receive clicks
            var btn = go.GetComponent<Button>();
            if (!btn) btn = go.AddComponent<Button>();
            if (btn.targetGraphic == null) btn.targetGraphic = bg;

            // Highlight color
            bool isSelected = (row.id == _selectedClassId);
            bg.color = isSelected ? selectedColor : unselectedColor;

            // Click card to select
            int capturedI = i;
            var capturedId = row.id;
            var capturedName = row.name;
            var capturedCode = row.code;

            // Clear previous listeners
            btn.onClick.RemoveAllListeners();

            // Add new listener
            btn.onClick.AddListener(() =>
            {
                // If this card is already selected → unselect it
                if (_selectedClassId == capturedId)
                {
                    _selectedClassId = null;
                    _selectedIndex   = -1;

                    // Optional: clear global selection too
                    ClassSelection.CurrentClassId   = null;
                    ClassSelection.CurrentClassName = null;
                    ClassSelection.CurrentClassCode = null;
                }
                else
                {
                    // Otherwise select this card
                    _selectedClassId = capturedId;
                    _selectedIndex   = capturedI;

                    ClassSelection.CurrentClassId   = capturedId;
                    ClassSelection.CurrentClassName = capturedName;
                    ClassSelection.CurrentClassCode = capturedCode;
                }

                UpdateButtonVisibility();
                RenderPage(); // re-tint all tiles with new selection state
            });
        }

        if (prevPageBtn) prevPageBtn.interactable = (_pageIndex > 0);
        if (nextPageBtn) nextPageBtn.interactable = (_pageIndex < pageCount - 1);
        if (pageLabel) pageLabel.text = $"Page {_pageIndex + 1} of {pageCount}";
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

    // helpers
    private ClassRow GetSelectedRow()
    {
        if (string.IsNullOrEmpty(_selectedClassId)) return default;
        return _all.Find(r => r.id == _selectedClassId);
    }
}