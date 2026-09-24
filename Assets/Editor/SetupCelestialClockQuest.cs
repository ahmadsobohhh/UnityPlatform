using System.Collections.Generic;
using ImagineQuest.Gameplay;
using ImagineQuest.QuestFramework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click scene setup for the Celestial Clock vertical slice. Keeping the asset
/// assignment here makes the gameplay script safe to run even if an artist later swaps
/// the ship, captain, or chest prefab.
/// </summary>
public static class SetupCelestialClockQuest
{
    private const string QuestScenePath = "Assets/Scenes/Gameplay/CelestialClockQuest.unity";
    private const string StudentHubScenePath = "Assets/Scenes/StudentPages/StudentHub.unity";
    private const string ClassroomScenePath = "Assets/Scenes/StudentPages/ClassroomScene.unity";
    private const string ShipPrefabPath = "Assets/Gameplay/ColonialShip/02_Prefabs/Colonial Ship_Empty.prefab";
    private const string CaptainPrefabPath = "Assets/Gameplay/Pirate/Prefabs/Pirate_01.prefab";
    private const string TreasurePrefabPath = "Assets/Gameplay/ColonialShip/02_Prefabs/Extra Items/Treasure_Chest.prefab";
    private const string QuestDefinitionPath = "Assets/Content/Quests/CelestialClockQuest.asset";

    [MenuItem("Tools/Imagine Quest/Setup Celestial Clock Quest")]
    public static void RunFromMenu()
    {
        RunInternal(true);
    }

    /// <summary>Batch-mode entry point used by the project verification command.</summary>
    public static void RunFromCommandLine()
    {
        RunInternal(false);
    }

