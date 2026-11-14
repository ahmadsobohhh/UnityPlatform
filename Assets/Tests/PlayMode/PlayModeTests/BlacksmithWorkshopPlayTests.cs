using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImagineQuest.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class BlacksmithWorkshopPlayTests
{
    private string checkpoint;
    private BlacksmithRoomBootstrap room;
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private object Field(string name) => room.GetType().GetField(name, Private).GetValue(room);
    private void Call(string name, params object[] args) => room.GetType().GetMethod(name, Private).Invoke(room, args);

    [UnityTest]
    public IEnumerator WorkshopCanLearnRestoreUnlockAndReview()
    {
        QuestSession.Begin(BlacksmithQuestContent.Id, "local-practice");
        yield return SceneManager.LoadSceneAsync("BlacksmithRoom");
        yield return null;
        room = UnityEngine.Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        Assert.That(room, Is.Not.Null);
        var service = room.GetComponent<BlacksmithQuestService>();
        Assert.That(service.Ready, Is.True);
        string assignment = "smoke-" + Guid.NewGuid().ToString("N");
        service.Initialize(null, assignment);
        checkpoint = new BlacksmithQuestStore(Path.Combine(Application.persistentDataPath, "BlacksmithQuest"))
            .FilePath(service.State.userId, service.State.classId, assignment);
        Assert.That(service.IsPractice, Is.True);
        Call("ShowModal", new object[] { null });
        // Initial URP variants can display a cyan placeholder during asynchronous compilation.
        yield return new WaitForSecondsRealtime(3);
#if UNITY_EDITOR
        float deadline = Time.realtimeSinceStartup + 30;
        while (UnityEditor.ShaderUtil.anythingCompiling && Time.realtimeSinceStartup < deadline) yield return null;
#endif
        if (!Application.isBatchMode)
        {
            yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            int cyan = 0, sampled = 0;
            for (int y = 0; y < frame.height; y += 40)
                for (int x = 0; x < frame.width; x += 40)
                {
                    Color pixel = frame.GetPixel(x, y); sampled++;
                    if (pixel.r < .05f && pixel.g > .9f && pixel.b > .9f) cyan++;
                }
            UnityEngine.Object.Destroy(frame);
            Assert.That(cyan / (float)sampled, Is.LessThan(.10f), "The workshop must render geometry, not pending shader placeholders.");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/BlacksmithWorkshop-Room.png"));
        }
        Assert.That(((List<TMP_Text>)Field("stationLabels")).Count, Is.EqualTo(3));
        Assert.That(((List<GameObject>)Field("repairParts")).TrueForAll(p => !p.activeSelf), Is.True);

        foreach (int station in new[] { 0, 1, 2 })
        {
            Call("OpenStation", station);
            Assert.That(((GameObject)Field("lessonPanel")).activeSelf, Is.True);
            Assert.That(service.State.HasLesson(station), Is.True);
            Call("AnswerStation", (station + 1) % 3);
            Assert.That(service.State.HasAnswer(station), Is.False);
            if (station == 1 && !Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/BlacksmithWorkshop-Lesson.png"));
            }
            Call("AnswerStation", station);
            Assert.That(service.State.HasAnswer(station), Is.True);
            Assert.That(((List<GameObject>)Field("repairParts"))[station].activeSelf, Is.True);
            Call("ShowModal", new object[] { null });
        }
        Assert.That(service.State.HasKey, Is.True);
        Assert.That(((TMP_Text)Field("keyText")).text, Does.Contain("READY"));
        Assert.That(service.State.verifiedXp, Is.Zero);
        service.Initialize(null, assignment);
        Assert.That(service.State.CompletedCount, Is.EqualTo(3), "Completion must survive loading its checkpoint.");
        Assert.That(service.State.TryOpenPortal(), Is.True);
        service.Save();
        Call("ShowCompletion");
        Assert.That(((GameObject)Field("completionPanel")).activeSelf, Is.True);
        if (!Application.isBatchMode)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/BlacksmithWorkshop-Complete.png"));
        }
        Call("ShowModal", new object[] { null });
        var player = (CharacterController)Field("player");
        player.enabled = false;
        player.transform.position = new Vector3(0, .05f, -.9f);
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        player.enabled = true;
        if (!Application.isBatchMode)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/BlacksmithWorkshop-Portal.png"));
        }
        Assert.That(service.State.TurnIn(), Is.True);
        Assert.That(service.Save(), Is.True);
        service.Initialize(null, assignment);
        Assert.That(service.State.turnedIn, Is.True);
        // Screenshots are asynchronous; allow the render thread to finish writing.
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator CinderQuayCanFinishBothWavesWithTheWeakestBlade()
    {
        if (QuestSession.Instance != null) QuestSession.Instance.Clear();
        yield return SceneManager.LoadSceneAsync("BlacksmithRoom");
        yield return null;
        room = UnityEngine.Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        Assert.That(room.GetComponent<BlacksmithQuestService>(), Is.Null, "The raid must not write assigned quest progress.");
        var trial = (WorkshopBladeTrial)Field("bladeTrial");
        trial.TryAnswer(0, 0, out _);
        trial.TryAnswer(1, 0, out _);
        trial.TryAnswer(2, 1, out _);
        Assert.That(trial.Damage, Is.EqualTo(16));
        Call("ForgeRaidBlade");
        Assert.That(((GameObject)Field("raidDoor")).activeSelf, Is.False);
        Call("StartRaid");
        Call("ShowModal", new object[] { Field("pausePanel") });
        var combat = Field("raidCombat");
        var combatType = combat.GetType();
        var tick = combatType.GetMethod("Tick");
        var strike = combatType.GetMethod("Strike", Private);
        var enemies = (IList)combatType.GetField("enemies", Private).GetValue(combat);
        var player = (CharacterController)Field("player");
        for (int wave = 1; wave <= 2; wave++)
        {
            tick.Invoke(combat, new object[] { 2f, null });
            Assert.That(enemies.Count, Is.EqualTo(wave + 1));
            while (enemies.Count > 0)
            {
                var foe = enemies[0];
                var root = (Transform)foe.GetType().GetField("Root").GetValue(foe);
                Call("PlaceRaidPlayer", root.position + new Vector3(0, .035f, 1.7f), 180f);
                Physics.SyncTransforms();
                for (int hit = 0; hit < 3; hit++) strike.Invoke(combat, null);
                Assert.That(enemies.Contains(foe), Is.False, "48 HP requires three 16-damage strikes.");
                var fallen = (IList)combatType.GetField("debris", Private).GetValue(combat);
                Assert.That(fallen.Count, Is.GreaterThanOrEqualTo(6));
                var skull = (GameObject)fallen[fallen.Count - 6];
                Assert.That(skull.TryGetComponent<Rigidbody>(out var body) && !body.isKinematic, Is.True);
                Assert.That(skull.TryGetComponent<BoxCollider>(out _), Is.True);
            }
        }
        Assert.That((bool)tick.Invoke(combat, new object[] { .02f, null }), Is.True);
        Assert.That(player.enabled, Is.True);
        Call("ResetRaid");
        Assert.That(trial.Total, Is.Zero);
        Assert.That(((GameObject)Field("raidDoor")).activeSelf, Is.True);
        Assert.That(((GameObject)Field("raidGate")).activeSelf, Is.False);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (room != null) UnityEngine.Object.Destroy(room.gameObject);
        if (QuestSession.Instance != null) QuestSession.Instance.Clear();
        yield return null;
        if (checkpoint != null && File.Exists(checkpoint)) File.Delete(checkpoint);
    }
}
