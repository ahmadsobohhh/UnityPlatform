using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public static class SetupWelcomePage
{
    [MenuItem("Tools/Rework Welcome Page UI")]
    public static void Run()
    {
        string scenePath = "Assets/Scenes/UniversalPages/WelcomePage.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvas not found in WelcomePage.", "OK");
            return;
        }

        FixCanvasScaler(canvas);
        SetupVideoBackground(canvas.transform);
        SetupDarkOverlay(canvas.transform);
        SetupTitle(canvas.transform);
        SetupMainMenu(canvas.transform);
        SetupOptionsMenu(canvas.transform);
        SetupFadeOverlay(canvas.transform);
        SetupMenuAnimator(canvas);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Done", "WelcomePage UI has been reworked!", "OK");
    }

    static void FixCanvasScaler(GameObject canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    static void SetupVideoBackground(Transform canvas)
    {
        var bgTransform = canvas.Find("Background");

        var existingVideo = canvas.Find("VideoBackground");
        if (existingVideo != null)
            Object.DestroyImmediate(existingVideo.gameObject);

        var videoGO = new GameObject("VideoBackground");
        videoGO.transform.SetParent(canvas, false);
        videoGO.transform.SetAsFirstSibling();

        var rect = videoGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        videoGO.AddComponent<RawImage>();
        var player = videoGO.AddComponent<VideoPlayer>();
        videoGO.AddComponent<VideoBackground>();

        string[] guids = AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Video" });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            player.clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
            Debug.Log($"[UI Setup] Video clip assigned: {path}");
            videoGO.SetActive(true);
            if (bgTransform != null) bgTransform.gameObject.SetActive(false);
        }
        else
        {
            videoGO.SetActive(false);
        }
    }

    static void SetupDarkOverlay(Transform canvas)
    {
        var existing = canvas.Find("DarkOverlay");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject("DarkOverlay");
        go.transform.SetParent(canvas, false);

        var videoT = canvas.Find("VideoBackground");
        var bgT = canvas.Find("Background");
        int insertIdx = 1;
        if (videoT != null) insertIdx = videoT.GetSiblingIndex() + 1;
        else if (bgT != null) insertIdx = bgT.GetSiblingIndex() + 1;
        go.transform.SetSiblingIndex(insertIdx);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0.45f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f);
        img.raycastTarget = false;

        // Gradient child that feathers the right edge
        var featherGO = new GameObject("GradientEdge");
        featherGO.transform.SetParent(go.transform, false);

        var featherRect = featherGO.AddComponent<RectTransform>();
        featherRect.anchorMin = new Vector2(1f, 0f);
        featherRect.anchorMax = new Vector2(1f, 1f);
        featherRect.pivot = new Vector2(0f, 0.5f);
        featherRect.anchoredPosition = Vector2.zero;
        featherRect.sizeDelta = new Vector2(200f, 0f);

        var featherImg = featherGO.AddComponent<Image>();
        featherImg.color = Color.white;
        featherImg.raycastTarget = false;

        // Create a gradient texture (black to transparent)
        var gradTex = new Texture2D(256, 1, TextureFormat.RGBA32, false);
        for (int x = 0; x < 256; x++)
        {
            float a = Mathf.Lerp(0.7f, 0f, (float)x / 255f);
            gradTex.SetPixel(x, 0, new Color(0, 0, 0, a));
        }
        gradTex.Apply();
        gradTex.wrapMode = TextureWrapMode.Clamp;

        string gradPath = "Assets/Editor/DarkGradient.png";
        var pngBytes = gradTex.EncodeToPNG();
        System.IO.File.WriteAllBytes(gradPath, pngBytes);
        Object.DestroyImmediate(gradTex);
        AssetDatabase.ImportAsset(gradPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(gradPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var gradSprite = AssetDatabase.LoadAssetAtPath<Sprite>(gradPath);
        featherImg.sprite = gradSprite;
        featherImg.type = Image.Type.Simple;
    }

    static void SetupFadeOverlay(Transform canvas)
    {
        var existing = canvas.Find("FadeOverlay");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var fadeGO = new GameObject("FadeOverlay");
        fadeGO.transform.SetParent(canvas, false);
        fadeGO.transform.SetAsLastSibling();

        var rect = fadeGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = fadeGO.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
    }

    static void SetupTitle(Transform canvas)
    {
        var titleTransform = canvas.Find("GameTitle");
        if (titleTransform == null) return;

        titleTransform.gameObject.SetActive(true);

        var rect = titleTransform.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(60f, -40f);
        rect.sizeDelta = new Vector2(500f, 200f);

        var tmp = titleTransform.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.fontSize = 72f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.9f, 0.85f, 0.75f, 1f);
            tmp.enableVertexGradient = true;
            tmp.colorGradient = new VertexGradient(
                new Color(1f, 0.95f, 0.85f),
                new Color(1f, 0.95f, 0.85f),
                new Color(0.75f, 0.65f, 0.5f),
                new Color(0.75f, 0.65f, 0.5f)
            );
        }

        var group = titleTransform.GetComponent<CanvasGroup>();
        if (group == null) group = titleTransform.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
    }

    static void SetupMainMenu(Transform canvas)
    {
        var menuTransform = canvas.Find("MainMenu");
        if (menuTransform == null) return;

        menuTransform.gameObject.SetActive(true);

        // Remove VerticalLayoutGroup if previously added (we use manual positioning)
        var vlg = menuTransform.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) Object.DestroyImmediate(vlg);

        var rect = menuTransform.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(60f, 0f);
        rect.sizeDelta = new Vector2(350f, 0f);

        string[] buttonNames = { "BeginBtn", "OptionsBtn", "ExitBtn" };
        float spacing = 65f;

        for (int i = 0; i < buttonNames.Length; i++)
        {
            var btnT = menuTransform.Find(buttonNames[i]);
            if (btnT == null) continue;

            var btnRect = btnT.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0f, 0.5f);
            btnRect.anchorMax = new Vector2(1f, 0.5f);
            btnRect.pivot = new Vector2(0f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0f, -i * spacing);
            btnRect.sizeDelta = new Vector2(0f, 55f);

            StyleButton(btnT);
        }
    }

    static void SetupOptionsMenu(Transform canvas)
    {
        var menuTransform = canvas.Find("OptionsMenu");
        if (menuTransform == null) return;

        var rect = menuTransform.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(60f, 0f);
        rect.sizeDelta = new Vector2(450f, 0f);

        // Reposition children for the left-aligned layout
        float y = 60f;

        var musicTxt = menuTransform.Find("MusicTxt");
        if (musicTxt != null)
        {
            var r = musicTxt.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(10f, y);
            r.sizeDelta = new Vector2(120f, 50f);
            var t = musicTxt.GetComponent<TMP_Text>();
            if (t != null) { t.fontSize = 36f; t.alignment = TextAlignmentOptions.Left; }
        }

        var musicSlider = menuTransform.Find("MusicSlider");
        if (musicSlider != null)
        {
            var r = musicSlider.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(140f, y);
            r.sizeDelta = new Vector2(260f, 30f);
        }

        y -= 80f;

        var effectsTxt = menuTransform.Find("EffectsTxt");
        if (effectsTxt != null)
        {
            var r = effectsTxt.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(10f, y);
            r.sizeDelta = new Vector2(120f, 50f);
            var t = effectsTxt.GetComponent<TMP_Text>();
            if (t != null) { t.fontSize = 36f; t.alignment = TextAlignmentOptions.Left; }
        }

        var effectsSlider = menuTransform.Find("EffectsSlider");
        if (effectsSlider != null)
        {
            var r = effectsSlider.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(140f, y);
            r.sizeDelta = new Vector2(260f, 30f);
        }

        y -= 90f;

        var backBtn = menuTransform.Find("BackBtn");
        if (backBtn != null)
        {
            var r = backBtn.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(10f, y);
            r.sizeDelta = new Vector2(200f, 55f);
            StyleButton(backBtn);
        }
    }

    static void StyleButton(Transform btnT)
    {
        var img = btnT.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0, 0, 0, 0);
            img.sprite = null;
        }

        var btn = btnT.GetComponent<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
        }

        if (btnT.GetComponent<ButtonHoverEffect>() == null)
            btnT.gameObject.AddComponent<ButtonHoverEffect>();

        var label = btnT.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.fontSize = 42f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.85f, 0.82f, 0.75f, 1f);
            label.alignment = TextAlignmentOptions.Left;

            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = Vector2.zero;
        }
    }

    static void SetupMenuAnimator(GameObject canvas)
    {
        var animator = canvas.GetComponent<MenuAnimator>();
        if (animator == null) animator = canvas.AddComponent<MenuAnimator>();

        var so = new SerializedObject(animator);

        var fadeProp = so.FindProperty("fadeOverlay");
        var fadeT = canvas.transform.Find("FadeOverlay");
        if (fadeT != null) fadeProp.objectReferenceValue = fadeT.GetComponent<Image>();

        var titleProp = so.FindProperty("titleGroup");
        var titleT = canvas.transform.Find("GameTitle");
        if (titleT != null) titleProp.objectReferenceValue = titleT.GetComponent<CanvasGroup>();

        var btnsProp = so.FindProperty("menuButtons");
        var menuT = canvas.transform.Find("MainMenu");
        if (menuT != null)
        {
            string[] names = { "BeginBtn", "OptionsBtn", "ExitBtn" };
            btnsProp.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var child = menuT.Find(names[i]);
                if (child != null)
                    btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = child.GetComponent<RectTransform>();
            }
        }

        so.ApplyModifiedProperties();
    }
}
