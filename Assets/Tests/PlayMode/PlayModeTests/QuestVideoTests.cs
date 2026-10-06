using System.Collections;
using ImagineQuest.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class QuestVideoTests
{
    [Test]
    public void EmptyUrlContinuesImmediatelyWithoutOverlay()
    {
        var host = new GameObject("Video test");
        try
        {
            var player = host.AddComponent<UrlVideoSequencePlayer>();
            int calls = 0;
            player.Play("", () => calls++);
            Assert.AreEqual(1, calls);
            Assert.IsFalse(player.IsPlaying);
            Assert.AreEqual(0, host.transform.childCount);
        }
        finally { Object.DestroyImmediate(host); }
    }

    [UnityTest]
    public IEnumerator SkipResumesOnceAndDisablingDoesNotResume()
    {
        var host = new GameObject("Video test");
        var player = host.AddComponent<UrlVideoSequencePlayer>();
        int calls = 0;
        player.Play("https://example.invalid/placeholder.mp4", () => calls++);
        player.Skip();
        player.Skip();
        Assert.AreEqual(1, calls);
        Assert.IsFalse(player.IsPlaying);
        player.Play("https://example.invalid/placeholder.mp4", () => calls++);
        host.SetActive(false);
        Assert.AreEqual(1, calls);
        Assert.IsFalse(player.IsPlaying);
        Object.Destroy(host);
        yield return null;
    }
}
