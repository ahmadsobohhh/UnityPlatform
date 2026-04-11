// Script: TeacherClassManager
// Path: Assets/Scripts/Teacher/ClassSelect/TeacherClassManager.cs
// Purpose: Handles teacher class CRUD, selection, and list rendering.

using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine.SceneManagement;
using System.Linq;

public class TeacherClassManager : MonoBehaviour
{
    static Sprite _solidUiSprite;

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

    [Header("Card Layout")]
    [SerializeField] private float classCardHeight = 80f;
    [SerializeField] private float fallbackClassCardWidth = 760f;

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

    [Header("Class row look")]
    [SerializeField] private Color rowFill = new Color(0.16f, 0.13f, 0.08f, 0.48f);
    [SerializeField] private Color rowFillHighlight = new Color(0.26f, 0.2f, 0.13f, 0.62f);
    [SerializeField] private Color rowFillPressed = new Color(0.11f, 0.09f, 0.06f, 0.72f);
    [SerializeField] private Color accentBarColor = new Color(0.98f, 0.86f, 0.58f, 0.85f);
    [SerializeField] private Color outlineColor = new Color(1f, 0.95f, 0.82f, 0.24f);
    [SerializeField] private Color titleColor = new Color(1f, 0.98f, 0.9f, 0.96f);
    [SerializeField] private Color subtitleColor = new Color(0.98f, 0.93f, 0.82f, 0.78f);
    [SerializeField] private Color chevronColor = new Color(1f, 0.9f, 0.7f, 0.82f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.16f);

    [Header("Font")]
    [SerializeField] private TMP_FontAsset cardFont;

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

        var byId = new Dictionary<string, ClassRow>();

        var indexQuery = db.Collection("users").Document(user.UserId).Collection("classes");
        var indexTask = indexQuery.GetSnapshotAsync();
        yield return new WaitUntil(() => indexTask.IsCompleted);
        if (indexTask.IsFaulted || indexTask.IsCanceled) { Debug.LogError(indexTask.Exception); yield break; }

        foreach (var d in indexTask.Result)
            UpsertRow(byId, SnapshotToRow(d));

        var ownedQuery = db.Collection("classes").WhereEqualTo("ownerUid", user.UserId);
        var ownedTask = ownedQuery.GetSnapshotAsync();
        yield return new WaitUntil(() => ownedTask.IsCompleted);
        if (ownedTask.IsFaulted || ownedTask.IsCanceled)
        {
            Debug.LogError("[TCM] Failed loading owned classes fallback: " + ownedTask.Exception);
        }
        else
        {
            foreach (var d in ownedTask.Result)
            {
                var row = SnapshotToRow(d);
                bool wasMissingInIndex = !byId.ContainsKey(row.id);
                UpsertRow(byId, row);

                if (wasMissingInIndex)
                    yield return StartCoroutine(RepairTeacherIndexRow(user.UserId, row));
            }
        }

        _all.Clear();
        _all.AddRange(byId.Values.Where(r => !string.IsNullOrEmpty(r.id)));

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

    private ClassRow SnapshotToRow(DocumentSnapshot d)
    {
        string id = d.ContainsField("id") ? d.GetValue<string>("id") : d.Id;
        string name = d.ContainsField("name") ? d.GetValue<string>("name") : "(Unnamed)";
        string code = d.ContainsField("code") ? d.GetValue<string>("code") : "—";

        long createdAtSec = 0;
        if (d.ContainsField("createdAt"))
        {
            var ts = d.GetValue<Firebase.Firestore.Timestamp>("createdAt");
            createdAtSec = (long)(ts.ToDateTime().ToUniversalTime() - System.DateTime.UnixEpoch).TotalSeconds;
        }

        return new ClassRow
        {
            id = id,
            name = string.IsNullOrWhiteSpace(name) ? "(Unnamed)" : name,
            code = string.IsNullOrWhiteSpace(code) ? "—" : code,
            createdAtSeconds = createdAtSec
        };
    }

