using System.Collections;
using System.Reflection;
using ImagineQuest.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;

public sealed class WorkshopForgeRewardTests
{
    private GameObject root;
    private WorkshopForgeReward reward;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Reward test");
        reward = root.AddComponent<WorkshopForgeReward>();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(root);
        yield return null;
    }

    [Test]
    public void WrongAndRepeatedAnswersDoNotUnlockReward()
    {
        // Keep the bootstrap inactive so its world/Firebase-independent answer logic can
        // be exercised without constructing the entire workshop and capturing the cursor.
        var host = new GameObject("Inactive workshop");
        host.transform.SetParent(root.transform);
        host.SetActive(false);
        var workshop = host.AddComponent<BlacksmithRoomBootstrap>();
        var feedback = new GameObject("Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedback.transform.SetParent(host.transform);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = typeof(BlacksmithRoomBootstrap);
        type.GetField("forgeReward", flags).SetValue(workshop, reward);
        type.GetField("practiceFeedback", flags).SetValue(workshop, feedback.GetComponent<TMP_Text>());
        type.GetField("stationProgress", flags).SetValue(workshop, feedback.GetComponent<TMP_Text>());
        var answer = type.GetMethod("AnswerStation", flags);
        var index = type.GetField("practiceIndex", flags);
        index.SetValue(workshop, 2);
        answer.Invoke(workshop, new object[] { 0 });
        Assert.IsFalse(reward.StoneRevealed);
        answer.Invoke(workshop, new object[] { 2 });
        answer.Invoke(workshop, new object[] { 2 });
        index.SetValue(workshop, 0);
        answer.Invoke(workshop, new object[] { 0 });
        Assert.IsFalse(reward.StoneRevealed, "Two distinct answers are not enough.");
        index.SetValue(workshop, 1);
        answer.Invoke(workshop, new object[] { 1 });
        Assert.IsTrue(reward.StoneRevealed, "All stations may be answered in any order.");
        Assert.IsFalse(reward.PortalOpened, "Answering alone must not open the portal.");
    }

    [Test]
    public void StoneRequiresAllThreeStationsAndOnlySpawnsOnce()
    {
        for (var count = 0; count < 3; count++)
        {
            reward.SetCompletedStations(count);
            reward.CheckPlayerProximity(new Vector3(0f, 0f, 0.2f));
            Assert.IsFalse(reward.StoneRevealed);
            Assert.IsFalse(reward.PortalOpened);
        }
        reward.SetCompletedStations(3);
        reward.SetCompletedStations(3);
        Assert.IsTrue(reward.StoneRevealed);
        Assert.IsFalse(reward.PortalOpened);
        Assert.AreEqual(1, root.transform.childCount);
    }

    [UnityTest]
    public IEnumerator WalkingToStoneOpensOnePortalAndConsumesStone()
    {
        reward.SetCompletedStations(3);
        reward.CheckPlayerProximity(new Vector3(0f, 0f, 3f));
        Assert.IsFalse(reward.PortalOpened, "A distant learner cannot activate the stone.");
        reward.CheckPlayerProximity(new Vector3(0f, 3f, 0.2f));
        Assert.IsFalse(reward.PortalOpened, "A learner on another level cannot activate the stone.");
        reward.CheckPlayerProximity(new Vector3(0f, 0.05f, 0.9f));
        Assert.IsTrue(reward.PortalOpened);
        Assert.IsFalse(root.transform.Find("Forge Stone Reward").gameObject.activeSelf);
        reward.CheckPlayerProximity(new Vector3(0f, 0f, 0.2f));
        reward.SetCompletedStations(3);
        Assert.AreEqual(2, root.transform.childCount, "Repeated updates must not duplicate the reward.");
        yield return new WaitForSeconds(1f);
        var portal = root.transform.Find("Living Emerald Portal");
        Assert.Greater(portal.localScale.x, 0.99f);
        Assert.IsNotNull(portal.Find("Swirling emerald gateway"), "Shader must be included in builds.");
    }
}
