using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupStudentProfile
{
    static readonly Color BgColor = new Color(0.03f, 0.03f, 0.06f, 1f);
    static readonly Color CardBg = new Color(0.06f, 0.06f, 0.1f, 0.92f);
    static readonly Color LabelColor = new Color(0.85f, 0.82f, 0.75f, 1f);
    static readonly Color DimColor = new Color(0.5f, 0.48f, 0.42f, 1f);
    static readonly Color AccentColor = new Color(0.65f, 0.55f, 0.35f, 1f);
    static readonly Color BtnColor = new Color(0.65f, 0.55f, 0.35f, 1f);
    static readonly Color BtnTextColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    static readonly Color XpBarBg = new Color(0.12f, 0.12f, 0.15f, 1f);
    static readonly Color XpBarFill = new Color(0.65f, 0.55f, 0.35f, 1f);

    static TMP_FontAsset menuFont;

    static readonly string[] AvatarIds = { "hero", "mage", "knight", "bandit", "monk", "archer", "samurai", "scholar" };
    static readonly string[] SpritePaths =
    {
        "Assets/Images/Student/Avatar/heroavatar1.png",
        "Assets/Images/Student/Avatar/mageavatar1.png",
        "Assets/Images/Student/Avatar/knightavatar1.png",
        "Assets/Images/Student/Avatar/banditavatar1.png",
        "Assets/Images/Student/Avatar/monkavatar1.png",
        "Assets/Images/Student/Avatar/archeravatar1.png",
        "Assets/Images/Student/Avatar/samuraiavatar1.png",
        "Assets/Images/Student/Avatar/scholaravatar1.png",
    };

    [MenuItem("Tools/Setup Student Profile Scene")]
    public static void Run()
    {
        LoadFont();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGO = new GameObject("Main Camera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        cam.orthographic = true;
        cameraGO.tag = "MainCamera";

        var canvas = new GameObject("Canvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.AddComponent<GraphicRaycaster>();

        // Background
        var bg = MakeImage(canvas.transform, "Background", Vector2.zero, Vector2.zero, true);
        bg.GetComponent<Image>().color = BgColor;
        bg.GetComponent<Image>().raycastTarget = false;

        // Main card
        var card = new GameObject("ProfileCard");
        card.transform.SetParent(canvas.transform, false);
        var cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(700, 750);
        card.AddComponent<Image>().color = CardBg;
        card.AddComponent<CanvasGroup>();

        float y = 310f;

        // Title
        MakeText(card.transform, "TitleTxt", "Your Profile", 48f, new Vector2(0, y), new Vector2(600, 65));
        y -= 90f;

        // Avatar frame
        var avatarFrame = new GameObject("AvatarFrame");
        avatarFrame.transform.SetParent(card.transform, false);
        var afRect = avatarFrame.AddComponent<RectTransform>();
        afRect.anchorMin = new Vector2(0.5f, 0.5f);
        afRect.anchorMax = new Vector2(0.5f, 0.5f);
        afRect.anchoredPosition = new Vector2(0, y);
        afRect.sizeDelta = new Vector2(200, 200);
        avatarFrame.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 1f);

        var avatarImg = new GameObject("AvatarImage");
        avatarImg.transform.SetParent(avatarFrame.transform, false);
        var aiRect = avatarImg.AddComponent<RectTransform>();
        aiRect.anchorMin = new Vector2(0.1f, 0.1f);
        aiRect.anchorMax = new Vector2(0.9f, 0.9f);
        aiRect.offsetMin = Vector2.zero;
        aiRect.offsetMax = Vector2.zero;
        var aiImg = avatarImg.AddComponent<Image>();
        aiImg.color = Color.white;
        aiImg.preserveAspect = true;
        aiImg.raycastTarget = false;
        y -= 120f;

        // Class name badge
        MakeText(card.transform, "ClassName", "Hero", 30f, new Vector2(0, y), new Vector2(300, 40), AccentColor);
        y -= 50f;

        // Username
        MakeText(card.transform, "Username", "Loading...", 38f, new Vector2(0, y), new Vector2(500, 50));
        y -= 45f;

        // Member since
        MakeText(card.transform, "MemberSince", "Member since Jan 2026", 20f, new Vector2(0, y), new Vector2(400, 30), DimColor);
        y -= 60f;

        // Stats row
        var statsRow = new GameObject("StatsRow");
        statsRow.transform.SetParent(card.transform, false);
        var srRect = statsRow.AddComponent<RectTransform>();
        srRect.anchorMin = new Vector2(0.5f, 0.5f);
        srRect.anchorMax = new Vector2(0.5f, 0.5f);
        srRect.anchoredPosition = new Vector2(0, y);
        srRect.sizeDelta = new Vector2(600, 100);

        MakeStatBox(statsRow.transform, "LevelStat", "LEVEL", "1", -200f);
        MakeStatBox(statsRow.transform, "XpStat", "TOTAL XP", "0", 0f);
        MakeStatBox(statsRow.transform, "ClassesStat", "CLASSES", "0", 200f);

        y -= 80f;

        // XP Bar
        MakeText(card.transform, "XpLabel", "Experience", 20f, new Vector2(0, y), new Vector2(400, 28), DimColor);
        y -= 25f;

        var xpBarBg = new GameObject("XpBarBg");
        xpBarBg.transform.SetParent(card.transform, false);
        var xbRect = xpBarBg.AddComponent<RectTransform>();
        xbRect.anchorMin = new Vector2(0.5f, 0.5f);
        xbRect.anchorMax = new Vector2(0.5f, 0.5f);
        xbRect.anchoredPosition = new Vector2(0, y);
        xbRect.sizeDelta = new Vector2(500, 20);
        xpBarBg.AddComponent<Image>().color = XpBarBg;

        var xpFill = new GameObject("XpBarFill");
        xpFill.transform.SetParent(xpBarBg.transform, false);
        var xfRect = xpFill.AddComponent<RectTransform>();
        xfRect.anchorMin = Vector2.zero;
        xfRect.anchorMax = new Vector2(0.5f, 1f);
        xfRect.offsetMin = Vector2.zero;
        xfRect.offsetMax = Vector2.zero;
        var xfImg = xpFill.AddComponent<Image>();
        xfImg.color = XpBarFill;
        xfImg.type = Image.Type.Filled;
        xfImg.fillMethod = Image.FillMethod.Horizontal;
        xfImg.fillAmount = 1f;
        y -= 55f;

        // Buttons
        var backBtn = MakeButton(card.transform, "BackBtn", "< Back", new Vector2(-150, y), new Vector2(200, 50));
        var signOutBtn = MakeButton(card.transform, "SignOutBtn", "Sign Out", new Vector2(150, y), new Vector2(200, 50));
        signOutBtn.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f, 1f);

        // Wire StudentProfilePage
        WireProfileScript(canvas, card, avatarImg, xpFill, statsRow);

        // EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        string savePath = "Assets/Scenes/StudentPages/StudentProfile.unity";
        EditorSceneManager.SaveScene(scene, savePath);

        AddToBuildSettings(savePath);

        EditorUtility.DisplayDialog("Done", "Student Profile scene created!\nPath: " + savePath, "OK");
    }

    static void WireProfileScript(GameObject canvas, GameObject card, GameObject avatarImg, GameObject xpFill, GameObject statsRow)
    {
        var page = canvas.AddComponent<StudentProfilePage>();
        var so = new SerializedObject(page);

        so.FindProperty("avatarImage").objectReferenceValue = avatarImg.GetComponent<Image>();
        so.FindProperty("usernameText").objectReferenceValue =
            card.transform.Find("Username")?.GetComponent<TMP_Text>();
        so.FindProperty("classNameText").objectReferenceValue =
            card.transform.Find("ClassName")?.GetComponent<TMP_Text>();
        so.FindProperty("memberSinceText").objectReferenceValue =
            card.transform.Find("MemberSince")?.GetComponent<TMP_Text>();

        so.FindProperty("xpBarFill").objectReferenceValue = xpFill.GetComponent<Image>();
        so.FindProperty("uiGroup").objectReferenceValue = card.GetComponent<CanvasGroup>();
        so.FindProperty("backButton").objectReferenceValue =
            card.transform.Find("BackBtn")?.GetComponent<Button>();
        so.FindProperty("signOutButton").objectReferenceValue =
            card.transform.Find("SignOutBtn")?.GetComponent<Button>();

        // Wire stats
        var levelStat = statsRow.transform.Find("LevelStat");
        if (levelStat != null)
            so.FindProperty("levelText").objectReferenceValue =
                levelStat.Find("Value")?.GetComponent<TMP_Text>();

        var xpStat = statsRow.transform.Find("XpStat");
        if (xpStat != null)
            so.FindProperty("totalXpText").objectReferenceValue =
                xpStat.Find("Value")?.GetComponent<TMP_Text>();

        var classesStat = statsRow.transform.Find("ClassesStat");
        if (classesStat != null)
            so.FindProperty("classesJoinedText").objectReferenceValue =
                classesStat.Find("Value")?.GetComponent<TMP_Text>();

        // Wire avatar sprites
        var spritesProp = so.FindProperty("avatarSprites");
        var idsProp = so.FindProperty("avatarIds");
        spritesProp.arraySize = AvatarIds.Length;
        idsProp.arraySize = AvatarIds.Length;

        for (int i = 0; i < AvatarIds.Length; i++)
        {
            idsProp.GetArrayElementAtIndex(i).stringValue = AvatarIds[i];
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePaths[i]);
            spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
        }

        so.ApplyModifiedProperties();
    }

    static void AddToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[SetupStudentProfile] Added StudentProfile to Build Settings.");
    }

    // ─── UI Helpers ──────────────────────────────────────────

    static void LoadFont()
    {
        string[] guids = AssetDatabase.FindAssets("Treamd SDF t:TMP_FontAsset", new[] { "Assets/Text" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("BlackPearl SDF t:TMP_FontAsset", new[] { "Assets/Text" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
            menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static GameObject MakeText(Transform parent, string name, string text, float size, Vector2 pos, Vector2 sizeDelta, Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = sizeDelta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color ?? LabelColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) tmp.font = menuFont;

        return go;
    }

    static void MakeStatBox(Transform parent, string name, string label, string value, float x)
    {
        var box = new GameObject(name);
        box.transform.SetParent(parent, false);
        var rect = box.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0);
        rect.sizeDelta = new Vector2(160, 90);
        box.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.8f);

        MakeText(box.transform, "Value", value, 36f, new Vector2(0, 8f), new Vector2(140, 45), AccentColor);
        MakeText(box.transform, "Label", label, 16f, new Vector2(0, -28f), new Vector2(140, 25), DimColor);
    }

    static GameObject MakeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, bool fullScreen)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        if (fullScreen)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        go.AddComponent<Image>();
        return go;
    }

    static GameObject MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        go.AddComponent<Image>().color = BtnColor;
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        go.AddComponent<ButtonHoverEffect>();

        var txtGO = new GameObject("Text (TMP)");
        txtGO.transform.SetParent(go.transform, false);
        var tRect = txtGO.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = BtnTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) tmp.font = menuFont;

        return go;
    }
}
