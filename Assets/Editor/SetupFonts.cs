using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class SetupFonts
{
    [MenuItem("Tools/Apply Fonts to Welcome Page")]
    public static void Run()
    {
        string scenePath = "Assets/Scenes/UniversalPages/WelcomePage.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvas not found.", "OK");
            return;
        }

        string fontDir = "Assets/Text/WelcomePage";

        TMP_FontAsset titleFont = FindOrCreateSDF(fontDir, "BlackPearl");
        TMP_FontAsset menuFont = FindOrCreateSDF(fontDir, "Treamd");
        if (menuFont == null) menuFont = FindOrCreateSDF(fontDir, "BlackPearl");

        if (titleFont != null)
        {
            ApplyFont(canvas.transform, "GameTitle", titleFont, 80f);
            Debug.Log($"[Fonts] Title font: BlackPearl");
        }

        if (menuFont != null)
        {
            ApplyButtonFont(canvas.transform, "MainMenu/BeginBtn", menuFont, 42f);
            ApplyButtonFont(canvas.transform, "MainMenu/OptionsBtn", menuFont, 42f);
            ApplyButtonFont(canvas.transform, "MainMenu/ExitBtn", menuFont, 42f);
            ApplyButtonFont(canvas.transform, "OptionsMenu/BackBtn", menuFont, 42f);
            ApplyFont(canvas.transform, "OptionsMenu/MusicTxt", menuFont, 36f);
            ApplyFont(canvas.transform, "OptionsMenu/EffectsTxt", menuFont, 36f);
            Debug.Log($"[Fonts] Menu font: {menuFont.name}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Done", "Fonts applied to WelcomePage!", "OK");
    }

    static TMP_FontAsset FindOrCreateSDF(string dir, string fontName)
    {
        // Look for existing SDF asset
        string[] guids = AssetDatabase.FindAssets($"{fontName} SDF t:TMP_FontAsset", new[] { dir });
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));

        // Also try any TMP_FontAsset with a similar name
        guids = AssetDatabase.FindAssets($"t:TMP_FontAsset", new[] { dir });
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (path.ToLower().Contains(fontName.ToLower().Replace("-variablefont_wght", "")))
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        Debug.LogWarning($"[Fonts] SDF asset not found for {fontName}. Please create it manually: right-click the .ttf > Create > TextMeshPro > Font Asset");
        return null;
    }

    static void ApplyFont(Transform root, string path, TMP_FontAsset font, float size)
    {
        var t = root.Find(path);
        if (t == null) return;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp == null) return;
        tmp.font = font;
        tmp.fontSize = size;
    }

    static void ApplyButtonFont(Transform root, string path, TMP_FontAsset font, float size)
    {
        var t = root.Find(path);
        if (t == null) return;
        var tmp = t.GetComponentInChildren<TMP_Text>();
        if (tmp == null) return;
        tmp.font = font;
        tmp.fontSize = size;
    }
}
