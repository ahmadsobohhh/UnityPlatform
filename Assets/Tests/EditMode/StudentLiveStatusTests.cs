using System;
using System.Collections.Generic;
using ImagineQuest.Student;
using NUnit.Framework;

public class StudentLiveStatusTests
{
    [Test]
    public void PendingReleaseGateRejectsCloudCallsBeforeAuthenticationOrNetwork()
    {
        Assert.IsFalse(StudentCloudConfig.Enabled, "Leave the release gate off while production deployment and billing are pending.");
        Assert.AreEqual("capstone-d51c2", StudentCloudConfig.ProjectId);
        var error = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await GameCloudClient.CallAsync<GameCloudClient.Empty, GameCloudClient.Acknowledgement>("ensureStudentStatus", new GameCloudClient.Empty()));
        Assert.That(error.Message, Does.Contain("not enabled"));
    }
    [TestCase(50L, 0L)]
    [TestCase(0L, 175L)]
    [TestCase(1L, 9223372036854775807L)]
    public void PreservesAuthoritativeBalancesIncludingZeroAndLargeXp(long hearts, long xp)
    {
        var data = new StudentStatusSnapshot(new Dictionary<string, object> { { "xp", xp }, { "level", 1L }, { "epoch", 2L } },
            new Dictionary<string, object> { { "hearts", hearts } }, false);
        Assert.AreEqual(hearts, data.Hearts); Assert.AreEqual(xp, data.Xp); Assert.AreEqual(2, data.Epoch); Assert.IsFalse(data.IsCached);
    }
    [TestCase(-1L)]
    [TestCase(51L)]
    public void RejectsInvalidStandingInsteadOfShowingFakeHearts(long hearts)
    {
        Assert.Throws<InvalidOperationException>(() => new StudentStatusSnapshot(new Dictionary<string, object>(),
            new Dictionary<string, object> { { "hearts", hearts } }, false));
    }
    [Test]
    public void MissingSnapshotsAndMalformedWalletsAreNotFreshStudents()
    {
        Assert.Throws<InvalidOperationException>(() => new StudentStatusSnapshot(null, new Dictionary<string, object>(), true));
        Assert.Throws<InvalidOperationException>(() => new StudentStatusSnapshot(new Dictionary<string, object> { { "xp", "175" } },
            new Dictionary<string, object>(), false));
    }
    [Test]
    public void CachedStatusIsExplicitlyDistinguishableFromLiveData()
    {
        Assert.IsTrue(new StudentStatusSnapshot(new Dictionary<string, object> { { "xp", 45L } },
            new Dictionary<string, object> { { "hearts", 20L } }, true).IsCached);
    }
}
