using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine.SceneManagement;
public class StudentCharacterSelect : MonoBehaviour
{
    [Header("Character Slots (Character1 → Character8)")]
    public List<GameObject> characterSlots;  // Drag all Character GameObjects (size 8)
    public GameObject joinGUI;
    public GameObject classInfo;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color dimColor = new Color(55, 55, 0); // darker for unused/locked

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // Count of classrooms the student already joined (0..8)
    private int classroomCount = 0;

    // Cache of joined classrooms in slot order
    private struct ClassroomInfo
    {
        public string id;
        public string name;
        public string code;
        public Timestamp joinedAt;
    }
    private List<ClassroomInfo> joinedClassrooms = new List<ClassroomInfo>(8);

    // Map: slot index -> classId (only for occupied slots)
    private Dictionary<int, string> slotIndexToClassId = new Dictionary<int, string>(8);

    [Header("Join UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinConfirmButton;
    [SerializeField] private TMP_Text joinStatusLabel;   // optional

    [Header("Class Info UI")]
    [SerializeField] private TMP_Text classInfoNameText;
    [SerializeField] private TMP_Text classInfoLevelText;
    [SerializeField] private TMP_Text classInfoXpText;
    [SerializeField] private TMP_Text classInfoGoldText;
    [SerializeField] private Button classInfoJoinButton;
    [SerializeField] private Button classInfoCloseButton;

    // ---------------------------
    // Unity lifecycle
    // ---------------------------
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (joinGUI) joinGUI.SetActive(false);
        if (classInfo) classInfo.SetActive(false);

        // --- Wire Join button (Join GUI)
        if (joinConfirmButton)
        {
            joinConfirmButton.onClick.RemoveAllListeners();
            joinConfirmButton.onClick.AddListener(() =>
            {
                if (joinStatusLabel) joinStatusLabel.text = "";
                string code = joinCodeInput ? joinCodeInput.text.Trim().ToUpperInvariant() : "";
                StartCoroutine(JoinByCodeRoutine(code));
            });
        }

        // --- Wire ClassInfo close (optional)
        if (classInfoCloseButton)
        {
            classInfoCloseButton.onClick.RemoveAllListeners();
            classInfoCloseButton.onClick.AddListener(() =>
            {
                if (classInfo) classInfo.SetActive(false);
            });
        }

        // Initial fetch & paint
        StartCoroutine(CheckStudentClassrooms());
    }

    // ---------------------------
    // Join by Code (existing flow)
    // ---------------------------
    private IEnumerator JoinByCodeRoutine(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            if (joinStatusLabel) joinStatusLabel.text = "Enter a class code.";
            yield break;
        }

        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No signed-in user found!");
            if (joinStatusLabel) joinStatusLabel.text = "Not signed in.";
            yield break;
        }

        // 1) Find class by code in /classes
        var findTask = db.Collection("classes").WhereEqualTo("code", code).Limit(1).GetSnapshotAsync();
        yield return new WaitUntil(() => findTask.IsCompleted);

        if (findTask.IsFaulted || findTask.IsCanceled)
        {
            Debug.LogError(findTask.Exception);
            if (joinStatusLabel) joinStatusLabel.text = "Network error. Try again.";
            yield break;
        }

        var querySnap = findTask.Result;
        if (querySnap.Count == 0)
        {
            if (joinStatusLabel) joinStatusLabel.text = "Invalid code.";
            yield break;
        }

        // Grab the single matching class
        DocumentSnapshot classDoc = null;
        foreach (var d in querySnap.Documents) { classDoc = d; break; }

        string classId   = classDoc.Id;
        string className = classDoc.ContainsField("name") ? classDoc.GetValue<string>("name") : "(Unnamed)";
        string classCode = classDoc.ContainsField("code") ? classDoc.GetValue<string>("code") : code;

        var now = Timestamp.GetCurrentTimestamp();

        // 2) Index under the student: /users/{uid}/classrooms/{classId}
        var studentIdxRef = db.Collection("users").Document(user.UserId).Collection("classrooms").Document(classId);
        var studentIdxData = new Dictionary<string, object> {
            { "id", classId },
            { "name", className },
            { "code", classCode },
            { "joinedAt", now }
        };
        var addIdxTask = studentIdxRef.SetAsync(studentIdxData);

