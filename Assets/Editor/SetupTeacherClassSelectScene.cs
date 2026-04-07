using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupTeacherClassSelectScene
{
    const string SCENE_PATH = "Assets/Scenes/TeacherPages/TeacherClassSelect.unity";
    const string BG_PATH    = "Assets/Images/Student/StudentHubBG.png";

    static TMP_FontAsset menuFont;

    [MenuItem("Tools/Setup Teacher Class Select Scene")]
    public static void Run()
    {
        LoadFont();

        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        PurgeRootObjects();

        var cameraGO = new GameObject("Main Camera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.04f, 0.02f, 1f);
        cam.orthographic = true;
        cameraGO.tag = "MainCamera";

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var ct = canvasGO.transform;

        BuildBackground(ct);
        BuildEmberOverlay(ct);
        BuildContentPanel(ct);
        BuildTitle(ct);
        BuildEmptyListText(ct);
        BuildBottomBar(ct);
        BuildPagination(ct);
        BuildCreateClassPopup(ct);
        BuildEditClassPopup(ct);
        BuildSettingsUI(ct);
        BuildFadeOverlay(ct);

        WireTeacherClassManager(canvasGO);
        WireAnimator(canvasGO);
        WireBackgroundParallax(canvasGO);
        EnsureAudioManager();
        ApplyFontToAll(ct);
        FixSiblingOrder(ct);

        AddToBuildSettings(SCENE_PATH);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Done",
            "TeacherClassSelect scene rebuilt with dark fantasy theme!\n\n" +
            "  Background + parallax + embers\n" +
            "  ContentPanel (class scroll list)\n" +
            "  BottomBar (Create · Join · Edit · Sign Out)\n" +
            "  Pagination (Prev / Next)\n" +
            "  Create & Edit popups\n" +
            "  Settings gear + panel\n" +
            "  Fade intro animation\n\n" +
            "Scene added to Build Settings.",
            "OK");
    }

    // ═══════════════════════════════════════════════
    //  BACKGROUND + EMBERS
    // ═══════════════════════════════════════════════

    static void BuildBackground(Transform canvas)
    {
        var go = new GameObject("Background");
        go.layer = 5;
        go.transform.SetParent(canvas, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-50, -50);
        rect.offsetMax = new Vector2(50, 50);

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        var sprite = LoadBgSprite();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.06f, 0.04f, 0.02f, 1f);
        }
    }

    static void BuildEmberOverlay(Transform canvas)
    {
        var go = new GameObject("EmberOverlay");
        go.transform.SetParent(canvas, false);
        Stretch(go.AddComponent<RectTransform>());
        go.AddComponent<StudentHubBackground>();
    }

    // ═══════════════════════════════════════════════
    //  CONTENT PANEL  (class list scroll)
    // ═══════════════════════════════════════════════

    static void BuildContentPanel(Transform canvas)
    {
        var panelGO = new GameObject("ContentPanel");
        panelGO.transform.SetParent(canvas, false);

        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.14f);
        panelRect.anchorMax = new Vector2(0.92f, 0.84f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.02f, 0.04f, 0.82f);
        panelImg.raycastTarget = false;

        var panelOutline = panelGO.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.5f, 0.4f, 0.2f, 0.3f);
        panelOutline.effectDistance = new Vector2(2, -2);

        panelGO.AddComponent<CanvasGroup>();

        // Scroll area (full panel, with padding for page label)
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(panelGO.transform, false);
        var scrollRect = scrollGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(12, 12);
        scrollRect.offsetMax = new Vector2(-12, -12);

        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = 0.1f;
        sr.scrollSensitivity = 25f;
        sr.inertia = true;
        sr.decelerationRate = 0.065f;

        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        scrollGO.AddComponent<Mask>().showMaskGraphic = false;

        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        Stretch(vpGO.AddComponent<RectTransform>());
        vpGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        sr.viewport = vpGO.GetComponent<RectTransform>();

        var containerGO = new GameObject("ClassListContainer");
        containerGO.transform.SetParent(vpGO.transform, false);
        var containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = Vector2.zero;

        var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 8;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        containerGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = containerRect;
    }

    // ═══════════════════════════════════════════════
    //  TITLE
    // ═══════════════════════════════════════════════

    static void BuildTitle(Transform canvas)
    {
        var go = new GameObject("title");
        go.transform.SetParent(canvas, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -15);
        rect.sizeDelta = new Vector2(700, 80);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "Your Classes!";
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
        tmp.raycastTarget = false;
        ApplyFont(tmp);

        go.AddComponent<CanvasGroup>();
    }

    static void BuildEmptyListText(Transform canvas)
    {
        var go = new GameObject("EmptyListGraphic");
        go.transform.SetParent(canvas, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(700, 140);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "No classes yet. Create one!";
        tmp.fontSize = 46;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.95f, 0.88f, 0.70f, 0.9f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
        go.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    //  BOTTOM BAR  (Create · Join · Edit · Sign Out)
    // ═══════════════════════════════════════════════

    static void BuildBottomBar(Transform canvas)
    {
        var barGO = new GameObject("BottomBar");
        barGO.transform.SetParent(canvas, false);
        var barRect = barGO.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0, 20);
        barRect.sizeDelta = new Vector2(900, 60);

        var hlg = barGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 18;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        CreateBottomButton(barGO.transform, "createBtn", "Create", 190);
        CreateBottomButton(barGO.transform, "JoinBtn", "Open", 190);
        CreateBottomButton(barGO.transform, "editBtn", "Edit", 170);
        CreateBottomButton(barGO.transform, "SignOutBtn", "Sign Out", 210);
    }

    static void BuildPagination(Transform canvas)
    {
        var barGO = new GameObject("PaginationBar");
        barGO.transform.SetParent(canvas, false);
        var barRect = barGO.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0.12f);
        barRect.anchorMax = new Vector2(0.5f, 0.12f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0, 4);
        barRect.sizeDelta = new Vector2(500, 40);

        var hlg = barGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 12;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        CreateSmallButton(barGO.transform, "PrevPageBtn", "\u2190 Prev", 120);

        var labelGO = new GameObject("PageLabel");
        labelGO.transform.SetParent(barGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(160, 36);
        labelGO.AddComponent<LayoutElement>().preferredWidth = 160;
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "Page 1 of 1";
        labelTmp.fontSize = 26;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.color = new Color(0.85f, 0.78f, 0.60f, 0.9f);
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;
        ApplyFont(labelTmp);

        CreateSmallButton(barGO.transform, "NextPageBtn", "Next \u2192", 120);
    }

    static void CreateBottomButton(Transform parent, string name, string label, float width)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        btnGO.AddComponent<RectTransform>().sizeDelta = new Vector2(width, 55);

        var le = btnGO.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 55;

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.28f, 0.22f, 0.10f, 0.75f);

        var btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btnGO.AddComponent<ButtonHoverEffect>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        Stretch(textGO.AddComponent<RectTransform>());
        textGO.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0);
        textGO.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    static void CreateSmallButton(Transform parent, string name, string label, float width)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        btnGO.AddComponent<RectTransform>().sizeDelta = new Vector2(width, 36);
        btnGO.AddComponent<LayoutElement>().preferredWidth = width;

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.18f, 0.14f, 0.08f, 0.7f);

        var btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btnGO.AddComponent<ButtonHoverEffect>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        Stretch(textGO.AddComponent<RectTransform>());

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.85f, 0.78f, 0.60f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    // ═══════════════════════════════════════════════
    //  CREATE CLASS POPUP
    // ═══════════════════════════════════════════════

    static void BuildCreateClassPopup(Transform canvas)
    {
        var go = new GameObject("CreateClassPanel");
        go.transform.SetParent(canvas, false);
        Stretch(go.AddComponent<RectTransform>());

        var dimImg = go.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.5f);
        dimImg.raycastTarget = true;

        go.SetActive(false);

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(go.transform, false);
        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.3f);
        panelRect.anchorMax = new Vector2(0.75f, 0.7f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
        panelGO.AddComponent<Outline>().effectColor = new Color(0.5f, 0.4f, 0.2f, 0.4f);
        panelGO.GetComponent<Outline>().effectDistance = new Vector2(2, -2);

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = new Vector2(-20, -10);
        var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Create a Class";
        titleTmp.fontSize = 48;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.enableVertexGradient = true;
        titleTmp.colorGradient = new VertexGradient(
            new Color(1f, 0.97f, 0.88f), new Color(1f, 0.97f, 0.88f),
            new Color(0.72f, 0.60f, 0.40f), new Color(0.72f, 0.60f, 0.40f));
        titleTmp.raycastTarget = false;
        ApplyFont(titleTmp);

        // Input field
        BuildPopupInput(panelGO.transform, "CreateClassNameInput", "Enter class name...", 0.35f, 0.55f);

        // Confirm button
        BuildPopupButton(panelGO.transform, "CreateConfirmBtn", "Create", 0.25f, 0.08f, 0.75f, 0.28f);

        // Close X
        BuildPopupCloseX(panelGO.transform, "CreateCloseBtn");
    }

    // ═══════════════════════════════════════════════
    //  EDIT CLASS POPUP
    // ═══════════════════════════════════════════════

    static void BuildEditClassPopup(Transform canvas)
    {
        var go = new GameObject("EditPanel");
        go.transform.SetParent(canvas, false);
        Stretch(go.AddComponent<RectTransform>());

        var dimImg = go.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.5f);
        dimImg.raycastTarget = true;

        go.SetActive(false);

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(go.transform, false);
        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.25f);
        panelRect.anchorMax = new Vector2(0.75f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        panelGO.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
        panelGO.AddComponent<Outline>().effectColor = new Color(0.5f, 0.4f, 0.2f, 0.4f);
        panelGO.GetComponent<Outline>().effectDistance = new Vector2(2, -2);

        // Title
        var titleGO = new GameObject("EditTitleLabel");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = new Vector2(-20, -10);
        var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Edit Class";
        titleTmp.fontSize = 44;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.95f, 0.90f, 0.78f, 1f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.enableVertexGradient = true;
        titleTmp.colorGradient = new VertexGradient(
            new Color(1f, 0.97f, 0.88f), new Color(1f, 0.97f, 0.88f),
            new Color(0.72f, 0.60f, 0.40f), new Color(0.72f, 0.60f, 0.40f));
        titleTmp.raycastTarget = false;
        ApplyFont(titleTmp);

        // Input
        BuildPopupInput(panelGO.transform, "EditNameInput", "New class name...", 0.42f, 0.62f);

        // Rename button
        BuildPopupButton(panelGO.transform, "EditConfirmBtn", "Rename", 0.08f, 0.08f, 0.48f, 0.30f);

        // Delete button (red-tinted)
        BuildPopupButton(panelGO.transform, "DeleteConfirmBtn", "Delete", 0.52f, 0.08f, 0.92f, 0.30f,
            new Color(0.35f, 0.08f, 0.06f, 0.85f), new Color(1f, 0.70f, 0.65f, 1f));

        // Close X
        BuildPopupCloseX(panelGO.transform, "EditCloseBtn");
    }

    // ═══════════════════════════════════════════════
    //  POPUP HELPERS (input, button, close)
    // ═══════════════════════════════════════════════

    static void BuildPopupInput(Transform parent, string name, string placeholder,
        float yMin, float yMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, yMin);
        rect.anchorMax = new Vector2(0.9f, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.06f, 0.04f, 0.9f);

        go.AddComponent<Outline>().effectColor = new Color(0.5f, 0.4f, 0.2f, 0.3f);
        go.GetComponent<Outline>().effectDistance = new Vector2(1, -1);

        // Text area
        var textAreaGO = new GameObject("Text Area");
        textAreaGO.transform.SetParent(go.transform, false);
        var taRect = textAreaGO.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(14, 6);
        taRect.offsetMax = new Vector2(-14, -6);

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textAreaGO.transform, false);
        Stretch(phGO.AddComponent<RectTransform>());
        var phTmp = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 30;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.6f, 0.55f, 0.45f, 0.7f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false;
        ApplyFont(phTmp);

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textAreaGO.transform, false);
        Stretch(textGO.AddComponent<RectTransform>());
        var textTmp = textGO.AddComponent<TextMeshProUGUI>();
        textTmp.text = "";
        textTmp.fontSize = 32;
        textTmp.fontStyle = FontStyles.Bold;
        textTmp.color = new Color(0.95f, 0.92f, 0.85f, 1f);
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;
        textTmp.raycastTarget = false;
        ApplyFont(textTmp);

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRect;
        input.textComponent = textTmp;
        input.placeholder = phTmp;
        input.fontAsset = menuFont;
        input.pointSize = 32;
        input.targetGraphic = bgImg;
    }

    static void BuildPopupButton(Transform parent, string name, string label,
        float xMin, float yMin, float xMax, float yMax,
        Color? bgColor = null, Color? textColor = null)
    {
        Color bg = bgColor ?? new Color(0.28f, 0.22f, 0.10f, 0.85f);
        Color txt = textColor ?? new Color(0.95f, 0.90f, 0.78f, 1f);

        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = btnGO.AddComponent<Image>();
        img.color = bg;

        var btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btnGO.AddComponent<ButtonHoverEffect>();

        btnGO.AddComponent<Outline>().effectColor = new Color(0.5f, 0.4f, 0.2f, 0.3f);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        Stretch(textGO.AddComponent<RectTransform>());
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = txt;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    static void BuildPopupCloseX(Transform parent, string name)
    {
        var closeGO = new GameObject(name);
        closeGO.transform.SetParent(parent, false);
        var rect = closeGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-8, -8);
        rect.sizeDelta = new Vector2(40, 40);

        closeGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var btn = closeGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = closeGO.GetComponent<Image>();

        var xGO = new GameObject("X");
        xGO.transform.SetParent(closeGO.transform, false);
        Stretch(xGO.AddComponent<RectTransform>());
        var tmp = xGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "X";
        tmp.fontSize = 28;
        tmp.color = new Color(0.80f, 0.70f, 0.50f, 0.8f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    // ═══════════════════════════════════════════════
    //  SETTINGS GEAR + PANEL
    // ═══════════════════════════════════════════════

    static void BuildSettingsUI(Transform canvas)
    {
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
        iconImg.sprite = LoadGearSprite();
        iconImg.color = new Color(0.95f, 0.88f, 0.68f, 1f);
        iconImg.raycastTarget = false;

        var panelGO = new GameObject("SettingsPanel");
        panelGO.transform.SetParent(canvas, false);
        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 80);
        panelRect.sizeDelta = new Vector2(280, 100);
        panelGO.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.02f, 0.85f);
        panelGO.AddComponent<CanvasGroup>();

        BuildSliderRow(panelGO.transform, "MusicSlider", "Music", 10);

        var closeGO = new GameObject("CloseBtn");
        closeGO.transform.SetParent(panelGO.transform, false);
        var closeRect = closeGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-4, -4);
        closeRect.sizeDelta = new Vector2(26, 26);
        closeGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.targetGraphic = closeGO.GetComponent<Image>();
        var xGO = new GameObject("X");
        xGO.transform.SetParent(closeGO.transform, false);
        Stretch(xGO.AddComponent<RectTransform>());
        var xTmp = xGO.AddComponent<TextMeshProUGUI>();
        xTmp.text = "X";
        xTmp.fontSize = 18;
        xTmp.color = new Color(0.80f, 0.70f, 0.50f, 0.8f);
        xTmp.alignment = TextAlignmentOptions.Center;
        xTmp.raycastTarget = false;
        ApplyFont(xTmp);

        var toggle = btnGO.AddComponent<SettingsToggle>();
        var so = new SerializedObject(toggle);
        so.FindProperty("settingsPanel").objectReferenceValue = panelGO;
        so.ApplyModifiedProperties();

        ClearPersistentListeners(btn.onClick);
        var toggleAction = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), toggle, "Toggle") as UnityEngine.Events.UnityAction;
        if (toggleAction != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, toggleAction);

        ClearPersistentListeners(closeBtn.onClick);
        var closeAction = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), toggle, "Close") as UnityEngine.Events.UnityAction;
        if (closeAction != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.onClick, closeAction);
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
        Stretch(bgGO.AddComponent<RectTransform>());
        bgGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);

        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        Stretch(fillAreaGO.AddComponent<RectTransform>());

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        Stretch(fillGO.AddComponent<RectTransform>());
        fillGO.AddComponent<Image>().color = new Color(0.85f, 0.68f, 0.30f, 0.8f);

        var handleAreaGO = new GameObject("Handle Slide Area");
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        Stretch(handleAreaGO.AddComponent<RectTransform>());

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        handleGO.AddComponent<RectTransform>().sizeDelta = new Vector2(16, 0);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(0.95f, 0.85f, 0.55f, 1f);

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillGO.GetComponent<RectTransform>();
        slider.handleRect = handleGO.GetComponent<RectTransform>();
        slider.targetGraphic = handleImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
    }

    static void BuildFadeOverlay(Transform canvas)
    {
        var go = new GameObject("FadeOverlay");
        go.transform.SetParent(canvas, false);
        go.transform.SetAsLastSibling();
        Stretch(go.AddComponent<RectTransform>());
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
    }

    // ═══════════════════════════════════════════════
    //  WIRE: TeacherClassManager (list mode)
    // ═══════════════════════════════════════════════

    static void WireTeacherClassManager(GameObject canvasGO)
    {
        var mgr = canvasGO.AddComponent<TeacherClassManager>();
        var so = new SerializedObject(mgr);
        var t = canvasGO.transform;

        // List mode refs
        var containerT = FindDeep(t, "ClassListContainer");
        if (containerT != null)
            so.FindProperty("classListContainer").objectReferenceValue = containerT;

        var emptyT = FindDeep(t, "EmptyListGraphic");
        if (emptyT != null)
            so.FindProperty("emptyListGraphic").objectReferenceValue = emptyT.gameObject;

        // Pagination
        var prevT = FindDeep(t, "PrevPageBtn");
        if (prevT != null)
            so.FindProperty("prevPageBtn").objectReferenceValue = prevT.GetComponent<Button>();

        var nextT = FindDeep(t, "NextPageBtn");
        if (nextT != null)
            so.FindProperty("nextPageBtn").objectReferenceValue = nextT.GetComponent<Button>();

        var pageLabelT = FindDeep(t, "PageLabel");
        if (pageLabelT != null)
            so.FindProperty("pageLabel").objectReferenceValue = pageLabelT.GetComponent<TMP_Text>();

        // Bottom bar buttons
        var joinT = FindDeep(t, "JoinBtn");
        if (joinT != null)
            so.FindProperty("joinBtn").objectReferenceValue = joinT.GetComponent<Button>();

        var createT = FindDeep(t, "createBtn");
        if (createT != null)
            so.FindProperty("createBtn").objectReferenceValue = createT.GetComponent<Button>();

        var editT = FindDeep(t, "editBtn");
        if (editT != null)
            so.FindProperty("editBtn").objectReferenceValue = editT.GetComponent<Button>();

        // Create class popup
        var createPanelT = t.Find("CreateClassPanel");
        if (createPanelT != null)
        {
            so.FindProperty("createClassPanel").objectReferenceValue = createPanelT.gameObject;

            var inputT = FindDeep(createPanelT, "CreateClassNameInput");
            if (inputT != null)
                so.FindProperty("createClassNameInput").objectReferenceValue = inputT.GetComponent<TMP_InputField>();

            var confirmT = FindDeep(createPanelT, "CreateConfirmBtn");
            if (confirmT != null)
                so.FindProperty("createConfirmBtn").objectReferenceValue = confirmT.GetComponent<Button>();
        }

        // Edit popup
        var editPanelT = t.Find("EditPanel");
        if (editPanelT != null)
        {
            so.FindProperty("editPanel").objectReferenceValue = editPanelT.gameObject;

            var editTitleT = FindDeep(editPanelT, "EditTitleLabel");
            if (editTitleT != null)
                so.FindProperty("editTitleLabel").objectReferenceValue = editTitleT.GetComponent<TMP_Text>();

            var editInputT = FindDeep(editPanelT, "EditNameInput");
            if (editInputT != null)
                so.FindProperty("editNameInput").objectReferenceValue = editInputT.GetComponent<TMP_InputField>();

            var editConfirmT = FindDeep(editPanelT, "EditConfirmBtn");
            if (editConfirmT != null)
                so.FindProperty("editConfirmBtn").objectReferenceValue = editConfirmT.GetComponent<Button>();

            var deleteT = FindDeep(editPanelT, "DeleteConfirmBtn");
            if (deleteT != null)
                so.FindProperty("deleteConfirmBtn").objectReferenceValue = deleteT.GetComponent<Button>();
        }

        so.ApplyModifiedProperties();

        // Wire Sign Out button
        var signOutT = FindDeep(t, "SignOutBtn");
        if (signOutT != null)
        {
            var btn = signOutT.GetComponent<Button>();
            if (btn != null)
            {
                ClearPersistentListeners(btn.onClick);
                var action = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), mgr, "SignOut") as UnityEngine.Events.UnityAction;
                if (action != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
            }
        }

        // Wire popup close buttons
        WireCloseButton(t, "CreateCloseBtn", mgr);
        WireCloseButton(t, "EditCloseBtn", mgr);
    }

    static void WireCloseButton(Transform root, string name, TeacherClassManager mgr)
    {
        var closeT = FindDeep(root, name);
        if (closeT == null) return;
        var btn = closeT.GetComponent<Button>();
        if (btn == null) return;

        ClearPersistentListeners(btn.onClick);
        var action = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), mgr, "HidePopups") as UnityEngine.Events.UnityAction;
        if (action != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
    }

    // ═══════════════════════════════════════════════
    //  WIRE: StudentHubAnimator
    // ═══════════════════════════════════════════════

    static void WireAnimator(GameObject canvasGO)
    {
        var animator = canvasGO.AddComponent<StudentHubAnimator>();
        var so = new SerializedObject(animator);
        var t = canvasGO.transform;

        var fadeT = FindDeep(t, "FadeOverlay");
        if (fadeT != null)
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeT.GetComponent<Image>();

        var titleT = FindDeep(t, "title");
        if (titleT != null)
        {
            var cg = titleT.GetComponent<CanvasGroup>() ?? titleT.gameObject.AddComponent<CanvasGroup>();
            so.FindProperty("titleGroup").objectReferenceValue = cg;
        }

        var panelT = t.Find("ContentPanel");
        if (panelT != null)
            so.FindProperty("parchmentRect").objectReferenceValue = panelT.GetComponent<RectTransform>();

        var btnNames = new[] { "createBtn", "JoinBtn", "editBtn", "SignOutBtn" };
        var found = new List<RectTransform>();
        foreach (var n in btnNames)
        {
            var bt = FindDeep(t, n);
            if (bt != null) found.Add(bt.GetComponent<RectTransform>());
        }
        var btnsProp = so.FindProperty("buttons");
        btnsProp.arraySize = found.Count;
        for (int i = 0; i < found.Count; i++)
            btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = found[i];

        so.ApplyModifiedProperties();
    }

    static void WireBackgroundParallax(GameObject canvasGO)
    {
        var emberOverlay = canvasGO.transform.Find("EmberOverlay");
        if (emberOverlay == null) return;
        var bgScript = emberOverlay.GetComponent<StudentHubBackground>();
        if (bgScript == null) return;

        var so = new SerializedObject(bgScript);
        var bg = canvasGO.transform.Find("Background");
        if (bg != null)
            so.FindProperty("backgroundRect").objectReferenceValue = bg.GetComponent<RectTransform>();
        so.ApplyModifiedProperties();
    }

    static void EnsureAudioManager()
    {
        if (GameObject.Find("AudioManager") != null) return;
        var go = new GameObject("AudioManager");
        var manager = go.AddComponent<AudioManager>();
        var so = new SerializedObject(manager);

        string[] musicGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music" });
        if (musicGuids.Length > 0)
            so.FindProperty("backgroundMusic").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(musicGuids[0]));

        string[] sfxGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" });
        foreach (string guid in sfxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (lower.Contains("hover")) so.FindProperty("buttonHoverClip").objectReferenceValue = clip;
            else if (lower.Contains("click")) so.FindProperty("buttonClickClip").objectReferenceValue = clip;
        }
        so.ApplyModifiedProperties();
    }

    // ═══════════════════════════════════════════════
    //  SIBLING ORDER
    // ═══════════════════════════════════════════════

    static void FixSiblingOrder(Transform canvas)
    {
        string[] order = {
            "Background", "EmberOverlay", "ContentPanel", "title",
            "EmptyListGraphic", "BottomBar", "PaginationBar",
            "CreateClassPanel", "EditPanel",
            "SettingsBtn", "SettingsPanel", "FadeOverlay"
        };
        int idx = 0;
        foreach (var name in order)
        {
            var child = canvas.Find(name) ?? FindDeep(canvas, name);
            if (child != null && child.parent == canvas) { child.SetSiblingIndex(idx); idx++; }
        }
    }

    // ═══════════════════════════════════════════════
    //  BUILD SETTINGS + HELPERS
    // ═══════════════════════════════════════════════

    static void AddToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes) if (s.path == scenePath) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void PurgeRootObjects()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Transform FindDeep(Transform parent, string name)
    {
        var d = parent.Find(name);
        if (d != null) return d;
        foreach (Transform child in parent)
        {
            var r = FindDeep(child, name);
            if (r != null) return r;
        }
        return null;
    }

    static Sprite LoadBgSprite()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BG_PATH);
        if (sprite != null) return sprite;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(BG_PATH))
            if (a is Sprite s) return s;
        return null;
    }

    static Sprite LoadGearSprite()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Student/GearIcon.png");
    }

    static void ClearPersistentListeners(UnityEngine.Events.UnityEventBase evt)
    {
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(evt, i);
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

    static void ApplyFontToAll(Transform root)
    {
        if (menuFont == null) return;
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.gameObject.name == "GearIcon") continue;
            t.font = menuFont;
        }
    }
}
