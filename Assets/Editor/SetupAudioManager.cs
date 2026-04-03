using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupAudioManager
{
    [MenuItem("Tools/Setup Audio Manager")]
    public static void Run()
    {
        string scenePath = "Assets/Scenes/UniversalPages/WelcomePage.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var existing = GameObject.Find("AudioManager");
        AudioManager manager;

        if (existing != null)
        {
            manager = existing.GetComponent<AudioManager>();
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
            Debug.Log($"[AudioManager Setup] Music clip: {clipPath}");
        }
        else
        {
            Debug.LogWarning("[AudioManager Setup] No audio clips found in Assets/Audio/Music.");
        }

        string[] sfxGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" });
        foreach (string guid in sfxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (lower.Contains("hover"))
            {
                so.FindProperty("buttonHoverClip").objectReferenceValue = clip;
                Debug.Log($"[AudioManager Setup] Hover clip: {path}");
            }
            else if (lower.Contains("click"))
            {
                so.FindProperty("buttonClickClip").objectReferenceValue = clip;
                Debug.Log($"[AudioManager Setup] Click clip: {path}");
            }
        }

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        string msg = existing != null
            ? "AudioManager updated with new music clip!"
            : "AudioManager created and saved in WelcomePage!";
        EditorUtility.DisplayDialog("Audio Manager", msg, "OK");
    }
}