        // 3) (Optional) add membership under class: /classes/{classId}/members/{uid}
        var memberRef = db.Collection("classes").Document(classId).Collection("members").Document(user.UserId);
        var memberData = new Dictionary<string, object> {
            { "uid", user.UserId },
            { "joinedAt", now }
            // optionally seed stats here: { "level", 1 }, { "xp", 0 }, { "gold", 0 }
        };
        var addMemberTask = memberRef.SetAsync(memberData);

        yield return new WaitUntil(() => addIdxTask.IsCompleted && addMemberTask.IsCompleted);

        if (addIdxTask.IsFaulted || addMemberTask.IsFaulted)
        {
            Debug.LogError("Join failed: " + (addIdxTask.Exception ?? addMemberTask.Exception));
            if (joinStatusLabel) joinStatusLabel.text = "Could not join. Try again.";
            yield break;
        }

        // 4) Success: refresh UI and (optionally) transition
        if (joinStatusLabel) joinStatusLabel.text = "Joined!";
        if (joinGUI) joinGUI.SetActive(false);

        // Refresh slots immediately so next one unlocks
        yield return StartCoroutine(CheckStudentClassrooms());

        // Pass to next scene (keep existing behavior)
        ClassSelection.CurrentClassId = classId;
        ClassSelection.CurrentClassName = className;
        ClassSelection.CurrentClassCode = classCode;

