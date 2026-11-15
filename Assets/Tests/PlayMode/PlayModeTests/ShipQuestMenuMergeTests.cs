using System.Collections;
using System.Linq;
using System.Reflection;
using ImagineQuest.Gameplay;
using ImagineQuest.Student;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ShipQuestMenuMergeTests
{
    [UnityTest]
    public IEnumerator ChartKeepsMainRaidAndPreviewAlongsideRepairWorkshop()
    {
        QuestSession.Instance?.Clear();
        yield return SceneManager.LoadSceneAsync("QuestArena");
        yield return WaitForShip();
        var buttons = OpenChart();
        foreach (var name in new[] {"Play Cinder Quay", "Watch chart preview", "Play assigned workshop", "Close Quest Selection"})
            Assert.That(buttons.Count(button => button.name == name), Is.EqualTo(1), name);
        var routes = buttons.Where(button => new[] {"Play Cinder Quay", "Watch chart preview", "Play assigned workshop", "Close Quest Selection"}.Contains(button.name)).ToArray();
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < routes.Length; i++) for (int j = i + 1; j < routes.Length; j++)
            Assert.IsFalse(Bounds(routes[i]).Overlaps(Bounds(routes[j])), routes[i].name + " overlaps " + routes[j].name);

        buttons.Single(button => button.name == "Play assigned workshop").onClick.Invoke();
        yield return WaitForScene("BlacksmithRoom");
        var repair = Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        Assert.IsNotNull(repair.GetComponent<BlacksmithQuestService>(), "The repair route must retain saved craft progression.");
        Assert.IsNotNull(GameObject.Find("Workshop UI").GetComponent<StudentStatusView>());

        yield return SceneManager.LoadSceneAsync("QuestArena");
        yield return WaitForShip();
        OpenChart().Single(button => button.name == "Play Cinder Quay").onClick.Invoke();
        yield return WaitForScene("BlacksmithRoom");
        var raid = Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        Assert.IsNull(raid.GetComponent<BlacksmithQuestService>(), "Main's raid must stay independent of repair rewards.");
        Assert.IsNotNull(GameObject.Find("Cinder Quay UI"));
    }

    private static Button[] OpenChart()
    {
        var ship = Object.FindFirstObjectByType<CelestialClockQuestBootstrap>();
        typeof(CelestialClockQuestBootstrap).GetMethod("OpenQuestPicker", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ship, null);
        return GameObject.Find("Quest Arena Interface").GetComponentsInChildren<Button>(true);
    }
    private static IEnumerator WaitForShip()
    {
        yield return new WaitForSecondsRealtime(4.3f);
        Assert.IsFalse(SceneTransition.IsTransitioning);
        Assert.IsNotNull(Object.FindFirstObjectByType<CelestialClockQuestBootstrap>());
    }
    private static IEnumerator WaitForScene(string scene)
    {
        float deadline = Time.realtimeSinceStartup + 15;
        while ((SceneManager.GetActiveScene().name != scene || SceneTransition.IsTransitioning) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
        Assert.IsFalse(SceneTransition.IsTransitioning);
        yield return null;
    }
    private static Rect Bounds(Button button)
    {
        var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    [TearDown]
    public void ClearContext() => QuestSession.Instance?.Clear();
}
