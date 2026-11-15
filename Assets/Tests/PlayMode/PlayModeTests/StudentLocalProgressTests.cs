using System;
using System.Collections;
using System.IO;
using System.Reflection;
using ImagineQuest.Gameplay;
using ImagineQuest.Student;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class StudentLocalProgressTests
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private BlacksmithRoomBootstrap room;
    private StudentStatusService status;
    private string directory, checkpoint;
    private void Call(string method, params object[] args) => typeof(BlacksmithRoomBootstrap).GetMethod(method, Private).Invoke(room, args);
    private void Profile(string owner) => typeof(StudentStatusService).GetMethod("UseLocalAccount", Private).Invoke(status, new object[] {owner, directory});

    [UnityTest]
    public IEnumerator ActualWorkshopUpdatesHudPersistsXpAndPreservesClassroomHearts()
    {
        Assert.AreEqual("1", Environment.GetEnvironmentVariable("IMAGINEQUEST_OFFLINE_TESTS"), "This fixture must never use a real account.");
        QuestSession.Begin(BlacksmithQuestContent.Id, "local-practice");
        yield return SceneManager.LoadSceneAsync("BlacksmithRoom"); yield return null;
        room = UnityEngine.Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        status = StudentStatusService.GetOrCreate();
        directory = Path.Combine(Application.temporaryCachePath, "LocalProgressTests-" + Guid.NewGuid().ToString("N"));
        Profile("local-guest");
        var quest = room.GetComponent<BlacksmithQuestService>();
        string assignment = "local-progress-" + Guid.NewGuid().ToString("N");
        quest.Initialize(null, assignment);
        checkpoint = new BlacksmithQuestStore(Path.Combine(Application.persistentDataPath, "BlacksmithQuest"))
            .FilePath(quest.State.userId, quest.State.classId, assignment);
        Assert.AreEqual(50, status.Snapshot.Hearts); Assert.AreEqual(0, status.Snapshot.Xp);
        var canvas = GameObject.Find("Workshop UI");
        var hud = canvas.transform.Find("Live Hearts and XP");
        Assert.AreEqual("50", hud.Find("Hearts").GetComponent<TMP_Text>().text);
        Assert.AreEqual("LOCAL XP", hud.Find("XP caption").GetComponent<TMP_Text>().text);
        Call("ShowModal", new object[] {null});
        foreach (int station in new[] {0, 1, 2})
        {
            Call("OpenStation", station);
            long before = status.Snapshot.Xp;
            Call("AnswerStation", (station + 1) % 3);
            Assert.AreEqual(before, status.Snapshot.Xp, "Wrong answers never earn XP.");
            Assert.AreEqual(50, status.Snapshot.Hearts, "Hearts are classroom standing, not quiz damage.");
            Call("AnswerStation", station);
            Assert.AreEqual(before + 25, status.Snapshot.Xp);
            Assert.AreEqual(status.Snapshot.Xp.ToString(), hud.Find("Saved XP").GetComponent<TMP_Text>().text);
            Call("OpenStation", station); Call("AnswerStation", station);
            Assert.AreEqual(before + 25, status.Snapshot.Xp, "Practising a mastered craft cannot multiply XP.");
            Call("ShowModal", new object[] {null});
        }
        Assert.AreEqual(75, status.Snapshot.Xp);
        quest.Initialize(null, assignment);
        Assert.AreEqual(75, status.Snapshot.Xp, "Reloading a checkpoint must not award completed crafts again.");
        Assert.IsTrue(quest.State.TryOpenPortal()); Assert.IsTrue(quest.State.TurnIn()); Assert.IsTrue(quest.Save());
        Assert.AreEqual(175, status.Snapshot.Xp); Assert.AreEqual(50, status.Snapshot.Hearts);
        Assert.AreEqual(175, status.SessionXp); Assert.AreEqual(0, quest.State.verifiedXp, "Local XP must never impersonate Firebase verification.");
        quest.Save(); Assert.AreEqual(175, status.Snapshot.Xp);

        new LocalStudentStatusStore(directory).AdjustHearts("local-guest", "teacher-local", -1, "Classroom standing", out _);
        status.Reconnect(); Assert.AreEqual(49, status.Snapshot.Hearts); Assert.AreEqual(175, status.Snapshot.Xp);
        var view = StudentStatusView.Attach(canvas.transform, true);
        view.OpenRecap();
        var card = canvas.transform.Find("Personal recap/Recap card");
        Assert.AreEqual("49 / 50", card.Find("Classroom standing/Heart total").GetComponent<TMP_Text>().text);
        Assert.AreEqual("175", card.Find("Experience/Experience total").GetComponent<TMP_Text>().text);
        Assert.That(card.Find("Cloud connection").GetComponent<TMP_Text>().text, Does.StartWith("Local progress"));
        string evidence = Environment.GetEnvironmentVariable("IMAGINEQUEST_TEST_OUTPUT");
        if (!string.IsNullOrWhiteSpace(evidence))
        {
            Directory.CreateDirectory(evidence); yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "Local-Progress-Verified.png"));
            yield return null; yield return null;
        }
        // Recreate the scene-independent service, as a fresh game process would.
        UnityEngine.Object.Destroy(status.gameObject); yield return null;
        status = StudentStatusService.GetOrCreate(); Profile("local-guest");
        Assert.AreEqual(175, status.Snapshot.Xp); Assert.AreEqual(49, status.Snapshot.Hearts); Assert.AreEqual(0, status.SessionXp);
        Profile("another-student"); Assert.AreEqual(0, status.Snapshot.Xp); Assert.AreEqual(50, status.Snapshot.Hearts);
        Profile("local-guest"); Assert.AreEqual(175, status.Snapshot.Xp); Assert.AreEqual(49, status.Snapshot.Hearts);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (room != null) UnityEngine.Object.Destroy(room.gameObject);
        if (status != null) UnityEngine.Object.Destroy(status.gameObject);
        if (QuestSession.Instance != null) QuestSession.Instance.Clear();
        yield return null;
        if (checkpoint != null && File.Exists(checkpoint)) File.Delete(checkpoint);
        if (directory != null && Directory.Exists(directory))
        {
            Assert.That(Path.GetFullPath(directory), Does.StartWith(Path.GetFullPath(Application.temporaryCachePath) + Path.DirectorySeparatorChar + "LocalProgressTests-"));
            Directory.Delete(directory, true);
        }
    }
}
