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
    [Header("References")]
    public Transform classContainer;
    public GameObject classButtonPrefab;
    public GameObject noClassesText;

    [Header("Scene")]
    public string classSceneName = "StudentClass";

    [Header("Card Style (when no prefab)")]
    [SerializeField] private Vector2 cardSize = new Vector2(0, 70);
    [SerializeField] private Color cardBgColor = new Color(1f, 1f, 1f, 0.06f);
    [SerializeField] private Color cardTextColor = new Color(0.92f, 0.88f, 0.80f, 1f);
    [SerializeField] private Color cardSubtextColor = new Color(0.70f, 0.65f, 0.55f, 0.8f);
    [SerializeField] private Color dividerColor = new Color(1f, 1f, 1f, 0.08f);

    [Header("Font")]
    public TMP_FontAsset cardFont;

    private static readonly string[] ShipIcons = { "\u2693", "\u2694", "\u2606", "\u2658", "\u265F", "\u2620" };

    FirebaseFirestore db;
    FirebaseAuth auth;
    private StudentHubAnimator hubAnimator;

    async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        hubAnimator = FindObjectOfType<StudentHubAnimator>();

        await Task.Delay(100);
        LoadClasses();
    }

    public async void RefreshClasses()
    {
        Debug.Log("[StudentClassLoader] RefreshClasses called");
        if (db == null) db = FirebaseFirestore.DefaultInstance;
        if (auth == null) auth = FirebaseAuth.DefaultInstance;
        if (classContainer == null)
        {
            Debug.LogError("[StudentClassLoader] classContainer is null during refresh!");
            return;
        }
        Debug.Log($"[StudentClassLoader] classContainer: {classContainer.name}, children: {classContainer.childCount}");
        await Task.Delay(500);
        LoadClasses(forceServer: true);
    }

    async void LoadClasses(bool forceServer = false)
    {
        if (classContainer == null)
        {
            Debug.LogError("classContainer NOT assigned");
            return;
        }

        if (db == null) db = FirebaseFirestore.DefaultInstance;
        if (auth == null) auth = FirebaseAuth.DefaultInstance;

        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No user logged in.");
            return;
        }

        try
        {
            QuerySnapshot snapshot;
            var query = db.Collection("users")
                .Document(user.UserId)
                .Collection("classes");

            if (forceServer)
                snapshot = await query.GetSnapshotAsync(Source.Server);
            else
                snapshot = await query.GetSnapshotAsync();

            Debug.Log($"[StudentClassLoader] Loaded {snapshot.Count} classes (forceServer={forceServer})");

            foreach (Transform child in classContainer)
                Destroy(child.gameObject);

            if (snapshot.Count == 0)
            {
                Debug.Log("[StudentClassLoader] No classes found");
                if (noClassesText != null)
                    noClassesText.SetActive(true);
                return;
            }

            if (noClassesText != null)
                noClassesText.SetActive(false);

            var classIds = new List<string>();
            int index = 0;
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

                if (classButtonPrefab != null)
                    CreateClassButton(className, classId, classCode);
                else
                    CreateParchmentCard(className, classId, classCode, index);

                index++;
            }

            if (hubAnimator != null)
                hubAnimator.AnimateCards();

            _ = RepairMemberNames(user.UserId, classIds);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load classes: " + e.Message);

            if (noClassesText != null)
                noClassesText.SetActive(true);

            foreach (Transform child in classContainer)
                Destroy(child.gameObject);
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
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OpenClass(className, classId, classCode));
        }
    }

    void CreateParchmentCard(string className, string classId, string classCode, int index)
    {
        var cardGO = new GameObject($"ClassCard_{classId}");
        cardGO.transform.SetParent(classContainer, false);

        var cardRect = cardGO.AddComponent<RectTransform>();
        var layout = cardGO.AddComponent<LayoutElement>();
        layout.preferredHeight = 70;
        layout.flexibleWidth = 1;

        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = (index % 2 == 0) ? cardBgColor : new Color(cardBgColor.r, cardBgColor.g, cardBgColor.b, cardBgColor.a * 0.5f);

        var btn = cardGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => OpenClass(className, classId, classCode));

        cardGO.AddComponent<ClassCardHover>();
        cardGO.AddComponent<CanvasGroup>();

        // Icon
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(cardGO.transform, false);
        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(12, 0);
        iconRect.sizeDelta = new Vector2(44, 44);
        var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text = ShipIcons[index % ShipIcons.Length];
        iconTMP.fontSize = 26;
        iconTMP.color = new Color(0.85f, 0.75f, 0.55f, 0.7f);
        iconTMP.alignment = TextAlignmentOptions.Center;
        iconTMP.raycastTarget = false;
        if (cardFont != null) iconTMP.font = cardFont;

        // Class name
        var nameGO = new GameObject("ClassName");
        nameGO.transform.SetParent(cardGO.transform, false);
        var nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(64, string.IsNullOrEmpty(classCode) ? 0 : 16);
        nameRect.offsetMax = new Vector2(-16, -8);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = className;
        nameTMP.fontSize = 20;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = cardTextColor;
        nameTMP.alignment = TextAlignmentOptions.Left;
        nameTMP.overflowMode = TextOverflowModes.Ellipsis;
        nameTMP.raycastTarget = false;
        if (cardFont != null) nameTMP.font = cardFont;

        // Code subtitle
        if (!string.IsNullOrEmpty(classCode))
        {
            var codeGO = new GameObject("ClassCode");
            codeGO.transform.SetParent(cardGO.transform, false);
            var codeRect = codeGO.AddComponent<RectTransform>();
            codeRect.anchorMin = new Vector2(0, 0);
            codeRect.anchorMax = new Vector2(1, 0);
            codeRect.pivot = new Vector2(0, 0);
            codeRect.anchoredPosition = new Vector2(64, 6);
            codeRect.sizeDelta = new Vector2(-80, 20);
            var codeTMP = codeGO.AddComponent<TextMeshProUGUI>();
            codeTMP.text = $"Code: {classCode}";
            codeTMP.fontSize = 13;
            codeTMP.fontStyle = FontStyles.Italic;
            codeTMP.color = cardSubtextColor;
            codeTMP.alignment = TextAlignmentOptions.Left;
            codeTMP.raycastTarget = false;
            if (cardFont != null) codeTMP.font = cardFont;
        }

        // Bottom divider line
        var divGO = new GameObject("Divider");
        divGO.transform.SetParent(cardGO.transform, false);
        var divRect = divGO.AddComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0, 0);
        divRect.anchorMax = new Vector2(1, 0);
        divRect.pivot = new Vector2(0.5f, 0);
        divRect.anchoredPosition = Vector2.zero;
        divRect.sizeDelta = new Vector2(-24, 1);
        var divImg = divGO.AddComponent<Image>();
        divImg.color = dividerColor;
        divImg.raycastTarget = false;

        // Arrow indicator on right
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(cardGO.transform, false);
        var arrowRect = arrowGO.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-12, 0);
        arrowRect.sizeDelta = new Vector2(24, 24);
        var arrowTMP = arrowGO.AddComponent<TextMeshProUGUI>();
        arrowTMP.text = "\u25B6";
        arrowTMP.fontSize = 16;
        arrowTMP.color = new Color(0.85f, 0.75f, 0.55f, 0.4f);
        arrowTMP.alignment = TextAlignmentOptions.Center;
        arrowTMP.raycastTarget = false;
        if (cardFont != null) arrowTMP.font = cardFont;
    }

    private void OpenClass(string className, string classId, string classCode)
    {
        Debug.Log("Opening class: " + className);

        PlayerPrefs.SetString("SelectedClassId", classId);
        PlayerPrefs.SetString("SelectedClassName", className);
        PlayerPrefs.SetString("SelectedClassCode", classCode);
        PlayerPrefs.Save();

        SceneManager.LoadScene(classSceneName);
    }
}
