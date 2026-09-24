using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// Creates a local, standalone Windows preview that starts directly on the
/// Celestial Clock quest. Build output stays in the ignored Builds folder.
/// </summary>
public static class BuildCelestialClockPreview
{
    private const string QuestScenePath = "Assets/Scenes/Gameplay/CelestialClockQuest.unity";
    private const string OutputPath = "Builds/CelestialClockPreview/CelestialClockPreview.exe";

    [MenuItem("Tools/Imagine Quest/Build Celestial Clock Preview")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void BuildFromCommandLine()
    {
        Build();
    }

    private static void Build()
    {
        var outputDirectory = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { QuestScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Celestial Clock preview build failed: " + report.summary.result);

        UnityEngine.Debug.Log("[CelestialClockQuest] Preview built at " + OutputPath);
    }
}
