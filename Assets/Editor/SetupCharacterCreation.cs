using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupCharacterCreation
{
    [MenuItem("Tools/Setup Character Creation")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/StudentPages/StudentAvatarSelect.unity");
        foreach(var legacy in Object.FindObjectsByType<StudentCharacterSelect>(FindObjectsSortMode.None))
            Object.DestroyImmediate(legacy.gameObject);
        var canvas=Object.FindFirstObjectByType<Canvas>();
        if(canvas==null) canvas=new GameObject("Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
        for(int i=canvas.transform.childCount-1;i>=0;i--) Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
        if(canvas.GetComponent<CharacterCreation>()==null) canvas.gameObject.AddComponent<CharacterCreation>();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    [MenuItem("Imagine Quest/Character/Play Creator")]
    public static void Play()
    {
        if(EditorApplication.isPlaying) return;
        EditorSceneManager.OpenScene("Assets/Scenes/StudentPages/StudentAvatarSelect.unity");
        EditorApplication.isPlaying=true;
    }
}
