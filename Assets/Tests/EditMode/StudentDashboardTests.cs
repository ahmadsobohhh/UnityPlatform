using System;
using System.Collections.Generic;
using ImagineQuest.Student;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public class StudentDashboardTests
{
    private static Dictionary<string, object> Profile => new Dictionary<string, object>
    {
        { "username", "Aparna" }, { "avatarId", "mage" }, { "characterColor", "88AAFF" }
    };

    [Test]
    public void NewStudentDefaultsComeFromTheDataModel()
    {
        var data = StudentDashboardSnapshot.FromDocuments(Profile, null, null);
        Assert.AreEqual(1, data.Economy.Level);
        Assert.AreEqual(0, data.Economy.Xp);
        Assert.AreEqual(0, data.Economy.Crystals);
        Assert.AreEqual(0, data.Economy.Gold);
        Assert.AreEqual(50, data.Standing.Hearts);
    }

    [Test]
    public void UsesAuthoritativeWalletAndSeparateStandingIncludingZeroHearts()
    {
        var profile = Profile;
        profile["xp"] = 999999L; // Legacy/provisional profile stats are not authoritative.
        var wallet = new Dictionary<string, object>
        {
            { "level", 7L }, { "xp", 4200L }, { "crystals", 31L }, { "gold", 86L }, { "hearts", 999L }
        };
        var standing = new Dictionary<string, object> { { "hearts", 0L }, { "xp", 123L } };
        var data = StudentDashboardSnapshot.FromDocuments(profile, wallet, standing);
        Assert.AreEqual("Aparna", data.DisplayName);
        Assert.AreEqual("mage", data.AvatarId);
        Assert.AreEqual("88AAFF", data.CharacterColor);
        Assert.AreEqual(7, data.Economy.Level);
        Assert.AreEqual(4200, data.Economy.Xp);
        Assert.AreEqual(31, data.Economy.Crystals);
        Assert.AreEqual(86, data.Economy.Gold);
        Assert.AreEqual(0, data.Standing.Hearts);
        Assert.AreEqual(4200L, wallet["xp"], "Displaying zero Hearts must not reset the wallet.");
    }

    [Test]
    public void PartialWalletDoesNotInventLevelThresholds()
    {
        var data = StudentDashboardSnapshot.FromDocuments(Profile,
            new Dictionary<string, object> { { "xp", 100000L } }, null);
        Assert.AreEqual(1, data.Economy.Level);
        Assert.AreEqual(100000, data.Economy.Xp);
    }

    [Test]
    public void MissingUsernameFallsBackToStudentName()
    {
        var profile = new Dictionary<string, object> { { "firstName", " Ada " }, { "lastName", "Lovelace" } };
        Assert.AreEqual("Ada Lovelace", StudentDashboardSnapshot.FromDocuments(profile, null, null).DisplayName);
        Assert.AreEqual("Student", StudentDashboardSnapshot.FromDocuments(new Dictionary<string, object>(), null, null).DisplayName);
    }

    [TestCase("xp", -1L)]
    [TestCase("level", 0L)]
    [TestCase("crystals", "twenty")]
    [TestCase("gold", 1.5d)]
    public void MalformedBalancesAreNotDisplayedAsFreshAccounts(string field, object value)
    {
        Assert.Throws<InvalidOperationException>(() => StudentDashboardSnapshot.FromDocuments(Profile,
            new Dictionary<string, object> { { field, value } }, null));
    }

    [Test]
    public void MissingProfileFailsAndLargeBalancesArePreserved()
    {
        Assert.Throws<InvalidOperationException>(() => StudentDashboardSnapshot.FromDocuments(null, null, null));
        var data = StudentDashboardSnapshot.FromDocuments(Profile,
            new Dictionary<string, object> { { "xp", long.MaxValue } }, null);
        Assert.AreEqual(long.MaxValue, data.Economy.Xp);
    }

    [Test]
    public void StudentHubHasDashboardWithTheExistingAvatarCatalog()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/StudentPages/StudentHub.unity", OpenSceneMode.Additive);
        try
        {
            StudentDashboard dashboard = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                dashboard = root.GetComponentInChildren<StudentDashboard>(true);
                if (dashboard != null) break;
            }
            Assert.IsNotNull(dashboard);
            var serialized = new UnityEditor.SerializedObject(dashboard);
            var ids = serialized.FindProperty("avatarIds");
            var sprites = serialized.FindProperty("avatarSprites");
            Assert.AreEqual(8, ids.arraySize);
            Assert.AreEqual(ids.arraySize, sprites.arraySize);
            for (int i = 0; i < ids.arraySize; i++)
                Assert.IsNotNull(sprites.GetArrayElementAtIndex(i).objectReferenceValue, ids.GetArrayElementAtIndex(i).stringValue);
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}
