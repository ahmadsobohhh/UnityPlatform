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
    static Sprite _solidUiSprite;

    [Header("References (auto-found if left empty)")]
    public Transform classContainer;
    public GameObject noClassesText;

    [Header("Scene")]
    public string classSceneName = "ClassroomScene";

    [Header("Class row look")]
    [SerializeField] private float rowHeight = 100f;
    [SerializeField] private Color rowFill = new Color(0.97f, 0.94f, 0.86f, 1f);
    [SerializeField] private Color rowFillHighlight = new Color(1f, 0.98f, 0.92f, 1f);
    [SerializeField] private Color rowFillPressed = new Color(0.9f, 0.86f, 0.76f, 1f);
    [SerializeField] private Color accentBarColor = new Color(0.72f, 0.52f, 0.28f, 1f);
    [SerializeField] private Color outlineColor = new Color(0.32f, 0.22f, 0.14f, 0.85f);
    [SerializeField] private Color titleColor = new Color(0.22f, 0.14f, 0.08f, 1f);
    [SerializeField] private Color subtitleColor = new Color(0.42f, 0.32f, 0.22f, 0.92f);
    [SerializeField] private Color chevronColor = new Color(0.55f, 0.4f, 0.26f, 0.75f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.22f);
    [SerializeField] private Color probeRowFill = new Color(0.88f, 0.94f, 0.86f, 1f);
    [SerializeField] private Color probeAccentColor = new Color(0.35f, 0.58f, 0.32f, 1f);

    [Header("Visual style")]
    [SerializeField] private bool useGlassyClassCards = true;

    [Header("Scroll feel")]
    [SerializeField] private float scrollSensitivity = 6f;
    [SerializeField] private float scrollDecelerationRate = 0.065f;
    [SerializeField] private float scrollElasticity = 0.06f;

    [Header("Font")]
    public TMP_FontAsset cardFont;

    [Header("Load reveal")]
    [SerializeField] private bool waitForHubIntro = true;
    [SerializeField] private float maxHubIntroWait = 3.5f;
    [SerializeField] private float overlayRevealDelay = 0.25f;
    [SerializeField] private float overlayRevealDuration = 0.18f;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private CanvasGroup overlayGroup;
    private CanvasGroup classContainerGroup;
    private CanvasGroup overlayTitleGroup;

    async void Start()
    {
        var all = FindObjectsByType<StudentClassLoader>(FindObjectsSortMode.InstanceID);
        if (all.Length > 1 && all[0] != this)
        {
            Debug.LogWarning("[StudentClassLoader] Duplicate instance detected — disabling this one.");
            Destroy(this);
            return;
        }

        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        if (classContainer == null)
        {
            var go = GameObject.Find("ClassContainer");
            if (go != null)
                classContainer = go.transform;
        }

        if (noClassesText == null)
        {
            var go = GameObject.Find("NoClassesText");
            if (go != null)
                noClassesText = go;
        }

        if (classContainer != null)
        {
            if (useGlassyClassCards)
                ApplyGlassyClassCardTheme();

            EnsureLayoutSetup();
            EnsureOverlayTitle();
            CacheOverlayGroups();
            SetLoadingStateVisible(false);
        }

        // Keep reveal snappy even if old scene-serialized values are still high.
        overlayRevealDelay = Mathf.Clamp(overlayRevealDelay, 0f, 0.3f);
        overlayRevealDuration = Mathf.Clamp(overlayRevealDuration, 0.12f, 0.22f);

        await Task.Delay(50);
        LoadClasses();
    }

    public void ReloadClassesAfterJoin()
    {
        if (classContainer == null)
            return;

        SetLoadingStateVisible(false);
        LoadClasses();
    }

    private void ApplyGlassyClassCardTheme()
    {
        rowFill = new Color(0.16f, 0.13f, 0.08f, 0.48f);
        rowFillHighlight = new Color(0.26f, 0.2f, 0.13f, 0.62f);
        rowFillPressed = new Color(0.11f, 0.09f, 0.06f, 0.72f);
        accentBarColor = new Color(0.98f, 0.86f, 0.58f, 0.85f);
        outlineColor = new Color(1f, 0.95f, 0.82f, 0.24f);
        titleColor = new Color(1f, 0.98f, 0.9f, 0.96f);
        subtitleColor = new Color(0.98f, 0.93f, 0.82f, 0.78f);
        chevronColor = new Color(1f, 0.9f, 0.7f, 0.82f);
        shadowColor = new Color(0f, 0f, 0f, 0.16f);
        probeRowFill = new Color(0.2f, 0.22f, 0.17f, 0.44f);
        probeAccentColor = new Color(0.62f, 0.79f, 0.54f, 0.75f);
    }

    private void CacheOverlayGroups()
    {
        if (classContainer == null) return;

        var scroll = classContainer.GetComponentInParent<ScrollRect>();
        var overlayRoot = scroll != null ? scroll.transform.parent : classContainer.parent;

        if (overlayRoot != null)
        {
            overlayGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (overlayGroup == null)
                overlayGroup = overlayRoot.gameObject.AddComponent<CanvasGroup>();
        }

        classContainerGroup = classContainer.GetComponent<CanvasGroup>();
        if (classContainerGroup == null)
            classContainerGroup = classContainer.gameObject.AddComponent<CanvasGroup>();

        var title = GameObject.Find("YourClassesTitle");
        if (title != null)
        {
            overlayTitleGroup = title.GetComponent<CanvasGroup>();
            if (overlayTitleGroup == null)
                overlayTitleGroup = title.AddComponent<CanvasGroup>();
        }
    }

    private void SetLoadingStateVisible(bool isVisible)
    {
        float alpha = isVisible ? 1f : 0f;

        if (overlayGroup != null)
        {
            overlayGroup.alpha = alpha;
            overlayGroup.interactable = isVisible;
            overlayGroup.blocksRaycasts = isVisible;
        }

        if (classContainerGroup != null)
        {
            classContainerGroup.alpha = alpha;
            classContainerGroup.interactable = isVisible;
            classContainerGroup.blocksRaycasts = isVisible;
        }

        if (overlayTitleGroup != null)
        {
            overlayTitleGroup.alpha = alpha;
            overlayTitleGroup.interactable = false;
            overlayTitleGroup.blocksRaycasts = false;
        }
    }

    private System.Collections.IEnumerator RevealLoadedOverlay()
    {
        if (waitForHubIntro)
        {
            var hubAnimator = FindFirstObjectByType<StudentHubAnimator>();
            float waited = 0f;

            while (hubAnimator != null && !hubAnimator.IsIntroComplete && waited < maxHubIntroWait)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, overlayRevealDelay));

        float t = 0f;
        while (t < overlayRevealDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.01f, overlayRevealDuration));

            if (overlayGroup != null)
            {
                overlayGroup.alpha = a;
                overlayGroup.interactable = a > 0.99f;
                overlayGroup.blocksRaycasts = a > 0.99f;
            }

            if (classContainerGroup != null)
            {
                classContainerGroup.alpha = a;
                classContainerGroup.interactable = a > 0.99f;
                classContainerGroup.blocksRaycasts = a > 0.99f;
            }

            if (overlayTitleGroup != null)
                overlayTitleGroup.alpha = a;

            yield return null;
        }

        SetLoadingStateVisible(true);
    }

    private void EnsureLayoutSetup()
    {
        var vlg = classContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = classContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 16, 16);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
        }

        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = classContainer.GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = classContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ConfigureScrollFeel();
    }

    private void ConfigureScrollFeel()
    {
        var scroll = classContainer.GetComponentInParent<ScrollRect>();
        if (scroll == null) return;

        scroll.inertia = true;
        scroll.scrollSensitivity = Mathf.Max(1f, scrollSensitivity);
        scroll.decelerationRate = Mathf.Clamp01(scrollDecelerationRate);
        scroll.elasticity = Mathf.Clamp(scrollElasticity, 0f, 1f);
    }

    private void EnsureOverlayTitle()
    {
        var scroll = classContainer.GetComponentInParent<ScrollRect>();
        Transform overlayRoot = scroll != null ? scroll.transform.parent : classContainer.parent;
        if (overlayRoot == null) return;

        var overlayRt = overlayRoot as RectTransform;
        if (overlayRt == null)
            overlayRt = overlayRoot.GetComponent<RectTransform>();

        Transform titleParent = overlayRoot.parent != null ? overlayRoot.parent : overlayRoot;

        Transform existing = GameObject.Find("YourClassesTitle")?.transform;

        GameObject titleGo;
        if (existing != null)
        {
            titleGo = existing.gameObject;
            titleGo.transform.SetParent(titleParent, false);
        }
        else
        {
            titleGo = new GameObject("YourClassesTitle");
            titleGo.transform.SetParent(titleParent, false);
        }

        var titleRt = titleGo.GetComponent<RectTransform>();
        if (titleRt == null)
            titleRt = titleGo.AddComponent<RectTransform>();

        float overlayY = overlayRt != null ? overlayRt.anchoredPosition.y : 0f;
        float overlayHalfHeight = overlayRt != null ? overlayRt.rect.height * 0.5f : 120f;
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0f);
        titleRt.anchoredPosition = new Vector2(0f, overlayY + overlayHalfHeight + 14f);
        titleRt.sizeDelta = new Vector2(480f, 58f);
        titleRt.SetAsLastSibling();

        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        if (titleTmp == null)
            titleTmp = titleGo.AddComponent<TextMeshProUGUI>();

        titleTmp.text = "Your Classes!";
        titleTmp.fontSize = 38f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.raycastTarget = false;
        titleTmp.enableWordWrapping = false;
        ApplyCardFont(titleTmp);
    }

    async void LoadClasses()
    {
        if (classContainer == null)
        {
            Debug.LogError("[StudentClassLoader] classContainer is null and could not be auto-found.");
            return;
        }

        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("[StudentClassLoader] No user logged in.");
            return;
        }

        try
        {
            Debug.Log($"[StudentClassLoader] Loading classes for user {user.UserId}");

            var snapshot = await db.Collection("users")
                .Document(user.UserId)
                .Collection("classes")
                .GetSnapshotAsync();

            foreach (Transform child in classContainer)
                Destroy(child.gameObject);

            if (snapshot.Count == 0)
            {
                if (noClassesText != null)
                    noClassesText.SetActive(false);

                CreateClassRow(
                    "No classes yet",
                    "Use Join Class below to add one",
                    isProbe: true,
                    className: "",
                    classId: "",
                    classCode: "",
                    clickable: false);

                RebuildAndAnimate();
                return;
            }

            Debug.Log($"[StudentClassLoader] Found {snapshot.Count} class(es).");

            if (noClassesText != null)
                noClassesText.SetActive(false);

            var classIds = new List<string>();
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

                string subtitle = string.IsNullOrEmpty(classCode)
                    ? "Tap to open"
                    : $"Code · {classCode}";

                CreateClassRow(className, subtitle, isProbe: false, className, classId, classCode, clickable: true);
            }

            RebuildAndAnimate();
            _ = RepairMemberNames(user.UserId, classIds);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[StudentClassLoader] Failed to load classes: " + e);

            if (noClassesText != null)
                noClassesText.SetActive(true);

            foreach (Transform child in classContainer)
                Destroy(child.gameObject);
        }
    }

    private void RebuildAndAnimate()
    {
        var rt = classContainer as RectTransform;
        if (rt == null) rt = classContainer.GetComponent<RectTransform>();
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        var scroll = classContainer.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;

        ForceClassRowsVisible();
        StopCoroutine(nameof(RevealLoadedOverlay));
        StartCoroutine(nameof(RevealLoadedOverlay));
    }

    /// <summary>
    /// Ensure generated rows are visible once overlay reveal begins.
    /// </summary>
    void ForceClassRowsVisible()
    {
        classContainer.gameObject.SetActive(true);

        foreach (Transform child in classContainer)
        {
            child.gameObject.SetActive(true);
            var g = child.GetComponent<CanvasGroup>();
            if (g != null)
            {
                g.alpha = 1f;
                g.interactable = true;
                g.blocksRaycasts = true;
            }
        }
    }

    void CreateClassRow(
        string title,
        string subtitle,
        bool isProbe,
        string className,
        string classId,
        string classCode,
        bool clickable)
    {
        var row = new GameObject(isProbe ? "EmptyState_ClassRow" : $"ClassBox_{classId}");
        row.transform.SetParent(classContainer, false);

        var rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, rowHeight);

        var layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = rowHeight;
        layout.flexibleWidth = 1;

        var img = row.AddComponent<Image>();
        img.sprite = GetSolidUiSprite();
        img.type = Image.Type.Simple;
        img.color = isProbe ? probeRowFill : rowFill;

        var shadow = row.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(0f, -3f);

        var outline = row.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        if (clickable && !string.IsNullOrEmpty(classId))
        {
            var btn = row.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            string cn = className, cid = classId, cc = classCode;
            btn.onClick.AddListener(() => OpenClass(cn, cid, cc));

            var normal = isProbe ? probeRowFill : rowFill;
            img.color = normal;
            var trigger = row.AddComponent<StudentClassRowButtonColors>();
            trigger.Bind(img, normal, rowFillHighlight, rowFillPressed);
        }

        var rowGroup = row.AddComponent<CanvasGroup>();
        rowGroup.alpha = 1f;
        rowGroup.interactable = true;
        rowGroup.blocksRaycasts = true;

        const float accentW = 6f;
        var accentGo = new GameObject("Accent");
        accentGo.transform.SetParent(row.transform, false);
        var accentRt = accentGo.AddComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.offsetMin = new Vector2(3f, 6f);
        accentRt.offsetMax = new Vector2(3f + accentW, -6f);
        var accentImg = accentGo.AddComponent<Image>();
        accentImg.sprite = GetSolidUiSprite();
        accentImg.color = isProbe ? probeAccentColor : accentBarColor;
        accentImg.raycastTarget = false;

        if (clickable)
        {
            var chevronGo = new GameObject("Chevron");
            chevronGo.transform.SetParent(row.transform, false);
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
            chev.enableAutoSizing = false;
            ApplyCardFont(chev);
        }

        var textBlock = new GameObject("TextBlock");
        textBlock.transform.SetParent(row.transform, false);
        var tbRt = textBlock.AddComponent<RectTransform>();
        tbRt.anchorMin = Vector2.zero;
        tbRt.anchorMax = Vector2.one;
        float textLeft = 3f + accentW + 12f;
        float textRight = clickable ? 46f : 16f;
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
        titleTmp.text = title;
        titleTmp.fontSize = 26f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = titleColor;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.enableWordWrapping = false;
        titleTmp.overflowMode = TextOverflowModes.Ellipsis;
        titleTmp.raycastTarget = false;
        ApplyCardFont(titleTmp);

        var subGo = new GameObject("Subtitle");
        subGo.transform.SetParent(textBlock.transform, false);
        var subLe = subGo.AddComponent<LayoutElement>();
        subLe.preferredHeight = 24f;
        subLe.flexibleHeight = 0f;
        var subTmp = subGo.AddComponent<TextMeshProUGUI>();
        subTmp.text = subtitle;
        subTmp.fontSize = 17f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.color = subtitleColor;
        subTmp.alignment = TextAlignmentOptions.Left;
        subTmp.enableWordWrapping = false;
        subTmp.overflowMode = TextOverflowModes.Ellipsis;
        subTmp.raycastTarget = false;
        ApplyCardFont(subTmp);
    }

    void ApplyCardFont(TextMeshProUGUI tmp)
    {
        if (cardFont != null)
            tmp.font = cardFont;
    }

    /// <summary>Unity UI Image draws nothing when sprite is null; use a 1×1 white sprite and tint with color.</summary>
    static Sprite GetSolidUiSprite()
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
            Debug.LogWarning("[StudentClassLoader] RepairMemberNames failed (non-critical): " + e.Message);
        }
    }

    private void OpenClass(string className, string classId, string classCode)
    {
        Debug.Log("[StudentClassLoader] Opening class: " + className);

        PlayerPrefs.SetString("SelectedClassId", classId);
        PlayerPrefs.SetString("SelectedClassName", className);
        PlayerPrefs.SetString("SelectedClassCode", classCode);
        PlayerPrefs.Save();

        SceneManager.LoadScene(classSceneName);
    }
}
