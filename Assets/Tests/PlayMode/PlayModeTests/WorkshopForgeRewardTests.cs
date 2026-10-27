using System;
using System.Collections;
using System.IO;
using System.Reflection;
using ImagineQuest.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Video;

public sealed class WorkshopForgeRewardTests
{
    private GameObject root;
    private WorkshopForgeReward reward;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Portal reward test");
        reward = root.AddComponent<WorkshopForgeReward>();
    }

    [UnityTearDown]
    public IEnumerator TearDown() { UnityEngine.Object.Destroy(root); yield return null; }

    [Test]
    public void WrongAndRepeatedAnswersCannotRevealStoneEarly()
    {
        var state = new BlacksmithQuestState();
        state.ReadLesson(2);
        state.Answer(2, 0);
        reward.SetCompletedStations(state.CompletedCount);
        Assert.IsFalse(reward.StoneRevealed);
        state.Answer(2, 2);
        state.Answer(2, 2);
        state.ReadLesson(0); state.Answer(0, 0);
        reward.SetCompletedStations(state.CompletedCount);
        Assert.IsFalse(reward.StoneRevealed);
        state.ReadLesson(1); state.Answer(1, 1);
        reward.SetCompletedStations(state.CompletedCount);
        Assert.IsTrue(reward.StoneRevealed);
        Assert.IsFalse(reward.PortalOpened);
    }

    [Test]
    public void TouchRequiresUnlockedStoneCorrectHeightAndPlayerControl()
    {
        Assert.IsFalse(reward.CheckPlayerProximity(new Vector3(0, .05f, .2f)));
        reward.SetCompletedStations(3);
        Assert.IsFalse(reward.CheckPlayerProximity(new Vector3(0, .05f, 3)));
        Assert.IsFalse(reward.CheckPlayerProximity(new Vector3(0, 3, .2f)));
        Assert.IsFalse(reward.CheckPlayerProximity(new Vector3(0, .05f, .2f), hasControl: false));
        Assert.IsTrue(reward.CheckPlayerProximity(new Vector3(0, .05f, .2f)));
        Assert.IsFalse(reward.CheckPlayerProximity(new Vector3(0, .05f, .2f)));
        Assert.IsFalse(root.transform.Find("Forge Stone Reward").gameObject.activeSelf);
        Assert.IsTrue(root.transform.Find("Living Emerald Portal").gameObject.activeSelf);
        Assert.AreEqual(2, root.transform.childCount, "A repeated touch must not duplicate the portal.");
    }

    [Test]
    public void SaveFailureCannotConsumeStoneOrOpenPortal()
    {
        reward.Initialize(() => false);
        reward.SetCompletedStations(3);
        Assert.IsFalse(reward.CheckPlayerProximity(new Vector3(0, .05f, .2f)));
        Assert.IsFalse(reward.PortalOpened);
        Assert.IsTrue(root.transform.Find("Forge Stone Reward").gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator RestoredPortalUsesClientAnimationAndTransparentOutline()
    {
        reward.RestoreProgress(3, true);
        float deadline = Time.realtimeSinceStartup + 20;
        while (!reward.VideoFrameReceived && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(reward.VideoFrameReceived, reward.MediaStatus);
        Assert.IsTrue(reward.IsVideoPlaying, "The actual client video must be playing.");
        var video = root.GetComponent<VideoPlayer>();
        Assert.That(video.clip.name, Is.EqualTo("ClientPortal"));
        long frame = video.frame;
        yield return new WaitForSecondsRealtime(1);
        Assert.That(video.frame, Is.Not.EqualTo(frame), "The artwork must animate.");
        Assert.IsTrue(reward.CanEnterPortal(new Vector3(0, .05f, -4.1f)));
        Assert.IsFalse(reward.CanEnterPortal(new Vector3(0, .05f, -3)));
        Assert.IsFalse(reward.CanEnterPortal(new Vector3(0, .05f, -4.1f), false));
        Assert.IsFalse(reward.CanEnterPortal(new Vector3(0, 3, -4.1f)));

        // Render only the client surface over magenta to detect a black video rectangle.
        var surface = reward.PortalRenderer;
        surface.gameObject.layer = 31;
        var cameraObject = new GameObject("Transparency verification camera", typeof(Camera));
        cameraObject.transform.SetParent(root.transform);
        var camera = cameraObject.GetComponent<Camera>();
        camera.transform.position = WorkshopForgeReward.PortalPosition + Vector3.forward * 3;
        camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta;
        camera.cullingMask = 1 << 31;
        camera.orthographic = true;
        camera.orthographicSize = 1.8f;
        var target = new RenderTexture(256, 384, 24);
        camera.targetTexture = target;
        yield return new WaitForSecondsRealtime(.3f);
        yield return new WaitForEndOfFrame();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var pixels = new Texture2D(256, 384, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 256, 384), 0, 0);
        pixels.Apply();
        RenderTexture.active = previous;
        Color outside = pixels.GetPixel(36, 26);
        Color center = pixels.GetPixel(128, 192);
        Assert.That(outside.r, Is.GreaterThan(.8f));
        Assert.That(outside.b, Is.GreaterThan(.8f));
        Assert.That(outside.g, Is.LessThan(.2f), "The outline must reveal the scene behind it.");
        Assert.That(center.g, Is.GreaterThan(center.b), "The actual green client portal must be visible.");
        PortalTestEvidence.Write("ClientPortal-Transparency.png", pixels.EncodeToPNG());
        UnityEngine.Object.Destroy(pixels);
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.Destroy(target);
        reward.RestoreProgress(3, true);
        Assert.AreEqual(3, root.transform.childCount, "Restoring twice must reuse the portal.");
    }
}

public sealed class WorkshopPortalFlowTests
{
    private string checkpoint;
    private BlacksmithRoomBootstrap room;
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private object Field(string name) => room.GetType().GetField(name, Private).GetValue(room);
    private void Call(string name, params object[] args) => room.GetType().GetMethod(name, Private).Invoke(room, args);
    private void MovePlayer(Vector3 feet)
    {
        var actor = (CharacterController)Field("player");
        actor.enabled = false;
        actor.transform.position = feet;
        actor.enabled = true;
    }

    [UnityTest]
    public IEnumerator CraftsTouchSaveReloadAndWalkThroughReturnToShip()
    {
        yield return SceneManager.LoadSceneAsync("BlacksmithRoom");
        yield return null;
        room = UnityEngine.Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        string assignment = "portal-flow-" + Guid.NewGuid().ToString("N");
        var service = room.GetComponent<BlacksmithQuestService>();
        service.Initialize(null, assignment);
        checkpoint = new BlacksmithQuestStore(Path.Combine(Application.persistentDataPath, "BlacksmithQuest"))
            .FilePath(service.State.userId, service.State.classId, assignment);
        Call("ShowModal", new object[] { null });
        var reward = (WorkshopForgeReward)Field("forgeReward");
        Assert.IsFalse(reward.StoneRevealed);
        foreach (int station in new[] { 2, 0, 1 })
        {
            Call("OpenStation", station);
            Call("AnswerStation", (station + 1) % 3);
            Assert.IsFalse(service.State.HasAnswer(station));
            Call("AnswerStation", station);
            Call("ShowModal", new object[] { null });
        }
        Assert.IsTrue(reward.StoneRevealed);
        Assert.IsFalse(reward.PortalOpened);
        MovePlayer(new Vector3(0, .05f, .2f));
        Call("ShowModal", Field("pausePanel"));
        Call("CheckPortalContact");
        Assert.IsFalse(service.State.portalOpened, "Pausing must prevent contact activation.");
        Call("ShowModal", new object[] { null });
        Call("CheckPortalContact");
        Assert.IsTrue(service.State.portalOpened);
        Assert.IsTrue(reward.PortalOpened);
        Assert.That(service.State.verifiedXp, Is.Zero);
        Assert.IsFalse(service.State.turnedIn);

        yield return SceneManager.LoadSceneAsync("BlacksmithRoom");
        yield return null;
        room = UnityEngine.Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        service = room.GetComponent<BlacksmithQuestService>();
        service.Initialize(null, assignment);
        reward = (WorkshopForgeReward)Field("forgeReward");
        Assert.IsTrue(service.State.portalOpened);
        Assert.IsTrue(reward.PortalOpened, "Opening the portal must survive a scene reload.");
        Assert.That(service.State.CompletedCount, Is.EqualTo(3));
        Call("ShowModal", new object[] { null });
        float deadline = Time.realtimeSinceStartup + 20;
        while (!reward.VideoFrameReceived && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(reward.VideoFrameReceived, reward.MediaStatus);
        MovePlayer(new Vector3(0, .05f, -.9f));
        var actor = (CharacterController)Field("player");
        actor.transform.rotation = Quaternion.Euler(0, 180, 0);
        yield return new WaitForSecondsRealtime(1);
        yield return new WaitForEndOfFrame();
        var frame = ScreenCapture.CaptureScreenshotAsTexture();
        PortalTestEvidence.Write("ClientPortal-InWorkshop.png", frame.EncodeToPNG());
        UnityEngine.Object.Destroy(frame);
        Call("CheckPortalContact");
        Assert.IsFalse(service.State.turnedIn, "A distant avatar cannot finish the quest.");
        MovePlayer(new Vector3(0, .05f, -4.1f));
        Call("CheckPortalContact");
        Assert.IsTrue(service.State.turnedIn);
        deadline = Time.realtimeSinceStartup + 20;
        while (SceneManager.GetActiveScene().name != "QuestArena" && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("QuestArena"));
        var restored = new BlacksmithQuestStore(Path.Combine(Application.persistentDataPath, "BlacksmithQuest"))
            .Load(service.State.userId, service.State.classId, assignment);
        Assert.IsTrue(restored.turnedIn);
        Assert.IsTrue(restored.portalOpened);
        Assert.That(restored.verifiedXp, Is.Zero);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (checkpoint != null && File.Exists(checkpoint)) File.Delete(checkpoint);
        yield return null;
    }
}

internal static class PortalTestEvidence
{
    public static void Write(string name, byte[] bytes)
    {
        string output = Environment.GetEnvironmentVariable("IMAGINEQUEST_TEST_OUTPUT");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, name), bytes);
    }
}