    private void UpsertRow(Dictionary<string, ClassRow> map, ClassRow incoming)
    {
        if (string.IsNullOrEmpty(incoming.id))
            return;

        if (!map.TryGetValue(incoming.id, out var existing))
        {
            map[incoming.id] = incoming;
            return;
        }

        if (IsWeakName(existing.name) && !IsWeakName(incoming.name))
            existing.name = incoming.name;

        if (IsWeakCode(existing.code) && !IsWeakCode(incoming.code))
            existing.code = incoming.code;

        if (incoming.createdAtSeconds > existing.createdAtSeconds)
            existing.createdAtSeconds = incoming.createdAtSeconds;

        map[incoming.id] = existing;
    }

    private bool IsWeakName(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value == "(Unnamed)";
    }

    private bool IsWeakCode(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value == "—";
    }

    private IEnumerator RepairTeacherIndexRow(string teacherUid, ClassRow row)
    {
        if (string.IsNullOrEmpty(teacherUid) || string.IsNullOrEmpty(row.id))
            yield break;

        var createdAt = row.createdAtSeconds > 0
            ? Timestamp.FromDateTime(System.DateTime.UnixEpoch.AddSeconds(row.createdAtSeconds).ToUniversalTime())
            : Timestamp.GetCurrentTimestamp();

        var data = new Dictionary<string, object>
        {
            { "id", row.id },
            { "name", string.IsNullOrWhiteSpace(row.name) ? "(Unnamed)" : row.name },
            { "code", string.IsNullOrWhiteSpace(row.code) ? "—" : row.code },
            { "createdAt", createdAt }
        };

        var idxRef = db.Collection("users").Document(teacherUid).Collection("classes").Document(row.id);
        var repairTask = idxRef.SetAsync(data, SetOptions.MergeAll);
        yield return new WaitUntil(() => repairTask.IsCompleted);

        if (repairTask.IsFaulted || repairTask.IsCanceled)
            Debug.LogWarning("[TCM] Failed to repair teacher class index for " + row.id + ": " + repairTask.Exception);
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
            go.name = "ClassCard_" + row.id;
            go.transform.SetParent(classListContainer, false);
            go.SetActive(true);
            EnsureCardHasSize(go.GetComponent<RectTransform>());

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

        if (classListContainer is RectTransform containerRt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRt);
    }

    private GameObject CreateClassCard(string className, string code, bool selected)
    {
        var card = new GameObject("ClassCard", typeof(RectTransform));
        card.layer = 5;

        var rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, classCardHeight);

        var le = card.AddComponent<LayoutElement>();
        le.minHeight = classCardHeight;
        le.preferredHeight = classCardHeight;
        le.flexibleHeight = 0f;
        le.minWidth = fallbackClassCardWidth;
        le.preferredWidth = fallbackClassCardWidth;
        le.flexibleWidth = 1;

        var bg = card.AddComponent<Image>();
        bg.sprite = GetSolidUiSprite();
        bg.type = Image.Type.Simple;
        bg.color = selected ? rowFillHighlight : rowFill;
        bg.raycastTarget = true;
        bg.maskable = false;

        var shadow = card.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(0f, -3f);