        SceneManager.LoadScene("StudentClass");
    }

    // ---------------------------
    // Load student classrooms and paint
    // ---------------------------
    private IEnumerator CheckStudentClassrooms()
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No signed-in user found!");
            yield break;
        }

        // 🔍 Fetch the student's classrooms (order by joinedAt so slot order is stable)
        var classroomsRef = db.Collection("users").Document(user.UserId).Collection("classrooms").OrderBy("joinedAt");
        var task = classroomsRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("Failed to load classrooms: " + task.Exception);
            yield break;
        }

        QuerySnapshot snapshot = task.Result;

        // Rebuild cache
        joinedClassrooms.Clear();
        int i = 0;
        foreach (var doc in snapshot.Documents)
        {
            if (i >= 8) break; // cap to 8 slots
            ClassroomInfo info = new ClassroomInfo();
            info.id = doc.Id;
            info.name = doc.ContainsField("name") ? doc.GetValue<string>("name") : "(Class)";
            info.code = doc.ContainsField("code") ? doc.GetValue<string>("code") : "";
            info.joinedAt = doc.ContainsField("joinedAt") ? doc.GetValue<Timestamp>("joinedAt") : Timestamp.GetCurrentTimestamp();
            joinedClassrooms.Add(info);
            i++;
        }

        classroomCount = joinedClassrooms.Count;
        Debug.Log("Student has " + classroomCount + " classrooms.");

        ApplySlotColors();
        AssignSlotListeners();
    }

    // ---------------------------
    // Visual state for each slot
    // ---------------------------
    private void ApplySlotColors()
    {
        int total = characterSlots != null ? characterSlots.Count : 0;
        for (int i = 0; i < total; i++)
        {
            Image slotImage = characterSlots[i].GetComponent<Image>();
            if (slotImage == null) continue;

            // Bright:
            // - All occupied indices: i < classroomCount
            // - Next empty index: i == classroomCount (if we still have capacity)
            bool isOccupied = (i < classroomCount);
            bool isNextEmpty = (i == classroomCount) && (classroomCount < total);

            slotImage.color = (isOccupied || isNextEmpty) ? normalColor : dimColor;

            // Also toggle Button.interactable for extra clarity
            Button btn = characterSlots[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = (isOccupied || isNextEmpty);
            }
        }
    }

    // ---------------------------
    // Click behaviors for slots
    // ---------------------------
    private void AssignSlotListeners()
    {
        slotIndexToClassId.Clear();

        int total = characterSlots != null ? characterSlots.Count : 0;

        // Clear existing listeners
        for (int i = 0; i < total; i++)
        {
            Button btn = characterSlots[i].GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
        }

        // Occupied slots (0..classroomCount-1): open ClassInfo
        for (int i = 0; i < classroomCount && i < total; i++)
        {
            var btn = characterSlots[i].GetComponent<Button>();
            if (btn == null) continue;

            // Map slot -> classId
            slotIndexToClassId[i] = joinedClassrooms[i].id;

            int captured = i;
            btn.onClick.AddListener(() =>
            {
                OnSlotSelectedOccupied(captured);
            });
        }

        // Next empty (classroomCount): open JoinGUI (if capacity remains)
        if (classroomCount < total)
        {
            var btn = characterSlots[classroomCount].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    OnSlotSelectedNextEmpty();
                });
            }
        }

        // Remaining locked do nothing (already non-interactable from ApplySlotColors)
    }

    // --- Occupied slot clicked → open ClassInfo
    private void OnSlotSelectedOccupied(int index)
    {
        Debug.Log("Clicked OCCUPIED Character " + (index + 1));

        if (classInfo) classInfo.SetActive(true);
        if (joinGUI) joinGUI.SetActive(false);

        string classId = slotIndexToClassId.ContainsKey(index) ? slotIndexToClassId[index] : null;
        if (string.IsNullOrEmpty(classId))
        {
            Debug.LogWarning("No classId cached for occupied slot " + index);
            return;
        }

        StartCoroutine(OpenAndPopulateClassInfo(classId, index));
    }

    // --- Next empty slot clicked → open Join GUI
    private void OnSlotSelectedNextEmpty()
    {
        Debug.Log("Clicked NEXT EMPTY Character " + (classroomCount + 1));
        if (classInfo) classInfo.SetActive(false);
        if (joinGUI) joinGUI.SetActive(true);
        if (joinStatusLabel) joinStatusLabel.text = "";
        if (joinCodeInput) joinCodeInput.text = "";
    }

    // ---------------------------
    // Populate ClassInfo panel
    // ---------------------------
    private IEnumerator OpenAndPopulateClassInfo(string classId, int index)
    {
        var user = auth.CurrentUser;
        if (user == null) yield break;

        // 1) Try to read quick info from student's index (name, code)
        var idxRef = db.Collection("users").Document(user.UserId).Collection("classrooms").Document(classId);
        var idxTask = idxRef.GetSnapshotAsync();
        yield return new WaitUntil(() => idxTask.IsCompleted);

        string className = "(Class)";
        if (!idxTask.IsFaulted && !idxTask.IsCanceled && idxTask.Result.Exists)
        {
            var snap = idxTask.Result;
            className = snap.ContainsField("name") ? snap.GetValue<string>("name") : "(Class)";
        }

        // 2) Read player stats from /classes/{classId}/members/{uid}
        int level = 1;
        int xp = 0;
        int gold = 0;

        var memberRef = db.Collection("classes").Document(classId).Collection("members").Document(user.UserId);
        var memberTask = memberRef.GetSnapshotAsync();
        yield return new WaitUntil(() => memberTask.IsCompleted);

        if (!memberTask.IsFaulted && !memberTask.IsCanceled && memberTask.Result.Exists)
        {
            var m = memberTask.Result;
            if (m.ContainsField("level")) level = m.GetValue<int>("level");
            if (m.ContainsField("xp")) xp = m.GetValue<int>("xp");
            if (m.ContainsField("gold")) gold = m.GetValue<int>("gold");
        }

        // 3) Populate UI fields
        if (classInfoNameText) classInfoNameText.text = className;
        if (classInfoLevelText) classInfoLevelText.text = "Level: " + level;
        if (classInfoXpText) classInfoXpText.text = "XP: " + xp;
        if (classInfoGoldText) classInfoGoldText.text = "Gold: " + gold;

        // 4) Wire Join button to enter this classroom
        if (classInfoJoinButton)
        {
            classInfoJoinButton.onClick.RemoveAllListeners();
            classInfoJoinButton.onClick.AddListener(() =>
            {
                ClassSelection.CurrentClassId = classId;
                ClassSelection.CurrentClassName = className;
                // if needed: ClassSelection.CurrentClassCode = ... (not required here)
                SceneManager.LoadScene("StudentClass");
            });
        }
    }
    public void SignOut()
    {
        auth.SignOut();
        SceneManager.LoadScene("WelcomePage");
    }

    // ---------------------------
    // Utility: click-outside to close JoinGUI
    // ---------------------------
    void Update()
    {
        // If JoinGUI is active, detect click outside
        if (joinGUI != null && joinGUI.activeSelf && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            RectTransform rect = joinGUI.GetComponent<RectTransform>();
            if (rect != null && !RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
            {
                // Hide JoinGUI when clicked outside
                joinGUI.SetActive(false);
            }
        }

        // If classInfo is active, detect click outside
        if (classInfo != null && classInfo.activeSelf && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            RectTransform rect = classInfo.GetComponent<RectTransform>();
            if (rect != null && !RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
            {
                // Hide classInfo when clicked outside
                classInfo.SetActive(false);
            }
        }
    }
}
