using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupStudentHub
{
    const string GUI_PATH = "Assets/Images/Fantasy Wooden GUI  Free/normal_ui_set A";
    const string BG_PATH = "Assets/Images/Student/StudentHubBG.png";

    static TMP_FontAsset menuFont;

    [MenuItem("Tools/Rework Student Hub UI")]
    public static void Run()
    {
        LoadFont();
        string scenePath = "Assets/Scenes/StudentPages/StudentHub.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvas not found in StudentHub.", "OK");
            return;
        }

        CleanupOldObjects(canvas.transform);
        FixCanvasScaler(canvas);
        FixBackgroundOrder(canvas.transform);
        EnsureEmberOverlay(canvas.transform);
        BuildContentPanel(canvas.transform);
        EnsureTitle(canvas.transform);
        EnsureNoClassesText(canvas.transform);
        BuildBottomBar(canvas.transform);
        BuildSettingsUI(canvas.transform);
        EnsureFadeOverlay(canvas.transform);
        AddButtonEffects(canvas.transform);
        WireAnimator(canvas);
        WireClassLoader(canvas);
        WireBackgroundParallax(canvas);
        EnsureAudioManager();
        ApplyFontToAll(canvas.transform);
        FixSiblingOrder(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Done",
            "StudentHub UI rebuilt!\n\nHierarchy:\n" +
            "  Background (parallax)\n" +
            "  EmberOverlay\n" +
            "  ContentPanel (dark transparent)\n" +
            "    Scroll > Viewport > ClassContainer\n" +
            "  title\n" +
            "  NoClassesText\n" +
            "  BottomBar\n" +
            "  FadeOverlay", "OK");
    }

    // ────────────────────────── CLEANUP ──────────────────────────

    static void CleanupOldObjects(Transform canvas)
    {
        string[] stale = {
            "AnimatedBackground", "DarkOverlay", "Header",
            "NavBar", "ClassScrollArea", "NoClassesMessage",
            "VideoBackground", "Subtitle",
            "JoinClassBtn", "ProfileBtn", "SignOutBtn",
            "VignetteOverlay", "ClassParchment", "AddClassBtn",
            "ContentPanel", "SettingsBtn", "SettingsPanel"
        };
        foreach (var n in stale)
            DestroyChild(canvas, n);

        // Remove old standalone SignOutManager / LoadJoinClassScene root objects
        var som = GameObject.Find("SignOutManager");
        if (som != null && som.transform.parent == null)
            Object.DestroyImmediate(som);
        var ljs = GameObject.Find("LoadJoinClassScene");
        if (ljs != null && ljs.transform.parent == null)
            Object.DestroyImmediate(ljs);

        // Remove orphan StudentClassLoader root object
        var scl = GameObject.Find("StudentClassLoader");
        if (scl != null && scl.transform.parent != canvas.transform && scl.transform.parent == null)
            Object.DestroyImmediate(scl);
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

    static void FixBackgroundOrder(Transform canvas)
    {
        // Find the Background that is a direct child of this canvas (Layer 5 / UI)
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

        // Try loading as single sprite first, then try sub-sprites
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
            Debug.Log($"[SetupStudentHub] Background sprite assigned: {sprite.name} ({sprite.texture.width}x{sprite.texture.height})");
        }
        else
        {
            Debug.LogError($"[SetupStudentHub] Could not load any sprite from {BG_PATH}");
        }
    }

    static void ImportBackgroundSprite()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(BG_PATH) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[SetupStudentHub] Background not found at {BG_PATH}");
            return;
        }

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

    // ────────────────────────── EMBER OVERLAY ──────────────────────────

    static void EnsureEmberOverlay(Transform canvas)
    {
        var existing = canvas.Find("EmberOverlay");
        if (existing != null)
        {
            if (existing.GetComponent<StudentHubBackground>() == null)
                existing.gameObject.AddComponent<StudentHubBackground>();
            return;
        }

        var go = new GameObject("EmberOverlay");
        go.transform.SetParent(canvas, false);
        var rect = go.AddComponent<RectTransform>();
        Stretch(rect);
        go.AddComponent<StudentHubBackground>();
    }

    // ────────────────────── CONTENT PANEL (dark transparent, like welcome page) ──────────────────────

    static void BuildContentPanel(Transform canvas)
    {
        var panelGO = new GameObject("ContentPanel");
        panelGO.transform.SetParent(canvas, false);

        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.10f, 0.12f);
        panelRect.anchorMax = new Vector2(0.90f, 0.86f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.02f, 0.05f, 0.6f);
        panelImg.raycastTarget = false;

        panelGO.AddComponent<CanvasGroup>();

        // Scroll area inside the dark panel
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(panelGO.transform, false);

        var scrollRectT = scrollGO.AddComponent<RectTransform>();
        scrollRectT.anchorMin = Vector2.zero;
        scrollRectT.anchorMax = Vector2.one;
        scrollRectT.offsetMin = new Vector2(24, 16);
        scrollRectT.offsetMax = new Vector2(-24, -16);

        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = 0.1f;
        sr.scrollSensitivity = 25f;

        var sImg = scrollGO.AddComponent<Image>();
        sImg.color = new Color(0, 0, 0, 0);
        scrollGO.AddComponent<Mask>().showMaskGraphic = false;

        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpRect = vpGO.AddComponent<RectTransform>();
        Stretch(vpRect);
        var vpImg = vpGO.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        sr.viewport = vpRect;

        var ccGO = new GameObject("ClassContainer");
        ccGO.transform.SetParent(vpGO.transform, false);
        var ccRect = ccGO.AddComponent<RectTransform>();
        ccRect.anchorMin = new Vector2(0, 1);
        ccRect.anchorMax = new Vector2(1, 1);
        ccRect.pivot = new Vector2(0.5f, 1);
        ccRect.anchoredPosition = Vector2.zero;
        ccRect.sizeDelta = new Vector2(0, 0);

        var vlg = ccGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 8;
        vlg.padding = new RectOffset(24, 24, 16, 16);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        ccGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = ccRect;
    }

    // ────────────────────────── TITLE ──────────────────────────

    static void EnsureTitle(Transform canvas)
    {
        var existing = FindDeep(canvas, "title");
        if (existing != null)
        {
            if (existing.GetComponent<CanvasGroup>() == null)
                existing.gameObject.AddComponent<CanvasGroup>();

            var tmp = existing.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = "My Classes";
                tmp.fontSize = 56;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.95f, 0.90f, 0.78f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableVertexGradient = true;
                tmp.colorGradient = new VertexGradient(
                    new Color(1f, 0.97f, 0.88f),
                    new Color(1f, 0.97f, 0.88f),
                    new Color(0.72f, 0.60f, 0.40f),
                    new Color(0.72f, 0.60f, 0.40f));
                ApplyFont(tmp);
            }
            return;
        }

        var titleGO = new GameObject("title");
        titleGO.transform.SetParent(canvas, false);

        var rect = titleGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -15);
        rect.sizeDelta = new Vector2(600, 80);

        var tmp2 = titleGO.AddComponent<TextMeshProUGUI>();
        tmp2.text = "My Classes";
        tmp2.fontSize = 56;
        tmp2.fontStyle = FontStyles.Bold;
        tmp2.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        tmp2.alignment = TextAlignmentOptions.Center;
        tmp2.enableVertexGradient = true;
        tmp2.colorGradient = new VertexGradient(
            new Color(1f, 0.97f, 0.88f),
            new Color(1f, 0.97f, 0.88f),
            new Color(0.72f, 0.60f, 0.40f),
            new Color(0.72f, 0.60f, 0.40f));
        tmp2.raycastTarget = false;
        ApplyFont(tmp2);

        titleGO.AddComponent<CanvasGroup>();
    }

    // ────────────────────────── NO CLASSES TEXT ──────────────────────────

    static void EnsureNoClassesText(Transform canvas)
    {
        var existing = canvas.Find("NoClassesText");
        if (existing != null)
        {
            var tmp = existing.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = "No classes yet.\nJoin a class to get started!";
                tmp.fontSize = 42;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.95f, 0.88f, 0.70f, 0.9f);
                tmp.alignment = TextAlignmentOptions.Center;
                ApplyFont(tmp);
            }

            var ncRect = existing.GetComponent<RectTransform>();
            ncRect.anchorMin = new Vector2(0.5f, 0.5f);
            ncRect.anchorMax = new Vector2(0.5f, 0.5f);
            ncRect.anchoredPosition = new Vector2(0, 0);
            ncRect.sizeDelta = new Vector2(700, 140);
            ncRect.localScale = Vector3.one;
            return;
        }

        var go = new GameObject("NoClassesText");
        go.transform.SetParent(canvas, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(700, 140);

        var tmpNew = go.AddComponent<TextMeshProUGUI>();
        tmpNew.text = "No classes yet.\nJoin a class to get started!";
        tmpNew.fontSize = 42;
        tmpNew.fontStyle = FontStyles.Bold;
        tmpNew.color = new Color(0.95f, 0.88f, 0.70f, 0.9f);
        tmpNew.alignment = TextAlignmentOptions.Center;
        tmpNew.raycastTarget = false;
        ApplyFont(tmpNew);

        go.SetActive(false);
    }

    // ────────────────────────── BOTTOM BUTTONS ──────────────────────────
    // Matches the welcome page: text-only, transparent bg, hover scale + color

    static void BuildBottomBar(Transform canvas)
    {
        DestroyChild(canvas, "BottomBar");
        DestroyChild(canvas, "SignoutBtn");

        var barGO = new GameObject("BottomBar");
        barGO.transform.SetParent(canvas, false);

        var barRect = barGO.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0, 20);
        barRect.sizeDelta = new Vector2(750, 60);

        var hlg = barGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        CreateWelcomeStyleButton(barGO.transform, "JoinClassBtn", "Join Class", 220);
        CreateDivider(barGO.transform);
        CreateWelcomeStyleButton(barGO.transform, "ProfileBtn", "Profile", 180);
        CreateDivider(barGO.transform);
        CreateWelcomeStyleButton(barGO.transform, "SignOutBtn", "Sign Out", 200);
    }

    static void CreateWelcomeStyleButton(Transform parent, string name, string label, float width)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        var rect = btnGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 55);

        var le = btnGO.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 55;

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
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 38;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    static void CreateDivider(Transform parent)
    {
        var divGO = new GameObject("Divider");
        divGO.transform.SetParent(parent, false);

        var rect = divGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2, 30);

        var le = divGO.AddComponent<LayoutElement>();
        le.preferredWidth = 2;
        le.preferredHeight = 30;

        var img = divGO.AddComponent<Image>();
        img.color = new Color(0.55f, 0.45f, 0.22f, 0.5f);
        img.raycastTarget = false;
    }

    // ────────────────────────── SETTINGS GEAR + PANEL ──────────────────────────

    static void BuildSettingsUI(Transform canvas)
    {
        // --- Gear button (bottom-right) ---
        var btnGO = new GameObject("SettingsBtn");
        btnGO.transform.SetParent(canvas, false);

        var btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 0);
        btnRect.anchorMax = new Vector2(1, 0);
        btnRect.pivot = new Vector2(1, 0);
        btnRect.anchoredPosition = new Vector2(-20, 20);
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

        // --- Settings panel (popup above the gear) ---
        var panelGO = new GameObject("SettingsPanel");
        panelGO.transform.SetParent(canvas, false);

        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 80);
        panelRect.sizeDelta = new Vector2(280, 100);

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.04f, 0.02f, 0.85f);
        panelGO.AddComponent<CanvasGroup>();

        // Music label + slider row
        BuildSliderRow(panelGO.transform, "MusicSlider", "Music", 10);

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

        // Label
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

        // Slider
        var sliderGO = new GameObject(sliderName);
        sliderGO.transform.SetParent(rowGO.transform, false);
        var sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.38f, 0.25f);
        sliderRect.anchorMax = new Vector2(1f, 0.75f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Background track
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        Stretch(bgRect);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.1f);

        // Fill area
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

        // Handle
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

        // Slider component
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
            Debug.Log($"[SetupStudentHub] Music clip wired: {clipPath}");
        }
        else
        {
            Debug.LogWarning("[SetupStudentHub] No music clips found in Assets/Audio/Music");
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
        if (existing == null)
        {
            existing = FindDeep(canvas, "FadeOverlay");
        }
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

    // ────────────────────────── BUTTON EFFECTS ──────────────────────────

    static void AddButtonEffects(Transform canvas)
    {
        string[] names = { "joinBtn", "JoinClassBtn", "ProfileBtn", "SignOutBtn", "SettingsBtn" };
        foreach (var n in names)
        {
            var t = FindDeep(canvas, n);
            if (t == null) continue;
            if (t.GetComponent<ButtonHoverEffect>() == null)
                t.gameObject.AddComponent<ButtonHoverEffect>();
        }
    }

    // ────────────────────────── WIRE BACKGROUND PARALLAX ──────────────────────────

    static void WireBackgroundParallax(GameObject canvas)
    {
        var emberOverlay = canvas.transform.Find("EmberOverlay");
        if (emberOverlay == null) return;

        var bgScript = emberOverlay.GetComponent<StudentHubBackground>();
        if (bgScript == null) return;

        var so = new SerializedObject(bgScript);
        var bg = canvas.transform.Find("Background");
        if (bg != null)
        {
            var prop = so.FindProperty("backgroundRect");
            prop.objectReferenceValue = bg.GetComponent<RectTransform>();
        }
        so.ApplyModifiedProperties();
    }

    // ────────────────────────── WIRE ANIMATOR ──────────────────────────

    static void WireAnimator(GameObject canvas)
    {
        var animator = canvas.GetComponent<StudentHubAnimator>();
        if (animator == null) animator = canvas.AddComponent<StudentHubAnimator>();

        var so = new SerializedObject(animator);

        // Fade overlay
        var fadeT = FindDeep(canvas.transform, "FadeOverlay");
        if (fadeT != null)
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeT.GetComponent<Image>();

        // Title
        var titleT = FindDeep(canvas.transform, "title");
        if (titleT != null)
        {
            var cg = titleT.GetComponent<CanvasGroup>();
            if (cg == null) cg = titleT.gameObject.AddComponent<CanvasGroup>();
            so.FindProperty("titleGroup").objectReferenceValue = cg;
        }

        // Content panel (replaces old parchment)
        var panelT = canvas.transform.Find("ContentPanel");
        if (panelT != null)
            so.FindProperty("parchmentRect").objectReferenceValue = panelT.GetComponent<RectTransform>();

        // Buttons to animate
        var btnNames = new[] { "JoinClassBtn", "ProfileBtn", "SignOutBtn" };
        var found = new System.Collections.Generic.List<RectTransform>();
        foreach (var n in btnNames)
        {
            var t = FindDeep(canvas.transform, n);
            if (t != null) found.Add(t.GetComponent<RectTransform>());
        }
        var btnsProp = so.FindProperty("buttons");
        btnsProp.arraySize = found.Count;
        for (int i = 0; i < found.Count; i++)
            btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = found[i];

        // Class container
        var cc = FindClassContainer(canvas.transform);
        if (cc != null)
            so.FindProperty("classContainer").objectReferenceValue = cc;

        so.ApplyModifiedProperties();
    }

    // ────────────────────────── WIRE CLASS LOADER ──────────────────────────

    static void WireClassLoader(GameObject canvas)
    {
        var loader = canvas.GetComponent<StudentClassLoader>();
        if (loader == null) loader = canvas.AddComponent<StudentClassLoader>();

        var cc = FindClassContainer(canvas.transform);
        if (cc != null)
            loader.classContainer = cc;

        loader.cardFont = menuFont;

        var noClasses = canvas.transform.Find("NoClassesText");
        if (noClasses != null)
            loader.noClassesText = noClasses.gameObject;

        // Ensure LoadJoinClassScene on canvas
        var ljs = canvas.GetComponent<LoadJoinClassScene>();
        if (ljs == null) ljs = canvas.AddComponent<LoadJoinClassScene>();

        // Ensure SignOutManager on canvas
        var som = canvas.GetComponent<SignOutManager>();
        if (som == null) som = canvas.AddComponent<SignOutManager>();

        // Ensure LoadProfileScene on canvas
        var lps = canvas.GetComponent<LoadProfileScene>();
        if (lps == null) lps = canvas.AddComponent<LoadProfileScene>();

        // Wire JoinClassBtn
        var joinClassBtn = FindDeep(canvas.transform, "JoinClassBtn");
        if (joinClassBtn != null)
            WireButton(joinClassBtn, ljs, "GoToJoinClass");

        // Wire SignOutBtn
        var signOutBtn = FindDeep(canvas.transform, "SignOutBtn");
        if (signOutBtn != null)
            WireButton(signOutBtn, som, "SignOut");

        // Wire ProfileBtn
        var profileBtn = FindDeep(canvas.transform, "ProfileBtn");
        if (profileBtn != null)
            WireButton(profileBtn, lps, "GoToProfile");
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

    // ────────────────────────── SIBLING ORDER ──────────────────────────

    static void FixSiblingOrder(Transform canvas)
    {
        string[] order = {
            "Background",
            "EmberOverlay",
            "ContentPanel",
            "title",
            "NoClassesText",
            "BottomBar",
            "SettingsBtn",
            "SettingsPanel",
            "JoinGUI",
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

    // ────────────────────────── HELPERS ──────────────────────────

    static Sprite LoadSprite(string fileName)
    {
        string[] guids = AssetDatabase.FindAssets(fileName + " t:Sprite", new[] { GUI_PATH });
        if (guids.Length == 0)
            guids = AssetDatabase.FindAssets(fileName + " t:Texture2D", new[] { GUI_PATH });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains(fileName)) continue;

            var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in sprites)
            {
                if (asset is Sprite s) return s;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        Debug.LogWarning($"[SetupStudentHub] Sprite not found: {fileName}");
        return null;
    }

    static Transform FindClassContainer(Transform canvas)
    {
        var panel = canvas.Find("ContentPanel");
        if (panel == null) return null;
        var scroll = panel.Find("Scroll");
        if (scroll == null) return null;
        var vp = scroll.Find("Viewport");
        if (vp == null) return null;
        return vp.Find("ClassContainer");
    }

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

    static Sprite GetOrCreateGearSprite()
    {
        string path = "Assets/Images/Student/GearIcon.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float center = res * 0.5f;
        float outerR = res * 0.42f;
        float innerR = res * 0.26f;
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
                float gearR = innerR;
                if (Mathf.Abs(toothAngle) < toothHalfAngle)
                    gearR = outerR + toothDepth;
                else
                    gearR = outerR;

                float edgeSoft = 1.5f;
                float alpha = 0f;

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

                alpha = Mathf.Clamp01(alpha);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
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
