using System.Collections;
using System.Linq;
using System.Reflection;
using ImagineQuest.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class QuestArenaArrivalTests
{
    [UnityTest]
    public IEnumerator ArrivalClearsAndCrewWalksWithoutOfferingHelmFromSpawn()
    {
        if (QuestSession.Instance != null) QuestSession.Instance.Clear();
        yield return SceneManager.LoadSceneAsync("QuestArena");
        yield return null;
        var quest = Object.FindFirstObjectByType<CelestialClockQuestBootstrap>();
        Assert.That(quest.transform.Find("THE AURORA arrival"), Is.Not.Null);
        var player = Object.FindFirstObjectByType<CelestialClockPlayerController>();
        var helm = Object.FindObjectsByType<QuestArenaShipInteractable>(FindObjectsSortMode.None)
            .Single(fixture => fixture.InteractionPrompt == "TAKE THE HELM");
        var crew = Object.FindObjectsByType<AuroraCrewRoutine>(FindObjectsSortMode.None);
        var positions = crew.Select(member => member.transform.position).ToArray();
        Assert.That(crew.Length, Is.GreaterThanOrEqualTo(3));
        Assert.That(QuestArenaInteractionRegistry.FindNearest(player.transform.position + Vector3.up * .68f,
            1.65f, player.transform), Is.Not.SameAs(helm));
        Assert.That(QuestArenaInteractionRegistry.FindNearest(helm.transform.position, 1.65f), Is.SameAs(helm));
        var prompt = (TMP_Text)typeof(CelestialClockQuestBootstrap)
            .GetField("interactionText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(quest);
        Assert.That(prompt.gameObject.activeSelf, Is.False);
        yield return new WaitForSecondsRealtime(4.2f);
        Assert.That(quest.transform.Find("THE AURORA arrival"), Is.Null);
        Assert.That(crew.Where((member, i) => (member.transform.position - positions[i]).sqrMagnitude > .04f).Any(),
            Is.True, "At least one crew member must make real grounded progress along their route.");
    }
}
