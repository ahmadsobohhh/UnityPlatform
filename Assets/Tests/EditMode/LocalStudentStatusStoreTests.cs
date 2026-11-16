using System;
using System.IO;
using ImagineQuest.Student;
using NUnit.Framework;
using UnityEngine;

public sealed class LocalStudentStatusStoreTests
{
    private string directory;
    private LocalStudentStatusStore store;
    private static LocalStatusReward Reward(string receipt, int xp = 25) =>
        new LocalStatusReward { Receipt = receipt, Xp = xp, Title = "Craft mastered", Detail = "Local workshop" };

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Application.temporaryCachePath, "LocalStatusTests-" + Guid.NewGuid().ToString("N"));
        store = new LocalStudentStatusStore(directory);
    }

    [TearDown]
    public void TearDown()
    {
        Assert.That(Path.GetFullPath(directory), Does.StartWith(Path.GetFullPath(Application.temporaryCachePath) + Path.DirectorySeparatorChar + "LocalStatusTests-"));
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void FreshLocalStudentStartsAtFiftyHeartsAndZeroXp()
    {
        var saved = store.Load("student-a");
        Assert.AreEqual(50, saved.hearts); Assert.AreEqual(0, saved.xp);
        Assert.IsTrue(File.Exists(store.FilePath("student-a")));
        Assert.That(Path.GetFileName(store.FilePath("student-a")), Does.Not.Contain("student-a"));
    }

    [Test]
    public void CorrectCraftPersistsAcrossStoreInstancesAndCannotBeAwardedTwice()
    {
        var saved = store.Award("a", new[] { Reward("craft/0") }, out long awarded);
        Assert.AreEqual(25, awarded); Assert.AreEqual(25, saved.xp); Assert.AreEqual(50, saved.hearts);
        store = new LocalStudentStatusStore(directory);
        saved = store.Award("a", new[] { Reward("craft/0") }, out awarded);
        Assert.AreEqual(0, awarded); Assert.AreEqual(25, saved.xp); Assert.AreEqual(1, saved.activity.Count);
        Assert.IsTrue(File.Exists(store.FilePath("a") + ".bak"));
    }

    [Test]
    public void ThreeCraftsAndDeliveryAwardExactlyOneHundredSeventyFiveXp()
    {
        var rewards = new[] { Reward("craft/0"), Reward("craft/1"), Reward("craft/2"), Reward("delivery", 100) };
        Assert.AreEqual(175, store.Award("a", rewards, out long awarded).xp);
        Assert.AreEqual(175, awarded);
        Assert.AreEqual(175, store.Award("a", rewards, out awarded).xp);
        Assert.AreEqual(0, awarded);
    }

    [Test]
    public void ProfilesKeepIndependentStandingAndRewards()
    {
        store.Award("a", new[] { Reward("craft/0") }, out _);
        store.AdjustHearts("a", "teacher-1", -5, "Classroom standing", out _);
        Assert.AreEqual(50, store.Load("b").hearts); Assert.AreEqual(0, store.Load("b").xp);
        Assert.AreEqual(45, store.Load("a").hearts); Assert.AreEqual(25, store.Load("a").xp);
    }

    [Test]
    public void TeacherStandingAdjustmentsAreBoundedAndDeduplicateRetries()
    {
        Assert.Throws<InvalidOperationException>(() => store.AdjustHearts("a", "too-high", 1, "Reason", out _));
        Assert.Throws<ArgumentException>(() => store.AdjustHearts("a", "no-reason", -1, "", out _));
        Assert.AreEqual(49, store.AdjustHearts("a", "once", -1, "Late arrival", out bool applied).hearts);
        Assert.IsTrue(applied);
        Assert.AreEqual(49, store.AdjustHearts("a", "once", -1, "Late arrival", out applied).hearts);
        Assert.IsFalse(applied);
    }

    [Test]
    public void ZeroHeartsResetXpAndDoNotAllowReplayingAlreadyRewardedCrafts()
    {
        store.Award("a", new[] { Reward("craft/0") }, out _);
        var saved = store.AdjustHearts("a", "reset", -50, "A fresh start", out _);
        Assert.AreEqual(0, saved.hearts); Assert.AreEqual(0, saved.xp); Assert.AreEqual(1, saved.epoch);
        Assert.AreEqual(0, store.Award("a", new[] { Reward("craft/1") }, out long awarded).xp);
        Assert.AreEqual(0, awarded);
        store.AdjustHearts("a", "restore", 5, "Standing restored", out _);
        saved = store.Award("a", new[] { Reward("craft/0") }, out awarded);
        Assert.AreEqual(0, awarded); Assert.AreEqual(0, saved.xp); Assert.AreEqual(5, saved.hearts);
        Assert.AreEqual(25, store.Award("a", new[] { Reward("new-assignment/craft/0") }, out _).xp);
    }

    [Test]
    public void CorruptOrWrongOwnerSaveIsPreservedInsteadOfReset()
    {
        store.Load("a"); string path = store.FilePath("a");
        foreach (string invalid in new[] { "not json", "{\"schemaVersion\":1,\"ownerId\":\"someone-else\",\"hearts\":50}" })
        {
            File.WriteAllText(path, invalid);
            Assert.Throws<InvalidDataException>(() => store.Load("a"));
            Assert.AreEqual(invalid, File.ReadAllText(path));
        }
    }

    [Test]
    public void RecentActivityIsBoundedWhileOldRewardReceiptsStillPreventFarming()
    {
        for (int i = 0; i < 20; i++) store.Award("a", new[] { Reward("craft/" + i) }, out _);
        var saved = store.Award("a", new[] { Reward("craft/0") }, out long awarded);
        Assert.AreEqual(0, awarded); Assert.AreEqual(500, saved.xp);
        Assert.AreEqual(12, saved.activity.Count); Assert.AreEqual(20, saved.receipts.Count);
    }
}
