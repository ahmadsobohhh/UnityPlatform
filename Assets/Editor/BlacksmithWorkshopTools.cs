using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
using System.IO;
using System;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public static class BlacksmithWorkshopTools
{
    private static readonly TestRunnerApi Api;
    static BlacksmithWorkshopTools()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new Results());
    }
    [MenuItem("Imagine Quest/Blacksmith/Run Workshop Checks")]
    public static void RunChecks()
    {
        if (EditorApplication.isPlaying) return;
        Api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, groupNames = new[] { "^BlacksmithWorkshopTests" } }));
    }

    [MenuItem("Imagine Quest/Blacksmith/Run Workshop Play Checks")]
    public static void RunPlayChecks()
    {
        if (EditorApplication.isPlaying) return;
        Api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode,
            groupNames = new[] { "^BlacksmithWorkshopPlayTests", "^WorkshopForgeRewardTests", "^WorkshopPortalFlowTests" } }));
    }

    private class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor test) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, Path.GetFullPath("Temp/BlacksmithWorkshopResults.xml"));
            Debug.Log("WORKSHOP CHECKS: " + result.PassCount + " passed; " + result.FailCount + " failed.");
        }
    }

    [MenuItem("Imagine Quest/Blacksmith/Open Workshop")]
    public static void OpenWorkshop()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Gameplay/BlacksmithRoom.unity");
        SceneView.lastActiveSceneView?.Repaint();
    }

    [MenuItem("Imagine Quest/Blacksmith/Play Workshop")]
    public static void PlayWorkshop()
    {
        if (EditorApplication.isPlaying) return;
        OpenWorkshop();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "BlacksmithRoom")
            EditorApplication.isPlaying = true;
    }

    // Command-line preview build; the normal application build settings remain unchanged.
    public static void BuildPortalPreview()
    {
        string output = Environment.GetEnvironmentVariable("IMAGINEQUEST_BUILD_OUTPUT");
        if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("Set IMAGINEQUEST_BUILD_OUTPUT to an output .exe path.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Gameplay/BlacksmithRoom.unity", "Assets/Scenes/Gameplay/QuestArena.unity" },
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Portal preview build failed: " + report.summary.result);
        Debug.Log("PORTAL PREVIEW BUILD SUCCEEDED: " + output);
    }
}