    private static void RunInternal(bool showDialog)
    {
        var definition = CreateOrUpdateQuestDefinition();
        CreateQuestScene(definition);
        UpdateExistingQuestLaunchers();
        AddQuestSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Celestial Clock quest ready",
                "Created the CelestialClockQuest scene and updated existing student quest launch buttons to use it.", "OK");
        }
        else
        {
            Debug.Log("[CelestialClockQuest] Setup completed.");
        }
    }

    private static void CreateQuestScene(QuestDefinition definition)
    {
        var directory = System.IO.Path.GetDirectoryName(QuestScenePath);
        if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Celestial Clock Quest Runtime");
        var bootstrap = root.AddComponent<CelestialClockQuestBootstrap>();

        AssignPrefab(bootstrap, "colonialShipPrefab", ShipPrefabPath);
        AssignPrefab(bootstrap, "pirateCaptainPrefab", CaptainPrefabPath);
        AssignPrefab(bootstrap, "treasureChestPrefab", TreasurePrefabPath);
        var serialized = new SerializedObject(bootstrap);
        serialized.FindProperty("questDefinition").objectReferenceValue = definition;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, QuestScenePath);
    }

    private static QuestDefinition CreateOrUpdateQuestDefinition()
    {
        var directory = System.IO.Path.GetDirectoryName(QuestDefinitionPath);
        if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        var definition = AssetDatabase.LoadAssetAtPath<QuestDefinition>(QuestDefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<QuestDefinition>();
            AssetDatabase.CreateAsset(definition, QuestDefinitionPath);
        }

        var serialized = new SerializedObject(definition);
        serialized.FindProperty("questId").stringValue = "celestial-clock";
        serialized.FindProperty("displayName").stringValue = "The Riddle of the Celestial Clock";
        serialized.FindProperty("contentVersion").stringValue = "1.0.0";
        serialized.FindProperty("summary").stringValue =
            "A shipboard learning quest where students restore four Celestial Clock seals through fractions, angle turns, and one-step equations.";

        var rewardPolicy = serialized.FindProperty("rewardPolicy");
        rewardPolicy.FindPropertyRelative("experiencePerCorrectChallenge").intValue = 30;
        rewardPolicy.FindPropertyRelative("completionExperience").intValue = 10;
        rewardPolicy.FindPropertyRelative("perfectRunBonusExperience").intValue = 0;
        rewardPolicy.FindPropertyRelative("rewardDescriptor").stringValue = "Celestial navigator commendation";

        var nodes = serialized.FindProperty("nodes");
        nodes.arraySize = 4;
        ConfigureNode(nodes.GetArrayElementAtIndex(0), "sail-fraction", "The Sailmaker's Seal", QuestNodeType.Practice,
            "Restore the fraction seal.",
            "A fraction names equal parts of a whole. The bottom number tells how many equal parts there are; the top number tells how many are chosen.",
            "Three of four storm-sail panels must be raised. Which fraction names the raised panels?",
            "fraction-of-a-whole", new[] { "1/4", "3/4", "4/3" }, 1,
            "The denominator counts equal panels; the numerator counts raised panels.");
        ConfigureNode(nodes.GetArrayElementAtIndex(1), "compass-angle", "The Compass Seal", QuestNodeType.Practice,
            "Use a quarter turn to restore the compass.",
            "A full turn is 360°. One quarter-turn is 90°, so it carries a ship from north to east when turning clockwise.",
            "The compass points north. The Aurora makes one quarter-turn clockwise. Which direction is it facing?",
            "right-angle-turn", new[] { "West", "East", "South" }, 1,
            "A clockwise quarter-turn from north is east.");
        ConfigureNode(nodes.GetArrayElementAtIndex(2), "star-equation", "The Starlock Seal", QuestNodeType.Practice,
            "Balance the starlock equation.",
            "An equation is balanced like a ship's scale. Undo the operation beside the unknown to discover its value.",
            "The starlock reads: x + 7 = 19. What value unlocks x?",
            "one-step-addition-equation", new[] { "10", "12", "26" }, 1,
            "Undo +7 by subtracting 7 from 19.");
        ConfigureNode(nodes.GetArrayElementAtIndex(3), "celestial-clock", "Wake the Celestial Clock", QuestNodeType.BossChallenge,
            "Use clock-angle reasoning to wake the final seal.",
            "Clock angles use the same idea as turns. From 12 to 3 is one quarter of a full 360° turn, which is 90°.",
            "The minute hand points to 12 and the hour hand points to 3. What angle lies between the hands?",
            "right-angle-clock", new[] { "45°", "90°", "180°" }, 1,
            "From 12 to 3 is one quarter of a full turn: 90°.");

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void ConfigureNode(SerializedProperty node, string nodeId, string title, QuestNodeType type,
        string objective, string teachingNote, string prompt, string conceptId, string[] answerLabels, int correctIndex, string hint)
    {
        node.FindPropertyRelative("nodeId").stringValue = nodeId;
        node.FindPropertyRelative("title").stringValue = title;
        node.FindPropertyRelative("nodeType").enumValueIndex = (int)type;
        node.FindPropertyRelative("objectiveText").stringValue = objective;
        node.FindPropertyRelative("narrativeText").stringValue = teachingNote;
        node.FindPropertyRelative("requiredForCompletion").boolValue = true;

        var challenges = node.FindPropertyRelative("challenges");
        challenges.arraySize = 1;
        var challenge = challenges.GetArrayElementAtIndex(0);
        challenge.FindPropertyRelative("challengeId").stringValue = nodeId + "-challenge";
        challenge.FindPropertyRelative("conceptId").stringValue = conceptId;
        challenge.FindPropertyRelative("challengeType").enumValueIndex = (int)QuestChallengeType.MultipleChoice;
        challenge.FindPropertyRelative("prompt").stringValue = prompt;
        challenge.FindPropertyRelative("supportingText").stringValue = "Use the Teaching Note if you want a reminder.";
        challenge.FindPropertyRelative("correctAnswerId").stringValue = "answer-" + correctIndex;
        challenge.FindPropertyRelative("maximumAttempts").intValue = 3;
        challenge.FindPropertyRelative("requiredForCompletion").boolValue = true;
        challenge.FindPropertyRelative("hintText").stringValue = hint;
        challenge.FindPropertyRelative("successFeedback").stringValue = "Seal restored.";
        challenge.FindPropertyRelative("retryFeedback").stringValue = "Try the Teaching Note, then make another choice.";

        var answers = challenge.FindPropertyRelative("answers");
        answers.arraySize = answerLabels.Length;
        for (var index = 0; index < answerLabels.Length; index++)
        {
            var answer = answers.GetArrayElementAtIndex(index);
            answer.FindPropertyRelative("answerId").stringValue = "answer-" + index;
            answer.FindPropertyRelative("label").stringValue = answerLabels[index];
            answer.FindPropertyRelative("detailText").stringValue = string.Empty;
        }
    }

    private static void AssignPrefab(CelestialClockQuestBootstrap bootstrap, string propertyName, string assetPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("[CelestialClockQuest] Missing optional asset: " + assetPath);
            return;
        }

        var serialized = new SerializedObject(bootstrap);
        var property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError("[CelestialClockQuest] Could not find serialized property " + propertyName + ".");
            return;
        }

        property.objectReferenceValue = prefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void UpdateExistingQuestLaunchers()
    {
        UpdateLauncherInScene(StudentHubScenePath, true);
        UpdateLauncherInScene(ClassroomScenePath, false);
    }

    private static void UpdateLauncherInScene(string scenePath, bool updateHubButtonLabel)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var launcher = Object.FindFirstObjectByType<QuestLauncher>();
        if (launcher == null)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[CelestialClockQuest] No Canvas found in " + scenePath + ".");
                return;
            }

            launcher = canvas.gameObject.AddComponent<QuestLauncher>();
        }

        var serialized = new SerializedObject(launcher);
        var sceneName = serialized.FindProperty("questSceneName");
        if (sceneName != null)
        {
            sceneName.stringValue = "CelestialClockQuest";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var questId = serialized.FindProperty("defaultQuestId");
        if (questId != null)
        {
            questId.stringValue = "celestial-clock";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var assignmentId = serialized.FindProperty("defaultAssignmentId");
        if (assignmentId != null)
        {
            assignmentId.stringValue = "pirate-voyage";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        if (updateHubButtonLabel)
        {
            var buttonTransform = FindTransformInLoadedScene("StartPirateQuestButton");
            if (buttonTransform != null)
            {
                var label = buttonTransform.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = "BEGIN CELESTIAL CLOCK";
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Transform FindTransformInLoadedScene(string name)
    {
        var activeScene = SceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }
        }
        return null;
    }

    private static void AddQuestSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var containsQuest = false;
        foreach (var scene in scenes)
        {
            if (scene.path == QuestScenePath)
            {
                containsQuest = true;
                break;
            }
        }

        if (!containsQuest)
            scenes.Add(new EditorBuildSettingsScene(QuestScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
