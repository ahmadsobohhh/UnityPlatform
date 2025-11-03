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
    [Header("List UI")]
    [SerializeField] private Transform classListContainer;     // your ClassList (Grid/Vertical container)
    [SerializeField] private GameObject classListItemPrefab;   // prefab with children: "ClassName" (TMP_Text), "ClassCode" (TMP_Text)
    [SerializeField] private GameObject emptyListGraphic;      // optional "You have no classes"

    [Header("Pagination")]
    [SerializeField] private Button prevPageBtn;
    [SerializeField] private Button nextPageBtn;
    [SerializeField] private TMP_Text pageLabel;               // optional
    [SerializeField] private int pageSize = 6;

    [Header("Create Class")]
    [SerializeField] private TMP_InputField classNameInput;    // your input field in the Create panel
    [SerializeField] private int codeLength = 6;

    [Header("Buttons")]
    [SerializeField] private Button createBtn;   // Your "Create Class" button
    [SerializeField] private Button editBtn;     // Your "Edit" button

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
        StartCoroutine(LoadClasses());
    }

    // --- PUBLIC: wire these in the Inspector to your buttons ---
    public void CreateClass_FromInput()        => StartCoroutine(CreateClassRoutine(classNameInput?.text));
    public void RefreshClassList()             => StartCoroutine(LoadClasses());
    public void NextPage()                     { _pageIndex++; RenderPage(); }
    public void PrevPage()                     { _pageIndex = Mathf.Max(0, _pageIndex - 1); RenderPage(); }

    // Navigate to TeacherClass for the selected class
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

    // Call from your rename modal's Save button, or directly
    public void RenameSelected(string newNameRaw)
    {
        if (string.IsNullOrEmpty(_selectedClassId)) { Debug.LogWarning("No class selected."); return; }
        StartCoroutine(RenameRoutine(_selectedClassId, newNameRaw));
    }

    // Helper if you want to wire a TMP_InputField directly
    public void RenameSelected_FromInput(TMP_InputField input)
    {
        var txt = input ? input.text : null;
        RenameSelected(txt);
    }

    // Call from your delete confirm button
    public void DeleteSelected()
    {
        if (string.IsNullOrEmpty(_selectedClassId)) { Debug.LogWarning("No class selected."); return; }
        StartCoroutine(DeleteRoutine(_selectedClassId));
    }

    // --- Create class ---
    private IEnumerator CreateClassRoutine(string classNameRaw)
    {
        var user = auth.CurrentUser;
        if (user == null) { Debug.LogError("No signed-in user."); yield break; }

        string className = (classNameRaw ?? "").Trim();
        if (string.IsNullOrEmpty(className)) { Debug.LogWarning("Class name empty."); yield break; }

        // Ensure unique join code
        string code = null;
        bool unique = false;
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

        // Clear input (optional) and refresh
        if (classNameInput) classNameInput.text = "";
        yield return StartCoroutine(LoadClasses());
    }

    private string GenerateCode(int len)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new System.Random();
        var sb = new System.Text.StringBuilder(len);
        for (int i = 0; i < len; i++) sb.Append(alphabet[rng.Next(alphabet.Length)]);
        return sb.ToString();
    }

    // --- Load + render ---
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

        // reset button states
        if (string.IsNullOrEmpty(_selectedClassId))
        {
            if (createBtn) createBtn.gameObject.SetActive(true);
            if (editBtn)   editBtn.gameObject.SetActive(false);
        }
        else
        {
            if (createBtn) createBtn.gameObject.SetActive(false);
            if (editBtn)   editBtn.gameObject.SetActive(true);
        }

        _pageIndex = Mathf.Clamp(_pageIndex, 0, Mathf.Max(0, Mathf.CeilToInt(_all.Count / (float)pageSize) - 1));
        RenderPage();
    }

    private void RenderPage()
    {
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
            var go = Instantiate(classListItemPrefab, classListContainer);

            // Bind texts
            var nameTxt = go.transform.Find("ClassName")?.GetComponent<TMP_Text>();
            var codeTxt = go.transform.Find("ClassCode")?.GetComponent<TMP_Text>();
            if (nameTxt) nameTxt.text = row.name;
            if (codeTxt) codeTxt.text = $"Code: {row.code}";

            // Ensure an Image to tint for highlight
            var bg = go.GetComponent<Image>();
            if (!bg) bg = go.AddComponent<Image>(); // gives us a targetGraphic
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

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                _selectedClassId = capturedId;
                _selectedIndex = capturedI;

                ClassSelection.CurrentClassId = capturedId;
                ClassSelection.CurrentClassName = capturedName;
                ClassSelection.CurrentClassCode = capturedCode;

                if (createBtn) createBtn.gameObject.SetActive(false);
                if (editBtn)   editBtn.gameObject.SetActive(true);

                RenderPage(); // re-tint all tiles with new selection
            });
        }

        if (prevPageBtn) prevPageBtn.interactable = (_pageIndex > 0);
        if (nextPageBtn) nextPageBtn.interactable = (_pageIndex < pageCount - 1);
        if (pageLabel)   pageLabel.text = $"{_pageIndex + 1}/{pageCount}";
    }

    // --- Firestore ops: rename/delete both global + index ---
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

        if (t1.IsFaulted || t2.IsFaulted) { Debug.LogError("Rename failed."); yield break; }

        yield return StartCoroutine(LoadClasses());
    }

    private IEnumerator DeleteRoutine(string classId)
    {
        var user = auth.CurrentUser;
        if (user == null) yield break;

        var globalRef = db.Collection("classes").Document(classId);
        var idxRef = db.Collection("users").Document(user.UserId).Collection("classes").Document(classId);

        var del1 = globalRef.DeleteAsync();
        var del2 = idxRef.DeleteAsync();

        yield return new WaitUntil(() => del1.IsCompleted && del2.IsCompleted);

        if (del1.IsFaulted || del2.IsFaulted) { Debug.LogError("Delete failed."); yield break; }

        _selectedClassId = null;
        _selectedIndex = -1;
        yield return StartCoroutine(LoadClasses());
    }

    // --- helpers ---
    private ClassRow GetSelectedRow()
    {
        if (string.IsNullOrEmpty(_selectedClassId)) return default;
        return _all.Find(r => r.id == _selectedClassId);
    }
}
