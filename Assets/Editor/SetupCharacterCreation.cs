using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupCharacterCreation
{
    static readonly Color BgColor = new Color(0.03f, 0.03f, 0.06f, 1f);
    static readonly Color CardBg = new Color(0.06f, 0.06f, 0.1f, 0.9f);
    static readonly Color LabelColor = new Color(0.85f, 0.82f, 0.75f, 1f);
    static readonly Color DimColor = new Color(0.5f, 0.48f, 0.42f, 1f);
    static readonly Color BtnColor = new Color(0.65f, 0.55f, 0.35f, 1f);
    static readonly Color BtnTextColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    static readonly Color ArrowColor = new Color(0.85f, 0.8f, 0.65f, 0.8f);

    static TMP_FontAsset menuFont;

    static readonly string[] CharIds = { "hero", "mage", "knight", "bandit", "monk", "archer", "samurai", "scholar" };
    static readonly string[] CharNames = { "Hero", "Mage", "Knight", "Bandit", "Monk", "Archer", "Samurai", "Scholar" };
    static readonly string[] CharDescs =
    {
        "A courageous warrior with an unwavering sense of justice. Born to lead and protect.",
        "A brilliant spellcaster who bends the arcane forces of the world to their will.",
        "An armored guardian sworn to defend the realm against any threat.",
        "A cunning rogue who thrives in the shadows, striking when least expected.",
        "A disciplined martial artist who channels inner energy into powerful techniques.",
        "A keen-eyed marksman whose arrows never miss their target.",
        "An honorable warrior who follows the ancient code of bushido.",
        "A brilliant strategist who uses knowledge and wit as their greatest weapons."
    };

    [MenuItem("Tools/Setup Character Creation")]
    public static void Run()
    {
        string scenePath = "Assets/Scenes/StudentPages/StudentAvatarSelect.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        LoadFont();

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.AddComponent<GraphicRaycaster>();
        }
        else
        {
            FixCanvasScaler(canvas);
        }

        ClearOldUI(canvas.transform);

        BuildBackground(canvas.transform);
        var creationUI = BuildCharacterCreationUI(canvas.transform);
        WireCharacterCreationScript(canvas, creationUI);
        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Done", "Character Creation scene is ready!", "OK");
    }

    static void LoadFont()
    {
        string[] guids = AssetDatabase.FindAssets("Treamd SDF t:TMP_FontAsset", new[] { "Assets/Text" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("BlackPearl SDF t:TMP_FontAsset", new[] { "Assets/Text" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
            menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void FixCanvasScaler(GameObject canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    static void ClearOldUI(Transform canvas)
    {
        for (int i = canvas.childCount - 1; i >= 0; i--)
        {
            var child = canvas.GetChild(i);
            if (child.name == "EventSystem") continue;
            Object.DestroyImmediate(child.gameObject);
        }
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    static void BuildBackground(Transform canvas)
    {
        var bg = new GameObject("Background");
        bg.transform.SetParent(canvas, false);
        var rect = bg.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = bg.AddComponent<Image>();
        img.color = BgColor;
        img.raycastTarget = false;

        var vignette = new GameObject("Vignette");
        vignette.transform.SetParent(canvas, false);
        var vRect = vignette.AddComponent<RectTransform>();
        vRect.anchorMin = Vector2.zero;
        vRect.anchorMax = Vector2.one;
        vRect.offsetMin = Vector2.zero;
        vRect.offsetMax = Vector2.zero;
        var vImg = vignette.AddComponent<Image>();
        vImg.color = new Color(0, 0, 0, 0.3f);
        vImg.raycastTarget = false;
    }

    static GameObject BuildCharacterCreationUI(Transform canvas)
    {
        var root = new GameObject("CharacterCreationUI");
        root.transform.SetParent(canvas, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.AddComponent<CanvasGroup>();

        // Title
        MakeText(root.transform, "Title", "Choose Your Character", 52f, new Vector2(0, 440f), new Vector2(800, 70));

        // Character preview
        var preview = new GameObject("CharacterPreview");
        preview.transform.SetParent(root.transform, false);
        var pRect = preview.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.5f, 0.5f);
        pRect.anchorMax = new Vector2(0.5f, 0.5f);
        pRect.anchoredPosition = new Vector2(0, 80f);
        pRect.sizeDelta = new Vector2(350, 350);
        var pImg = preview.AddComponent<Image>();
        pImg.color = Color.white;
        pImg.preserveAspect = true;
        pImg.raycastTarget = false;

        // Preview card background (behind the character)
        var previewBg = new GameObject("PreviewBg");
        previewBg.transform.SetParent(root.transform, false);
        previewBg.transform.SetSiblingIndex(preview.transform.GetSiblingIndex());
        var pbRect = previewBg.AddComponent<RectTransform>();
        pbRect.anchorMin = new Vector2(0.5f, 0.5f);
        pbRect.anchorMax = new Vector2(0.5f, 0.5f);
        pbRect.anchoredPosition = new Vector2(0, 80f);
        pbRect.sizeDelta = new Vector2(400, 400);
        var pbImg = previewBg.AddComponent<Image>();
        pbImg.color = CardBg;
        pbImg.raycastTarget = false;

        // Character name
        MakeText(root.transform, "CharName", "Hero", 44f, new Vector2(0, -140f), new Vector2(600, 60));

        // Description
        var descGO = MakeText(root.transform, "CharDesc",
            "A courageous warrior with an unwavering sense of justice.",
            22f, new Vector2(0, -195f), new Vector2(600, 50));
        descGO.GetComponent<TMP_Text>().color = DimColor;

        // Left arrow
        var leftBtn = MakeArrowButton(root.transform, "LeftArrow", "<", new Vector2(-280f, 80f));

        // Right arrow
        var rightBtn = MakeArrowButton(root.transform, "RightArrow", ">", new Vector2(280f, 80f));

        // Color palette section
        MakeText(root.transform, "ColorLabel", "Choose Your Style", 26f, new Vector2(0, -260f), new Vector2(400, 40));
        var palette = BuildColorPalette(root.transform, new Vector2(0, -310f));

        // Selection ring (highlights current color)
        var ring = new GameObject("SelectionRing");
        ring.transform.SetParent(palette.transform, false);
        var ringRect = ring.AddComponent<RectTransform>();
        ringRect.sizeDelta = new Vector2(56, 56);
        var ringImg = ring.AddComponent<Image>();
        ringImg.color = new Color(1f, 0.9f, 0.6f, 0.9f);
        ringImg.raycastTarget = false;
        ring.transform.SetAsFirstSibling();

        // Confirm button
        var confirmBtn = MakeButton(root.transform, "ConfirmBtn", "Create Character",
            new Vector2(0, -400f), new Vector2(380, 60));
        confirmBtn.AddComponent<ButtonPulseEffect>();

        return root;
    }

    static GameObject BuildColorPalette(Transform parent, Vector2 position)
    {
        var container = new GameObject("ColorPalette");
        container.transform.SetParent(parent, false);
        var cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0.5f);
        cRect.anchorMax = new Vector2(0.5f, 0.5f);
        cRect.anchoredPosition = position;
        cRect.sizeDelta = new Vector2(360, 50);

        float spacing = 55f;
        float startX = -spacing * 2.5f;

        for (int i = 0; i < CharacterCreation.SwatchDisplayColors.Length; i++)
        {
            var swatch = new GameObject("Color" + i);
            swatch.transform.SetParent(container.transform, false);
            var sRect = swatch.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(startX + i * spacing, 0);
            sRect.sizeDelta = new Vector2(44, 44);

            var sImg = swatch.AddComponent<Image>();
            sImg.color = CharacterCreation.SwatchDisplayColors[i];

            swatch.AddComponent<Button>().transition = Selectable.Transition.None;
        }

        return container;
    }

    static void WireCharacterCreationScript(GameObject canvas, GameObject creationUI)
    {
        var existing = canvas.GetComponent<CharacterCreation>();
        if (existing != null) Object.DestroyImmediate(existing);

        var cc = canvas.AddComponent<CharacterCreation>();
        var so = new SerializedObject(cc);

        // Wire UI references
        so.FindProperty("characterPreview").objectReferenceValue =
            creationUI.transform.Find("CharacterPreview")?.GetComponent<Image>();
        so.FindProperty("nameText").objectReferenceValue =
            creationUI.transform.Find("CharName")?.GetComponent<TMP_Text>();
        so.FindProperty("descriptionText").objectReferenceValue =
            creationUI.transform.Find("CharDesc")?.GetComponent<TMP_Text>();
        so.FindProperty("leftArrow").objectReferenceValue =
            creationUI.transform.Find("LeftArrow")?.GetComponent<Button>();
        so.FindProperty("rightArrow").objectReferenceValue =
            creationUI.transform.Find("RightArrow")?.GetComponent<Button>();
        so.FindProperty("confirmButton").objectReferenceValue =
            creationUI.transform.Find("ConfirmBtn")?.GetComponent<Button>();
        so.FindProperty("uiGroup").objectReferenceValue =
            creationUI.GetComponent<CanvasGroup>();

        // Wire color swatches
        var palette = creationUI.transform.Find("ColorPalette");
        if (palette != null)
        {
            var swatchProp = so.FindProperty("colorSwatches");
            int count = 0;
            for (int i = 0; i < 6; i++)
            {
                var sw = palette.Find("Color" + i);
                if (sw != null) count++;
            }
            swatchProp.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                var sw = palette.Find("Color" + i);
                swatchProp.GetArrayElementAtIndex(i).objectReferenceValue = sw?.GetComponent<Image>();
            }

            so.FindProperty("selectionRing").objectReferenceValue =
                palette.Find("SelectionRing")?.GetComponent<Image>();
        }

        // Wire character data
        var charsProp = so.FindProperty("characters");
        charsProp.arraySize = CharIds.Length;

        string[] spritePaths =
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

        for (int i = 0; i < CharIds.Length; i++)
        {
            var elem = charsProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("id").stringValue = CharIds[i];
            elem.FindPropertyRelative("displayName").stringValue = CharNames[i];
            elem.FindPropertyRelative("description").stringValue = CharDescs[i];

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[i]);
            if (sprite != null)
                elem.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            else
                Debug.LogWarning($"[CharacterCreation] Sprite not found: {spritePaths[i]}");
        }

        so.ApplyModifiedProperties();
    }

    // ─── UI Helpers ──────────────────────────────────────────

    static GameObject MakeText(Transform parent, string name, string text, float size, Vector2 pos, Vector2 sizeDelta)
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
        tmp.color = LabelColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) tmp.font = menuFont;

        return go;
    }

    static GameObject MakeArrowButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(70, 70);

        go.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        go.AddComponent<ButtonHoverEffect>();

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var tRect = txtGO.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 52f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = ArrowColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) tmp.font = menuFont;

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
        tmp.fontSize = 32f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = BtnTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) tmp.font = menuFont;

        return go;
    }
}