        var outline = card.AddComponent<Outline>();
        outline.effectColor = selected ? new Color(1f, 0.95f, 0.82f, 0.45f) : outlineColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var btn = card.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.ColorTint;
        var cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white;
        cb.pressedColor = Color.white;
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(1f, 1f, 1f, 0.55f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        const float accentW = 6f;
        var accentGo = new GameObject("Accent");
        accentGo.transform.SetParent(card.transform, false);
        var accentRt = accentGo.AddComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.offsetMin = new Vector2(3f, 6f);
        accentRt.offsetMax = new Vector2(3f + accentW, -6f);
        var accentImg = accentGo.AddComponent<Image>();
        accentImg.sprite = GetSolidUiSprite();
        accentImg.color = selected ? new Color(1f, 0.9f, 0.64f, 0.95f) : accentBarColor;
        accentImg.raycastTarget = false;
        accentImg.maskable = false;

        var chevronGo = new GameObject("Chevron");
        chevronGo.transform.SetParent(card.transform, false);
        var chevRt = chevronGo.AddComponent<RectTransform>();
        chevRt.anchorMin = new Vector2(1f, 0f);
        chevRt.anchorMax = new Vector2(1f, 1f);
        chevRt.pivot = new Vector2(1f, 0.5f);
        chevRt.sizeDelta = new Vector2(40f, 0f);
        chevRt.anchoredPosition = new Vector2(-10f, 0f);
        var chev = chevronGo.AddComponent<TextMeshProUGUI>();
        chev.text = "\u203A";
        chev.fontSize = 40f;
        chev.fontStyle = FontStyles.Normal;
        chev.color = chevronColor;
        chev.alignment = TextAlignmentOptions.MidlineRight;
        chev.raycastTarget = false;
        chev.maskable = false;
        chev.enableAutoSizing = false;
        ApplyCardFont(chev);

        var textBlock = new GameObject("TextBlock");
        textBlock.transform.SetParent(card.transform, false);
        var tbRt = textBlock.AddComponent<RectTransform>();
        tbRt.anchorMin = Vector2.zero;
        tbRt.anchorMax = Vector2.one;
        float textLeft = 3f + accentW + 12f;
        float textRight = 46f;
        tbRt.offsetMin = new Vector2(textLeft, 10f);
        tbRt.offsetMax = new Vector2(-textRight, -10f);

        var vlg = textBlock.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 2, 0);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(textBlock.transform, false);
        var titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 38f;
        titleLe.flexibleHeight = 0f;
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = className;
        titleTmp.fontSize = 26f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = selected ? Color.white : titleColor;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.textWrappingMode = TextWrappingModes.NoWrap;
        titleTmp.overflowMode = TextOverflowModes.Ellipsis;
        titleTmp.raycastTarget = false;
        titleTmp.maskable = false;
        ApplyCardFont(titleTmp);

        var subGo = new GameObject("Subtitle");
        subGo.transform.SetParent(textBlock.transform, false);
        var subLe = subGo.AddComponent<LayoutElement>();
        subLe.preferredHeight = 24f;
        subLe.flexibleHeight = 0f;
        var subTmp = subGo.AddComponent<TextMeshProUGUI>();
        subTmp.text = $"Code: {code}";
        subTmp.fontSize = 17f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.color = selected ? new Color(1f, 1f, 1f, 0.85f) : subtitleColor;
        subTmp.alignment = TextAlignmentOptions.Left;
        subTmp.textWrappingMode = TextWrappingModes.NoWrap;
        subTmp.overflowMode = TextOverflowModes.Ellipsis;
        subTmp.raycastTarget = false;
        subTmp.maskable = false;
        ApplyCardFont(subTmp);

        return card;
    }

    private void EnsureCardHasSize(RectTransform rt)
    {
        if (rt == null)
            return;

        float containerWidth = 0f;
        if (classListContainer is RectTransform containerRt)
            containerWidth = containerRt.rect.width;

        float targetWidth = containerWidth > 1f ? containerWidth : fallbackClassCardWidth;

        rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, targetWidth);
        rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0f, classCardHeight);

        if (rt.sizeDelta.y < 1f)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, classCardHeight);
    }

    private void ApplyCardFont(TextMeshProUGUI tmp)
    {
        if (tmp != null && cardFont != null)
            tmp.font = cardFont;
    }

    private static Sprite GetSolidUiSprite()
    {
        if (_solidUiSprite != null)
            return _solidUiSprite;

        var tex = Texture2D.whiteTexture;
        _solidUiSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return _solidUiSprite;
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

