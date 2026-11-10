using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImagineQuest.Gameplay;
using ImagineQuest.Student;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StudentStatusViewTests
{
    [UnityTest]
    public IEnumerator WorkshopHudFitsBetweenMissionAndPortalAtSupportedAspectRatios()
    {
        yield return SceneManager.LoadSceneAsync("BlacksmithRoom"); yield return null;
        var room = UnityEngine.Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        typeof(BlacksmithRoomBootstrap).GetMethod("ShowModal", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(room, new object[] {null});
        var canvas = GameObject.Find("Workshop UI");
        var widget = (RectTransform)canvas.transform.Find("Live Hearts and XP");
        var mission = (RectTransform)canvas.transform.Find("Mission HUD");
        var key = (RectTransform)canvas.transform.Find("Key HUD");
        var preview = new StudentStatusSnapshot(new Dictionary<string, object> {{"xp",175L}}, new Dictionary<string, object> {{"hearts",49L}}, false);
        canvas.GetComponent<StudentStatusView>().Render(preview,"Live • progress saved",0,new List<StudentActivity>(),"");
        string folder = Environment.GetEnvironmentVariable("IMAGINEQUEST_TEST_OUTPUT") ?? "Temp";
        foreach (var size in new[] {new Vector2Int(1600,900),new Vector2Int(1280,720),new Vector2Int(1024,768)})
        {
            Screen.SetResolution(size.x,size.y,false); yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            var statusCorners = new Vector3[4]; var missionCorners = new Vector3[4]; var keyCorners = new Vector3[4];
            widget.GetWorldCorners(statusCorners); mission.GetWorldCorners(missionCorners); key.GetWorldCorners(keyCorners);
            Assert.GreaterOrEqual(statusCorners[0].x,missionCorners[2].x,"Status must not overlap the mission card.");
            Assert.LessOrEqual(statusCorners[2].x,keyCorners[0].x,"Status must not overlap the portal card.");
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Workshop-Hud-"+size.x+"x"+size.y+".png"));
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator TeacherStandingProvidesAReasonAndShowsResetConsequences()
    {
        Screen.SetResolution(1600, 900, false);
        var root = new GameObject("Teacher UI test", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
        var panel = root.AddComponent<TeacherStandingPanel>();
        yield return null;
        var overlay = root.transform.Find("Teacher standing"); overlay.gameObject.SetActive(true);
        var card = overlay.Find("Standing card");
        card.Find("Selected student").GetComponent<TMP_Text>().text = "Demo student";
        card.Find("Student totals").GetComponent<TMP_Text>().text = "49 / 50 Hearts    ·    175 XP";
        var fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var roster = (List<(string id, string name)>)typeof(TeacherStandingPanel).GetField("students", fields).GetValue(panel);
        roster.Add(("preview-student", "Demo student"));
        typeof(TeacherStandingPanel).GetMethod("RenderRoster", fields).Invoke(panel, null);
        typeof(TeacherStandingPanel).GetField("workshopOpen", fields).SetValue(panel, true);
        typeof(TeacherStandingPanel).GetMethod("RenderAssignment", fields).Invoke(panel, null);
        typeof(TeacherStandingPanel).GetField("loaded", fields).SetValue(panel, true);
        typeof(TeacherStandingPanel).GetField("hearts", fields).SetValue(panel, 49);
        card.Find("Reason field").GetComponent<TMP_InputField>().text = "Late arrival";
        Assert.IsTrue(card.Find("Save Hearts").GetComponent<Button>().interactable);
        typeof(TeacherStandingPanel).GetField("hearts", fields).SetValue(panel, 1);
        typeof(TeacherStandingPanel).GetMethod("RenderChange", fields).Invoke(panel, null);
        Assert.That(card.Find("Teacher feedback").GetComponent<TMP_Text>().text, Does.Contain("XP, Crystals and Gold reset"));
        typeof(TeacherStandingPanel).GetField("hearts", fields).SetValue(panel, 49);
        typeof(TeacherStandingPanel).GetMethod("RenderChange", fields).Invoke(panel, null);
        string folder = Environment.GetEnvironmentVariable("IMAGINEQUEST_TEST_OUTPUT") ?? "Temp";
        Directory.CreateDirectory(folder);
        yield return null; yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, "Teacher-Hearts-1600x900.png"));
        yield return null;
        UnityEngine.Object.Destroy(root); yield return null;
    }

    [UnityTest]
    public IEnumerator RecapRendersBalancesActivityOfflineStateAndSmallScreens()
    {
        var root = new GameObject("Recap UI test", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
        var view = StudentStatusView.Attach(root.transform, true, false);
        var snapshot = new StudentStatusSnapshot(new Dictionary<string, object> { { "xp", 175L }, { "level", 1L } },
            new Dictionary<string, object> { { "hearts", 49L } }, false);
        var entries = new List<StudentActivity> {
            new StudentActivity {Title="Repair kit delivered", Detail="The Aurora's Repair", Xp=100, Time=DateTime.UtcNow},
            new StudentActivity {Title="Classroom standing updated", Detail="Late arrival", Hearts=-1, Time=DateTime.UtcNow},
            new StudentActivity {Title="Forge craft mastered", Detail="The Aurora's Repair", Xp=25, Time=DateTime.UtcNow},
        };
        view.Render(snapshot, "Live • progress saved", 175, entries, "Your six most recent updates");
        view.OpenRecap();
        Assert.IsTrue(view.IsOpen);
        string folder = Environment.GetEnvironmentVariable("IMAGINEQUEST_TEST_OUTPUT") ?? "Temp";
        Directory.CreateDirectory(folder);
        foreach (var size in new[] { new Vector2Int(1600,900), new Vector2Int(1280,720), new Vector2Int(1024,768) })
        {
            Screen.SetResolution(size.x, size.y, false);
            yield return null; yield return null; yield return new WaitForEndOfFrame();
            if (!Application.isEditor) Assert.That(new Vector2Int(Screen.width, Screen.height), Is.EqualTo(size));
            Canvas.ForceUpdateCanvases();
            foreach (var icon in root.GetComponentsInChildren<StatusGlyph>())
                Assert.IsNotNull(icon.GetComponent<CanvasRenderer>(), "Heart and XP icons need renderers.");
            Assert.That(root.transform.Find("Personal recap/Recap card/Classroom standing/Heart total").GetComponent<TMP_Text>().text, Is.EqualTo("49 / 50"));
            Assert.That(root.transform.Find("Personal recap/Recap card/Experience/Experience total"), Is.Not.Null);
            foreach (var label in root.GetComponentsInChildren<TMP_Text>())
            {
                if (!label.gameObject.activeInHierarchy || string.IsNullOrEmpty(label.text)) continue;
                label.ForceMeshUpdate();
                // Every visible label must have a usable rectangle at all supported aspect ratios.
                Assert.That(label.rectTransform.rect.width, Is.GreaterThan(15), label.name);
                Assert.That(label.rectTransform.rect.height, Is.GreaterThan(5), label.name);
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "PersonalRecap-" + size.x + "x" + size.y + ".png"));
        }
        view.Render(null, "Sign in to connect your progress.", 0, new List<StudentActivity>(), "");
        Assert.AreEqual("—", root.transform.Find("Personal recap/Recap card/Classroom standing/Heart total").GetComponent<TMP_Text>().text);
        view.CloseRecap(); Assert.IsFalse(view.IsOpen);
        UnityEngine.Object.Destroy(root); yield return null;
    }
}
