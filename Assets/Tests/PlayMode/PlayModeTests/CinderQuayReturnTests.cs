using System.Collections;
using System.Reflection;
using ImagineQuest.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class CinderQuayReturnTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator PauseButtonReturnsToTheShip() => ReturnFrom("pausePanel");

    [UnityTest]
    public IEnumerator ResultButtonReturnsToTheShip() => ReturnFrom("raidResult");

    private static IEnumerator ReturnFrom(string panelName)
    {
        if (QuestSession.Instance != null) QuestSession.Instance.Clear();
        yield return SceneManager.LoadSceneAsync("BlacksmithRoom");
        var room = Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        Assert.That(room, Is.Not.Null);
        Assert.That(room.GetComponent<BlacksmithQuestService>(), Is.Null, "Practice does not have an assigned quest service.");
        yield return new WaitForSecondsRealtime(2.8f);
        // Allow Firebase's initial auth event to settle before populating a
        // synthetic classroom context in this signed-out test.
        QuestSession.Begin("celestial-clock", "test-class");
        yield return null;
        var session = QuestSession.Begin("celestial-clock", "test-class");

        var panel = (GameObject)typeof(BlacksmithRoomBootstrap).GetField(panelName, Private).GetValue(room);
        typeof(BlacksmithRoomBootstrap).GetMethod("ShowModal", Private).Invoke(room, new object[] { panel });
        var button = System.Array.Find(panel.GetComponentsInChildren<Button>(),
            candidate => candidate.GetComponentInChildren<TMP_Text>().text == "Back to the ship");
        Assert.That(button, Is.Not.Null);
        button.onClick.Invoke();
        button.onClick.Invoke(); // A repeated submit must not restart or duplicate the transition.

        var fade = (CanvasGroup)typeof(BlacksmithRoomBootstrap).GetField("raidDepartureFade", Private).GetValue(room);
        Assert.That(fade.gameObject.activeInHierarchy, Is.True);
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(fade.alpha, Is.GreaterThan(0f));

        float deadline = Time.realtimeSinceStartup + 10;
        while (SceneManager.GetActiveScene().name != "QuestArena" && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("QuestArena"));
        Assert.That(QuestSession.Instance, Is.SameAs(session));
        Assert.That(session.ClassId, Is.EqualTo("test-class"));
        yield return new WaitForSecondsRealtime(2.8f);
    }

    [TearDown]
    public void ClearSession()
    {
        if (QuestSession.Instance != null) QuestSession.Instance.Clear();
    }
}
