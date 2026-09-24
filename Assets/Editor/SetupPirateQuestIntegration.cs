using System.Collections.Generic;
using ImagineQuest.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates the first pirate quest scene and adds a clearly labelled entry point to the
/// existing Student Hub. Run after the Gameplay/Pirate asset folder has been imported.
/// </summary>
public static class SetupPirateQuestIntegration
{
    private const string PirateQuestScenePath = "Assets/Scenes/Gameplay/PirateQuest.unity";
    private const string StudentHubScenePath = "Assets/Scenes/StudentPages/StudentHub.unity";
    private const string ClassroomScenePath = "Assets/Scenes/StudentPages/ClassroomScene.unity";
    private const string TeacherClassScenePath = "Assets/Scenes/TeacherPages/TeacherClass.unity";

    [MenuItem("Tools/Imagine Quest/Setup Pirate Quest Integration")]
    public static void Run()
    {
        CreatePirateQuestScene();
        AddHubLaunchButton();
        AddClassLobbyControllers();
        AddScenesToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Pirate quest ready", "Connected the student and teacher class lobbies to the PirateQuest route. Teachers now unlock the quest per class; students launch it from their class lobby.", "OK");
    }

    private static void CreatePirateQuestScene()
    {
        var directory = System.IO.Path.GetDirectoryName(PirateQuestScenePath);
        if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Pirate Quest Runtime");
        var bootstrap = root.AddComponent<PirateQuestBootstrap>();

        var piratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gameplay/Pirate/Prefabs/Pirate_Full.prefab");
        if (piratePrefab != null)
        {
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("pirateNpcPrefab").objectReferenceValue = piratePrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        CreateVideoOverlay();
        EditorSceneManager.SaveScene(scene, PirateQuestScenePath);
    }

    private static void CreateVideoOverlay()
    {
        var canvasObject = new GameObject("Quest Video Overlay");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        var group = canvasObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        var imageObject = new GameObject("Video Display");
        imageObject.transform.SetParent(canvasObject.transform, false);
        var rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        imageObject.AddComponent<RawImage>().color = Color.white;

        canvasObject.AddComponent<UrlVideoSequencePlayer>();
    }

    private static void AddHubLaunchButton()
    {
        var scene = EditorSceneManager.OpenScene(StudentHubScenePath, OpenSceneMode.Single);
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Pirate Quest] StudentHub has no Canvas; launch button was not added.");
            return;
        }

        var launcher = canvas.GetComponent<QuestLauncher>();
        if (launcher == null)
            launcher = canvas.gameObject.AddComponent<QuestLauncher>();

        var existing = canvas.transform.Find("StartPirateQuestButton");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var buttonObject = new GameObject("StartPirateQuestButton");
        buttonObject.transform.SetParent(canvas.transform, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-42f, 38f);
        rect.sizeDelta = new Vector2(420f, 88f);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.68f, 0.38f, 0.08f, 0.96f);
        var button = buttonObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 0.65f, 1f);
        colors.pressedColor = new Color(0.78f, 0.58f, 0.28f, 1f);
        button.colors = colors;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, launcher.LaunchPirateQuest);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "START PIRATE QUEST";
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 28f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.95f, 0.8f);
        label.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddClassLobbyControllers()
    {
        AddComponentToCanvas<StudentQuestLobby>(StudentHubScenePath, "StudentHub");
        AddComponentToCanvas<StudentQuestLobby>(ClassroomScenePath, "ClassroomScene");
        AddComponentToCanvas<TeacherQuestLobby>(TeacherClassScenePath, "TeacherClass");
    }

    private static void AddComponentToCanvas<T>(string scenePath, string sceneName) where T : Component
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"[Pirate Quest] {sceneName} has no Canvas; no quest lobby controller was added.");
            return;
        }

        if (canvas.GetComponent<T>() == null)
            canvas.gameObject.AddComponent<T>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddScenesToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        AddSceneIfMissing(scenes, StudentHubScenePath);
        AddSceneIfMissing(scenes, PirateQuestScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string path)
    {
        foreach (var scene in scenes)
        {
            if (scene.path == path)
                return;
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
    }
}
