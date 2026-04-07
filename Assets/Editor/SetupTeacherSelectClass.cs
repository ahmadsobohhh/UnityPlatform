using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupTeacherSelectClass
{
    const string BG_PATH = "Assets/Images/Teacher/ClassSelect/TeacherClassSelectBG.png";

    static TMP_FontAsset menuFont;

    [MenuItem("Tools/Rework Teacher Class Select UI")]
    public static void Run()
    {
        LoadFont();
        string scenePath = "Assets/Scenes/TeacherPages/TeacherClassSelect.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvas not found in TeacherClassSelect.", "OK");
            return;
        }

        CleanupOldObjects(canvas.transform);
        FixCanvasScaler(canvas);
        SetupBackground(canvas.transform);
        SetupDarkOverlay(canvas.transform);
        EnsureFloatingParticles(canvas.transform);
        BuildClassListPanel(canvas.transform);
        BuildCreateClassPanel(canvas.transform);
        BuildEditPanel(canvas.transform);
        EnsureClassListItemPrefab(canvas.transform);
        BuildSignOutButton(canvas.transform);
        BuildBottomRightButtons(canvas.transform);
        BuildSettingsUI(canvas.transform);
        EnsureFadeOverlay(canvas.transform);
        EnsureAudioManager();
        WireTeacherClassManager(canvas);
        WireBackgroundParallax(canvas);
        ApplyFontToAll(canvas.transform);
        FixSiblingOrder(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Done",
            "TeacherClassSelect UI rebuilt!\n\n" +
            "  Background (captain hat)\n" +
            "  DarkOverlay\n" +
            "  FloatingParticles\n" +
            "  ClassListPanel (dark transparent)\n" +
            "  CreateClassPanel (popup)\n" +
            "  EditPanel (popup)\n" +
            "  SignOutBtn (bottom-left)\n" +
            "  BottomRightBar (Edit + Create)\n" +
            "  SettingsBtn + SettingsPanel\n" +
            "  FadeOverlay", "OK");
    }

    // ────────────────────────── CLEANUP ──────────────────────────

    static void CleanupOldObjects(Transform canvas)
    {
        string[] stale = {
            "DarkOverlay", "FloatingParticles", "ClassListPanel", "CreateClassPanel", "EditPanel",
            "BottomBar", "BottomRightBar", "FadeOverlay", "Header",
            "VideoBackground", "AnimatedBackground", "ClassManager",
            "SignOutBtn", "SignoutBtn", "SettingsBtn", "SettingsPanel",
            "BeginAdventure", "BeginBtn", "AdventureBtn", "ActionRow",
            "ClassList"
        };
        foreach (var n in stale)
            DestroyChild(canvas, n);

        // Also destroy any root-level objects that shouldn't exist
        foreach (string rootName in new[] { "BeginAdventure", "AdventureBtn", "SignOutManager", "SignoutBtn" })
        {
            var obj = GameObject.Find(rootName);
            if (obj != null) Object.DestroyImmediate(obj);
        }
    }

    // ────────────────────────── CANVAS ──────────────────────────

    static void FixCanvasScaler(GameObject canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    // ────────────────────────── BACKGROUND ──────────────────────────

    static void SetupBackground(Transform canvas)
    {
        Transform bg = null;
        foreach (Transform child in canvas)
        {
            if (child.name == "Background")
            {
                bg = child;
                break;
            }
        }

        if (bg == null)
        {
            var bgGO = new GameObject("Background");
            bgGO.layer = 5;
            bgGO.transform.SetParent(canvas, false);
            bgGO.AddComponent<RectTransform>();
            bgGO.AddComponent<Image>();
            bg = bgGO.transform;
        }
        bg.SetAsFirstSibling();

        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(-50, -50);
        bgRect.offsetMax = new Vector2(50, 50);

        ImportBackgroundSprite();

        var bgImg = bg.GetComponent<Image>();
        if (bgImg == null) bgImg = bg.gameObject.AddComponent<Image>();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BG_PATH);
        if (sprite == null)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(BG_PATH);
            foreach (var asset in allAssets)
            {
                if (asset is Sprite s) { sprite = s; break; }
            }
        }

        if (sprite != null)
        {
            bgImg.sprite = sprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;
        }
        else
        {
            Debug.LogError($"[SetupTeacherSelectClass] Could not load sprite from {BG_PATH}");
        }
    }

    static void ImportBackgroundSprite()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(BG_PATH) as TextureImporter;
        if (importer == null) return;

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        { importer.textureType = TextureImporterType.Sprite; dirty = true; }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
        if (importer.maxTextureSize < 4096)
        { importer.maxTextureSize = 4096; dirty = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
        if (importer.mipmapEnabled)
        { importer.mipmapEnabled = false; dirty = true; }

        if (dirty)
        {
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }
    }

    // ────────────────────────── DARK OVERLAY ──────────────────────────

    static void SetupDarkOverlay(Transform canvas)
    {
        var go = new GameObject("DarkOverlay");
        go.transform.SetParent(canvas, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);
        img.raycastTarget = false;
    }

    // ────────────────────────── FLOATING PARTICLES ──────────────────────────

    static void EnsureFloatingParticles(Transform canvas)
    {
        var existing = canvas.Find("FloatingParticles");
        if (existing != null)
        {
            if (existing.GetComponent<TeacherFloatingParticles>() == null)
                existing.gameObject.AddComponent<TeacherFloatingParticles>();
            return;
        }

        var go = new GameObject("FloatingParticles");
        go.transform.SetParent(canvas, false);

        var rect = go.AddComponent<RectTransform>();
        Stretch(rect);

        var cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        go.AddComponent<TeacherFloatingParticles>();
    }

    // ────────────────────────── CLASS LIST PANEL ──────────────────────────

    static void BuildClassListPanel(Transform canvas)
    {
        var panelGO = new GameObject("ClassListPanel");
        panelGO.transform.SetParent(canvas, false);

        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.12f, 0.12f);
        panelRect.anchorMax = new Vector2(0.88f, 0.88f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.02f, 0.05f, 0.65f);
        panelImg.raycastTarget = false;

        panelGO.AddComponent<CanvasGroup>();

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);

        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(0, 70);

        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "My Classes";
        titleTMP.fontSize = 52;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.overflowMode = TextOverflowModes.Overflow;
        titleTMP.enableWordWrapping = false;
        titleTMP.enableVertexGradient = true;
        titleTMP.colorGradient = new VertexGradient(
            new Color(1f, 0.97f, 0.88f),
            new Color(1f, 0.97f, 0.88f),
            new Color(0.72f, 0.60f, 0.40f),
            new Color(0.72f, 0.60f, 0.40f));
        titleTMP.raycastTarget = false;
        ApplyFont(titleTMP);

        // Scroll area for class list
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(panelGO.transform, false);

        var scrollRect = scrollGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(24, 60);
        scrollRect.offsetMax = new Vector2(-24, -80);

        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = 0.1f;
        sr.scrollSensitivity = 25f;

        var sImg = scrollGO.AddComponent<Image>();
        sImg.color = new Color(0, 0, 0, 0);
        scrollGO.AddComponent<Mask>().showMaskGraphic = false;

        // Viewport
        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpRect = vpGO.AddComponent<RectTransform>();
        Stretch(vpRect);
        var vpImg = vpGO.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        sr.viewport = vpRect;

        // ClassList container
        var ccGO = new GameObject("ClassList");
        ccGO.transform.SetParent(vpGO.transform, false);
        var ccRect = ccGO.AddComponent<RectTransform>();
        ccRect.anchorMin = new Vector2(0, 1);
        ccRect.anchorMax = new Vector2(1, 1);
        ccRect.pivot = new Vector2(0.5f, 1);
        ccRect.anchoredPosition = Vector2.zero;
        ccRect.sizeDelta = new Vector2(0, 0);

        var vlg = ccGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(16, 16, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        ccGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = ccRect;

        // Empty list text
        var emptyGO = new GameObject("EmptyListText");
        emptyGO.transform.SetParent(panelGO.transform, false);

        var emptyRect = emptyGO.AddComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
        emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
        emptyRect.anchoredPosition = Vector2.zero;
        emptyRect.sizeDelta = new Vector2(600, 100);

        var emptyTMP = emptyGO.AddComponent<TextMeshProUGUI>();
        emptyTMP.text = "No classes yet.\nCreate a class to get started!";
        emptyTMP.fontSize = 36;
        emptyTMP.fontStyle = FontStyles.Bold;
        emptyTMP.color = new Color(0.95f, 0.88f, 0.70f, 0.8f);
        emptyTMP.alignment = TextAlignmentOptions.Center;
        emptyTMP.raycastTarget = false;
        ApplyFont(emptyTMP);

        emptyGO.SetActive(false);

        // Pagination row at bottom of panel
        var pageRowGO = new GameObject("PaginationRow");
        pageRowGO.transform.SetParent(panelGO.transform, false);

        var pageRowRect = pageRowGO.AddComponent<RectTransform>();
        pageRowRect.anchorMin = new Vector2(0f, 0f);
        pageRowRect.anchorMax = new Vector2(1f, 0f);
        pageRowRect.pivot = new Vector2(0.5f, 0f);
        pageRowRect.anchoredPosition = new Vector2(0, 10);
        pageRowRect.sizeDelta = new Vector2(0, 50);

        var hlg = pageRowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 20;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        CreateStyledButton(pageRowGO.transform, "PrevPageBtn", "< Prev", 140, 40);
        CreatePageLabel(pageRowGO.transform);
        CreateStyledButton(pageRowGO.transform, "NextPageBtn", "Next >", 140, 40);
    }

    // ────────────────────────── CREATE CLASS PANEL (popup) ──────────────────────────

    static void BuildCreateClassPanel(Transform canvas)
    {
        var panelGO = new GameObject("CreateClassPanel");
        panelGO.transform.SetParent(canvas, false);
        panelGO.SetActive(false);

        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.25f);
        panelRect.anchorMax = new Vector2(0.75f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.02f, 0.05f, 0.85f);

        // Title
        var titleGO = new GameObject("CreateTitle");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(0, 60);

        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Create New Class";
        titleTMP.fontSize = 44;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.enableVertexGradient = true;
        titleTMP.colorGradient = new VertexGradient(
            new Color(1f, 0.97f, 0.88f),
            new Color(1f, 0.97f, 0.88f),
            new Color(0.72f, 0.60f, 0.40f),
            new Color(0.72f, 0.60f, 0.40f));
        titleTMP.raycastTarget = false;
        ApplyFont(titleTMP);

        // "Class Name" label above input
        var labelGO = new GameObject("ClassNameLabel");
        labelGO.transform.SetParent(panelGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0, 60);
        labelRect.sizeDelta = new Vector2(500, 35);

        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = "Class Name";
        labelTMP.fontSize = 28;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.color = new Color(0.80f, 0.75f, 0.60f, 0.9f);
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.raycastTarget = false;
        ApplyFont(labelTMP);

        // Class name input
        var inputGO = CreateInputField(panelGO.transform, "CreateClassNameInput", "Enter class name...");
        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(0, 15);
        inputRect.sizeDelta = new Vector2(500, 55);

        // Buttons row
        var btnRowGO = new GameObject("ButtonRow");
        btnRowGO.transform.SetParent(panelGO.transform, false);
        var btnRowRect = btnRowGO.AddComponent<RectTransform>();
        btnRowRect.anchorMin = new Vector2(0.5f, 0f);
        btnRowRect.anchorMax = new Vector2(0.5f, 0f);
        btnRowRect.pivot = new Vector2(0.5f, 0f);
        btnRowRect.anchoredPosition = new Vector2(0, 40);
        btnRowRect.sizeDelta = new Vector2(450, 55);

        var btnHlg = btnRowGO.AddComponent<HorizontalLayoutGroup>();
        btnHlg.childAlignment = TextAnchor.MiddleCenter;
        btnHlg.spacing = 30;
        btnHlg.childForceExpandWidth = false;
        btnHlg.childForceExpandHeight = true;
        btnHlg.childControlWidth = false;
        btnHlg.childControlHeight = true;

        CreateStyledButton(btnRowGO.transform, "CreateConfirmBtn", "Create", 180, 48);
        CreateStyledButton(btnRowGO.transform, "CreateCancelBtn", "Cancel", 180, 48);
    }

    // ────────────────────────── EDIT PANEL (popup) ──────────────────────────

    static void BuildEditPanel(Transform canvas)
    {
        var panelGO = new GameObject("EditPanel");
        panelGO.transform.SetParent(canvas, false);
        panelGO.SetActive(false);

        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.20f);
        panelRect.anchorMax = new Vector2(0.75f, 0.80f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.02f, 0.05f, 0.85f);

        // Title label
        var titleGO = new GameObject("EditTitleLabel");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(0, 60);

        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Edit Class";
        titleTMP.fontSize = 44;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.enableVertexGradient = true;
        titleTMP.colorGradient = new VertexGradient(
            new Color(1f, 0.97f, 0.88f),
            new Color(1f, 0.97f, 0.88f),
            new Color(0.72f, 0.60f, 0.40f),
            new Color(0.72f, 0.60f, 0.40f));
        titleTMP.raycastTarget = false;
        ApplyFont(titleTMP);

        // Name input
        var inputGO = CreateInputField(panelGO.transform, "EditNameInput", "New class name...");
        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(0, 20);
        inputRect.sizeDelta = new Vector2(500, 55);

        // Buttons row
        var btnRowGO = new GameObject("ButtonRow");
        btnRowGO.transform.SetParent(panelGO.transform, false);
        var btnRowRect = btnRowGO.AddComponent<RectTransform>();
        btnRowRect.anchorMin = new Vector2(0.5f, 0f);
        btnRowRect.anchorMax = new Vector2(0.5f, 0f);
        btnRowRect.pivot = new Vector2(0.5f, 0f);
        btnRowRect.anchoredPosition = new Vector2(0, 40);
        btnRowRect.sizeDelta = new Vector2(600, 55);

        var btnHlg = btnRowGO.AddComponent<HorizontalLayoutGroup>();
        btnHlg.childAlignment = TextAnchor.MiddleCenter;
        btnHlg.spacing = 20;
        btnHlg.childForceExpandWidth = false;
        btnHlg.childForceExpandHeight = true;
        btnHlg.childControlWidth = false;
        btnHlg.childControlHeight = true;

        CreateStyledButton(btnRowGO.transform, "EditConfirmBtn", "Save", 160, 48);
        CreateStyledButton(btnRowGO.transform, "DeleteConfirmBtn", "Delete", 160, 48);
        CreateStyledButton(btnRowGO.transform, "EditCancelBtn", "Cancel", 160, 48);
    }

    // ────────────────────────── CLASS LIST ITEM PREFAB ──────────────────────────

    static void EnsureClassListItemPrefab(Transform canvas)
    {
        var prefabParent = canvas.Find("Prefabs");
        if (prefabParent == null)
        {
            var prefabsGO = new GameObject("Prefabs");
            prefabsGO.transform.SetParent(canvas, false);
            prefabsGO.SetActive(false);
            prefabParent = prefabsGO.transform;
        }

        var existing = prefabParent.Find("ClassListItem");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var itemGO = new GameObject("ClassListItem");
        itemGO.transform.SetParent(prefabParent, false);

        var itemRect = itemGO.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 70);

        var le = itemGO.AddComponent<LayoutElement>();
        le.preferredHeight = 70;

        var itemImg = itemGO.AddComponent<Image>();
        itemImg.color = new Color(0.30f, 0.25f, 0.18f, 0.85f);

        var btn = itemGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = itemImg;
        itemGO.AddComponent<ButtonHoverEffect>();

        var nameGO = new GameObject("ClassName");
        nameGO.transform.SetParent(itemGO.transform, false);
        var nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(0.6f, 1f);
        nameRect.offsetMin = new Vector2(20, 5);
        nameRect.offsetMax = new Vector2(0, -5);

        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "Class Name";
        nameTMP.fontSize = 32;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        nameTMP.alignment = TextAlignmentOptions.Left;
        nameTMP.raycastTarget = false;
        ApplyFont(nameTMP);

        var codeGO = new GameObject("ClassCode");
        codeGO.transform.SetParent(itemGO.transform, false);
        var codeRect = codeGO.AddComponent<RectTransform>();
        codeRect.anchorMin = new Vector2(0.6f, 0f);
        codeRect.anchorMax = new Vector2(1f, 1f);
        codeRect.offsetMin = new Vector2(0, 5);
        codeRect.offsetMax = new Vector2(-20, -5);

        var codeTMP = codeGO.AddComponent<TextMeshProUGUI>();
        codeTMP.text = "Code: XXXXXX";
        codeTMP.fontSize = 26;
        codeTMP.fontStyle = FontStyles.Normal;
        codeTMP.color = new Color(0.75f, 0.68f, 0.55f, 0.9f);
        codeTMP.alignment = TextAlignmentOptions.Right;
        codeTMP.raycastTarget = false;
        ApplyFont(codeTMP);
    }

    // ────────────────────────── SIGN OUT (bottom-left) ──────────────────────────

    static void BuildSignOutButton(Transform canvas)
    {
        var btnGO = new GameObject("SignOutBtn");
        btnGO.transform.SetParent(canvas, false);

        var rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(20, 16);
        rect.sizeDelta = new Vector2(200, 48);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.28f, 0.22f, 0.10f, 0.75f);

        var btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btnGO.AddComponent<ButtonHoverEffect>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        Stretch(textRect);
        textRect.offsetMin = new Vector2(8, 0);
        textRect.offsetMax = new Vector2(-8, 0);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Sign Out";
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    // ────────────────────────── BOTTOM-RIGHT BUTTONS (Edit + Create) ──────────────────────────

    static void BuildBottomRightButtons(Transform canvas)
    {
        var barGO = new GameObject("BottomRightBar");
        barGO.transform.SetParent(canvas, false);

        var barRect = barGO.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0);
        barRect.anchorMax = new Vector2(0.5f, 0);
        barRect.pivot = new Vector2(0.5f, 0);
        barRect.anchoredPosition = new Vector2(0, 16);
        barRect.sizeDelta = new Vector2(620, 48);

        var hlg = barGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 12;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        CreateStyledButton(barGO.transform, "JoinBtn", "Enter Class", 190, 48);
        CreateStyledButton(barGO.transform, "EditBtn", "Edit Class", 180, 48);
        CreateStyledButton(barGO.transform, "CreateBtn", "Create Class", 210, 48);
    }

    // ────────────────────────── SETTINGS GEAR + PANEL ──────────────────────────

    static void BuildSettingsUI(Transform canvas)
    {
        // Gear button (top-right corner)
        var btnGO = new GameObject("SettingsBtn");
        btnGO.transform.SetParent(canvas, false);

        var btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 1);
        btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(1, 1);
        btnRect.anchoredPosition = new Vector2(-20, -20);
        btnRect.sizeDelta = new Vector2(52, 52);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.28f, 0.22f, 0.10f, 0.75f);

        var btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = btnImg;
        btnGO.AddComponent<ButtonHoverEffect>();

        var iconGO = new GameObject("GearIcon");
        iconGO.transform.SetParent(btnGO.transform, false);
        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.15f);
        iconRect.anchorMax = new Vector2(0.85f, 0.85f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = GetOrCreateGearSprite();
        iconImg.color = new Color(0.95f, 0.88f, 0.68f, 1f);
        iconImg.raycastTarget = false;

        // Settings panel (popup below the gear)
        var panelGO = new GameObject("SettingsPanel");
        panelGO.transform.SetParent(canvas, false);

        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -80);
        panelRect.sizeDelta = new Vector2(280, 140);

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.04f, 0.02f, 0.85f);
        panelGO.AddComponent<CanvasGroup>();

        // Music slider row
        BuildSliderRow(panelGO.transform, "MusicSlider", "Music", 10);

        // Effects slider row
        BuildSliderRow(panelGO.transform, "EffectsSlider", "Effects", 74);

        // Close button (X)
        var closeGO = new GameObject("CloseBtn");
        closeGO.transform.SetParent(panelGO.transform, false);
        var closeRect = closeGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-4, -4);
        closeRect.sizeDelta = new Vector2(26, 26);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0, 0, 0, 0);
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.targetGraphic = closeImg;
        var closeTxtGO = new GameObject("X");
        closeTxtGO.transform.SetParent(closeGO.transform, false);
        var closeTxtRect = closeTxtGO.AddComponent<RectTransform>();
        Stretch(closeTxtRect);
        var closeTMP = closeTxtGO.AddComponent<TextMeshProUGUI>();
        closeTMP.text = "X";
        closeTMP.fontSize = 18;
        closeTMP.color = new Color(0.80f, 0.70f, 0.50f, 0.8f);
        closeTMP.alignment = TextAlignmentOptions.Center;
        closeTMP.raycastTarget = false;
        ApplyFont(closeTMP);

        // Wire SettingsToggle
        var toggle = btnGO.GetComponent<SettingsToggle>();
        if (toggle == null) toggle = btnGO.AddComponent<SettingsToggle>();

        var so = new SerializedObject(toggle);
        so.FindProperty("settingsPanel").objectReferenceValue = panelGO;
        so.ApplyModifiedProperties();

        // Wire gear button onClick -> Toggle
        ClearPersistentListeners(btn.onClick);
        var toggleAction = System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction), toggle, "Toggle") as UnityEngine.Events.UnityAction;
        if (toggleAction != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, toggleAction);

        // Wire close button onClick -> Close
        ClearPersistentListeners(closeBtn.onClick);
        var closeAction = System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction), toggle, "Close") as UnityEngine.Events.UnityAction;
        if (closeAction != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.onClick, closeAction);
    }

    static void BuildSliderRow(Transform parent, string sliderName, string label, float yFromTop)
    {
        var rowGO = new GameObject(sliderName + "Row");
        rowGO.transform.SetParent(parent, false);

        var rowRect = rowGO.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.pivot = new Vector2(0.5f, 1);
        rowRect.anchoredPosition = new Vector2(0, -yFromTop);
        rowRect.sizeDelta = new Vector2(-32, 50);

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(rowGO.transform, false);
        var lblRect = lblGO.AddComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0);
        lblRect.anchorMax = new Vector2(0.35f, 1);
        lblRect.offsetMin = Vector2.zero;
        lblRect.offsetMax = Vector2.zero;
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text = label;
        lblTMP.fontSize = 20;
        lblTMP.fontStyle = FontStyles.Bold;
        lblTMP.color = new Color(0.85f, 0.78f, 0.60f, 1f);
        lblTMP.alignment = TextAlignmentOptions.Left;
        lblTMP.raycastTarget = false;
        ApplyFont(lblTMP);

        var sliderGO = new GameObject(sliderName);
        sliderGO.transform.SetParent(rowGO.transform, false);
        var sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.38f, 0.25f);
        sliderRect.anchorMax = new Vector2(1f, 0.75f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        Stretch(bgRect);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.1f);

        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        Stretch(fillAreaRect);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRect = fillGO.AddComponent<RectTransform>();
        Stretch(fillRect);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.85f, 0.68f, 0.30f, 0.8f);

        var handleAreaGO = new GameObject("Handle Slide Area");
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
        Stretch(handleAreaRect);

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16, 0);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(0.95f, 0.85f, 0.55f, 1f);

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.direction = Slider.Direction.LeftToRight;
    }

    // ────────────────────────── AUDIO MANAGER ──────────────────────────

    static void EnsureAudioManager()
    {
        var existing = GameObject.Find("AudioManager");
        AudioManager manager;

        if (existing != null)
        {
            manager = existing.GetComponent<AudioManager>();
            if (manager == null) manager = existing.AddComponent<AudioManager>();
        }
        else
        {
            var go = new GameObject("AudioManager");
            manager = go.AddComponent<AudioManager>();
        }

        var so = new SerializedObject(manager);

        string[] musicGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music" });
        if (musicGuids.Length > 0)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(musicGuids[0]);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            so.FindProperty("backgroundMusic").objectReferenceValue = clip;
        }

        string[] sfxGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" });
        foreach (string guid in sfxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (lower.Contains("hover"))
                so.FindProperty("buttonHoverClip").objectReferenceValue = clip;
            else if (lower.Contains("click"))
                so.FindProperty("buttonClickClip").objectReferenceValue = clip;
        }

        so.ApplyModifiedProperties();
    }

    // ────────────────────────── FADE OVERLAY ──────────────────────────

    static void EnsureFadeOverlay(Transform canvas)
    {
        var existing = canvas.Find("FadeOverlay");
        if (existing != null)
        {
            existing.SetAsLastSibling();
            return;
        }

        var go = new GameObject("FadeOverlay");
        go.transform.SetParent(canvas, false);
        go.transform.SetAsLastSibling();
        var rect = go.AddComponent<RectTransform>();
        Stretch(rect);
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
    }

    // ────────────────────────── WIRE TEACHER CLASS MANAGER ──────────────────────────

    static void WireTeacherClassManager(GameObject canvas)
    {
        var existing = GameObject.Find("ClassManager");
        TeacherClassManager manager;

        if (existing != null)
        {
            manager = existing.GetComponent<TeacherClassManager>();
            if (manager == null) manager = existing.AddComponent<TeacherClassManager>();
        }
        else
        {
            var go = new GameObject("ClassManager");
            go.transform.SetParent(canvas.transform, false);
            manager = go.AddComponent<TeacherClassManager>();
        }

        var so = new SerializedObject(manager);

        // Panels
        var classListPanel = canvas.transform.Find("ClassListPanel");
        if (classListPanel != null)
            so.FindProperty("classListPanel").objectReferenceValue = classListPanel.gameObject;

        var editPanel = canvas.transform.Find("EditPanel");
        if (editPanel != null)
            so.FindProperty("editPanel").objectReferenceValue = editPanel.gameObject;

        var createClassPanel = canvas.transform.Find("CreateClassPanel");
        if (createClassPanel != null)
            so.FindProperty("createClassPanel").objectReferenceValue = createClassPanel.gameObject;

        // Class list container
        var classList = FindDeep(canvas.transform, "ClassList");
        if (classList != null)
            so.FindProperty("classListContainer").objectReferenceValue = classList;

        // Class list item prefab
        var prefabs = canvas.transform.Find("Prefabs");
        if (prefabs != null)
        {
            var item = prefabs.Find("ClassListItem");
            if (item != null)
                so.FindProperty("classListItemPrefab").objectReferenceValue = item.gameObject;
        }

        // Empty list graphic
        var emptyText = FindDeep(canvas.transform, "EmptyListText");
        if (emptyText != null)
            so.FindProperty("emptyListGraphic").objectReferenceValue = emptyText.gameObject;

        // Pagination
        var prevBtn = FindDeep(canvas.transform, "PrevPageBtn");
        if (prevBtn != null)
            so.FindProperty("prevPageBtn").objectReferenceValue = prevBtn.GetComponent<Button>();

        var nextBtn = FindDeep(canvas.transform, "NextPageBtn");
        if (nextBtn != null)
            so.FindProperty("nextPageBtn").objectReferenceValue = nextBtn.GetComponent<Button>();

        var pageLabel = FindDeep(canvas.transform, "PageLabel");
        if (pageLabel != null)
            so.FindProperty("pageLabel").objectReferenceValue = pageLabel.GetComponent<TMP_Text>();

        // Create class panel fields
        var createInput = FindDeep(canvas.transform, "CreateClassNameInput");
        if (createInput != null)
            so.FindProperty("createClassNameInput").objectReferenceValue = createInput.GetComponent<TMP_InputField>();

        var createConfirmBtn = FindDeep(canvas.transform, "CreateConfirmBtn");
        if (createConfirmBtn != null)
            so.FindProperty("createConfirmBtn").objectReferenceValue = createConfirmBtn.GetComponent<Button>();

        // Edit panel fields
        var editTitleLabel = FindDeep(canvas.transform, "EditTitleLabel");
        if (editTitleLabel != null)
            so.FindProperty("editTitleLabel").objectReferenceValue = editTitleLabel.GetComponent<TMP_Text>();

        var editNameInput = FindDeep(canvas.transform, "EditNameInput");
        if (editNameInput != null)
            so.FindProperty("editNameInput").objectReferenceValue = editNameInput.GetComponent<TMP_InputField>();

        var editConfirmBtn = FindDeep(canvas.transform, "EditConfirmBtn");
        if (editConfirmBtn != null)
            so.FindProperty("editConfirmBtn").objectReferenceValue = editConfirmBtn.GetComponent<Button>();

        var deleteConfirmBtn = FindDeep(canvas.transform, "DeleteConfirmBtn");
        if (deleteConfirmBtn != null)
            so.FindProperty("deleteConfirmBtn").objectReferenceValue = deleteConfirmBtn.GetComponent<Button>();

        // List panel buttons (now in BottomRightBar)
        var joinBtn = FindDeep(canvas.transform, "JoinBtn");
        if (joinBtn != null)
            so.FindProperty("joinBtn").objectReferenceValue = joinBtn.GetComponent<Button>();

        var createBtn = FindDeep(canvas.transform, "CreateBtn");
        if (createBtn != null)
            so.FindProperty("createBtn").objectReferenceValue = createBtn.GetComponent<Button>();

        var editBtn = FindDeep(canvas.transform, "EditBtn");
        if (editBtn != null)
            so.FindProperty("editBtn").objectReferenceValue = editBtn.GetComponent<Button>();

        // Item colors — must contrast clearly against the dark panel (0.02, 0.02, 0.05, 0.65)
        var rowFillProp = so.FindProperty("rowFill");
        if (rowFillProp != null)
            rowFillProp.colorValue = new Color(0.30f, 0.25f, 0.18f, 0.85f);

        var rowHighlightProp = so.FindProperty("rowFillHighlight");
        if (rowHighlightProp != null)
            rowHighlightProp.colorValue = new Color(0.50f, 0.38f, 0.15f, 0.90f);

        so.ApplyModifiedProperties();

        // Wire SignOut button
        var signOutBtn = FindDeep(canvas.transform, "SignOutBtn");
        if (signOutBtn != null)
            WireButton(signOutBtn, manager, "SignOut");

        // Wire Cancel buttons to show list panel
        var createCancelBtn = FindDeep(canvas.transform, "CreateCancelBtn");
        if (createCancelBtn != null)
            WireButton(createCancelBtn, manager, "ShowListPanel");

        var editCancelBtn = FindDeep(canvas.transform, "EditCancelBtn");
        if (editCancelBtn != null)
            WireButton(editCancelBtn, manager, "ShowListPanel");
    }

    // ────────────────────────── WIRE BACKGROUND PARALLAX ──────────────────────────

    static void WireBackgroundParallax(GameObject canvas)
    {
        var particlesObj = canvas.transform.Find("FloatingParticles");
        if (particlesObj == null) return;

        var script = particlesObj.GetComponent<TeacherFloatingParticles>();
        if (script == null) return;

        var so = new SerializedObject(script);

        var bg = canvas.transform.Find("Background");
        if (bg != null)
            so.FindProperty("backgroundRect").objectReferenceValue = bg.GetComponent<RectTransform>();

        so.ApplyModifiedProperties();
    }

    // ────────────────────────── SIBLING ORDER ──────────────────────────

    static void FixSiblingOrder(Transform canvas)
    {
        string[] order = {
            "Background",
            "DarkOverlay",
            "FloatingParticles",
            "ClassListPanel",
            "CreateClassPanel",
            "EditPanel",
            "Prefabs",
            "ClassManager",
            "SignOutBtn",
            "BottomRightBar",
            "SettingsBtn",
            "SettingsPanel",
            "FadeOverlay"
        };

        int idx = 0;
        foreach (var name in order)
        {
            var t = canvas.Find(name);
            if (t == null) t = FindDeep(canvas, name);
            if (t != null && t.parent == canvas)
            {
                t.SetSiblingIndex(idx);
                idx++;
            }
        }
    }

    // ────────────────────────── UI FACTORY HELPERS ──────────────────────────

    static void CreateStyledButton(Transform parent, string name, string label, float width, float height)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        var rect = btnGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        var le = btnGO.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.28f, 0.22f, 0.10f, 0.75f);
        img.sprite = null;

        var btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;

        btnGO.AddComponent<ButtonHoverEffect>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        Stretch(textRect);
        textRect.offsetMin = new Vector2(8, 0);
        textRect.offsetMax = new Vector2(-8, 0);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    static void CreatePageLabel(Transform parent)
    {
        var go = new GameObject("PageLabel");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 40);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 160;
        le.preferredHeight = 40;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "Page 1 of 1";
        tmp.fontSize = 26;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = new Color(0.80f, 0.75f, 0.60f, 0.9f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    static GameObject CreateInputField(Transform parent, string name, string placeholder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 55);

        var bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.08f, 0.05f, 0.8f);

        var textAreaGO = new GameObject("Text Area");
        textAreaGO.transform.SetParent(go.transform, false);
        var textAreaRect = textAreaGO.AddComponent<RectTransform>();
        Stretch(textAreaRect);
        textAreaRect.offsetMin = new Vector2(12, 4);
        textAreaRect.offsetMax = new Vector2(-12, -4);

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textAreaGO.transform, false);
        var phRect = phGO.AddComponent<RectTransform>();
        Stretch(phRect);

        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text = placeholder;
        phTMP.fontSize = 28;
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.color = new Color(0.60f, 0.55f, 0.45f, 0.6f);
        phTMP.alignment = TextAlignmentOptions.Left;
        phTMP.raycastTarget = false;
        ApplyFont(phTMP);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textAreaGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        Stretch(textRect);

        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text = "";
        textTMP.fontSize = 28;
        textTMP.fontStyle = FontStyles.Normal;
        textTMP.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        textTMP.alignment = TextAlignmentOptions.Left;
        textTMP.raycastTarget = false;
        ApplyFont(textTMP);

        var inputField = go.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textTMP;
        inputField.placeholder = phTMP;
        inputField.fontAsset = menuFont;
        inputField.pointSize = 28;
        inputField.caretColor = new Color(0.95f, 0.85f, 0.55f, 1f);
        inputField.selectionColor = new Color(0.45f, 0.35f, 0.15f, 0.5f);

        return go;
    }

    // ────────────────────────── HELPERS ──────────────────────────

    static Transform FindDeep(Transform parent, string name)
    {
        var direct = parent.Find(name);
        if (direct != null) return direct;
        foreach (Transform child in parent)
        {
            var r = FindDeep(child, name);
            if (r != null) return r;
        }
        return null;
    }

    static void DestroyChild(Transform parent, string childName)
    {
        var t = parent.Find(childName);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static void ClearPersistentListeners(UnityEngine.Events.UnityEventBase evt)
    {
        int count = evt.GetPersistentEventCount();
        for (int i = count - 1; i >= 0; i--)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(evt, i);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void WireButton(Transform btnTransform, Component target, string methodName)
    {
        var btn = btnTransform.GetComponent<Button>();
        if (btn == null) return;

        ClearPersistentListeners(btn.onClick);

        var method = target.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null) return;

        var action = System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction), target, method) as UnityEngine.Events.UnityAction;
        if (action != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
    }

    static Sprite GetOrCreateGearSprite()
    {
        string path = "Assets/Images/Student/GearIcon.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float center = res * 0.5f;
        float outerR = res * 0.42f;
        float holeR = res * 0.14f;
        int teeth = 8;
        float toothDepth = res * 0.10f;
        float toothHalfAngle = Mathf.PI / teeth * 0.55f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                float toothAngle = Mathf.Repeat(angle + Mathf.PI, Mathf.PI * 2f / teeth) - Mathf.PI / teeth;
                float gearR = (Mathf.Abs(toothAngle) < toothHalfAngle)
                    ? outerR + toothDepth
                    : outerR;

                float edgeSoft = 1.5f;
                float alpha;

                if (dist < holeR - edgeSoft)
                    alpha = 0f;
                else if (dist < holeR + edgeSoft)
                    alpha = (dist - (holeR - edgeSoft)) / (edgeSoft * 2f);
                else if (dist < gearR - edgeSoft)
                    alpha = 1f;
                else if (dist < gearR + edgeSoft)
                    alpha = 1f - (dist - (gearR - edgeSoft)) / (edgeSoft * 2f);
                else
                    alpha = 0f;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
        }
        tex.Apply();

        string dir = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void ApplyFontToAll(Transform root)
    {
        if (menuFont == null) return;
        var allText = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in allText)
        {
            if (t.gameObject.name == "GearIcon") continue;
            t.font = menuFont;
        }
    }

    static void LoadFont()
    {
        string[] guids = AssetDatabase.FindAssets("Treamd SDF t:TMP_FontAsset", new[] { "Assets/Text" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("BlackPearl SDF t:TMP_FontAsset", new[] { "Assets/Text" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
            menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void ApplyFont(TMP_Text tmp)
    {
        if (menuFont != null) tmp.font = menuFont;
    }
}
