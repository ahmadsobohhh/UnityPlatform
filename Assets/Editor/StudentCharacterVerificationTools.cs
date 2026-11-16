using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using ImagineQuest.Characters;

[InitializeOnLoad]
public static class StudentCharacterVerificationTools
{
    private const string ActiveKey="Character verification active";
    private static readonly TestRunnerApi Api;
    public static string Output=>Path.GetFullPath("Temp/CharacterVerification");
    static StudentCharacterVerificationTools()
    {
        Api=ScriptableObject.CreateInstance<TestRunnerApi>();Api.RegisterCallbacks(new Results());
    }
    [MenuItem("Imagine Quest/Character/Verify Character Data")]
    public static void EditChecks()=>Run(false,false);
    [MenuItem("Imagine Quest/Character/Verify Character Models")]
    public static void PlayChecks()=>Run(true,false);
    [MenuItem("Imagine Quest/Character/Verify Windows Characters")]
    public static void WindowsChecks()=>Run(true,true);
    [Serializable] private sealed class MeshSnapshot {public string name;public Vector3[] vertices;public int[] triangles;}
    [Serializable] private sealed class CollarSnapshot {public Vector3 neck,head;public MeshSnapshot[] meshes;}
    [MenuItem("Imagine Quest/Character/Inspect Character Collars")]
    public static void InspectCollars()
    {
        if(EditorApplication.isPlaying) return;
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            for(int body=0;body<2;body++)
            {
                var root=UnityEngine.Object.Instantiate(StudentCharacterCatalog.Load().Bodies[body]);
                SceneManager.MoveGameObjectToScene(root,scene);
                var bones=root.GetComponentsInChildren<Transform>(true);
                var snapshot=new CollarSnapshot {neck=bones.First(t=>t.name=="neck_01").position,head=bones.First(t=>t.name=="Head").position,
                    meshes=root.GetComponentsInChildren<SkinnedMeshRenderer>().Select(renderer=>
                    {
                        var mesh=new Mesh();renderer.BakeMesh(mesh);
                        var result=new MeshSnapshot {name=renderer.name,vertices=mesh.vertices.Select(renderer.transform.TransformPoint).ToArray(),triangles=mesh.triangles};
                        UnityEngine.Object.DestroyImmediate(mesh);return result;
                    }).ToArray()};
                File.WriteAllText(Path.Combine(Output,$"collar-{body}.json"),JsonUtility.ToJson(snapshot));
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
        Debug.Log("CHARACTER collar geometry exported to "+Output);
    }
    private static void Run(bool play,bool native)
    {
        if(EditorApplication.isPlaying || SessionState.GetBool(ActiveKey,false)) return;
        Directory.CreateDirectory(Output);
        SessionState.SetBool(ActiveKey,true);
        SessionState.SetString(ActiveKey+" mode",native?"windows":play?"models":"data");
        SessionState.SetString(ActiveKey+" offline",Environment.GetEnvironmentVariable("IMAGINEQUEST_OFFLINE_TESTS")??"");
        SessionState.SetString(ActiveKey+" output",Environment.GetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT")??"");
        Environment.SetEnvironmentVariable("IMAGINEQUEST_OFFLINE_TESTS","1");
        Environment.SetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT",Output);
        File.Copy("ProjectSettings/EditorBuildSettings.asset",Path.Combine(Output,"EditorBuildSettings.before"),true);
        var settings=new ExecutionSettings(new Filter {testMode=play?TestMode.PlayMode:TestMode.EditMode,
            groupNames=play?new[]{"^StudentCharacterCreationTests","^StudentCharacterGeometryTests"}:new[]{"^StudentAppearanceTests"}});
        if(native)
        {
            // These properties are internal in the project's pinned test framework.
            // Keep the version-specific adapter confined to this editor verification tool.
            typeof(ExecutionSettings).GetProperty("targetPlatform",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(settings,(BuildTarget?)BuildTarget.StandaloneWindows64);
            typeof(ExecutionSettings).GetProperty("playerSavePath",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(settings,Path.Combine(Output,"WindowsPlayer"));
        }
        Api.Execute(settings);
    }
    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor test) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            if(!SessionState.GetBool(ActiveKey,false)) return;
            string mode=SessionState.GetString(ActiveKey+" mode","models");
            TestRunnerApi.SaveResultToFile(result,Path.Combine(Output,mode+".xml"));
            Environment.SetEnvironmentVariable("IMAGINEQUEST_OFFLINE_TESTS",SessionState.GetString(ActiveKey+" offline","") is string prior && prior.Length>0?prior:null);
            string previous=SessionState.GetString(ActiveKey+" output","");Environment.SetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT",previous.Length>0?previous:null);
            File.Copy(Path.Combine(Output,"EditorBuildSettings.before"),"ProjectSettings/EditorBuildSettings.asset",true);
            SessionState.SetBool(ActiveKey,false);
            Debug.Log($"CHARACTER {mode}: {result.PassCount} passed; {result.FailCount} failed. Evidence: {Output}");
        }
    }
}
