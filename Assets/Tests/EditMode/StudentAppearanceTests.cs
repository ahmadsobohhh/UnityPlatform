using ImagineQuest.Characters;
using NUnit.Framework;

public sealed class StudentAppearanceTests
{
    [Test]
    public void EverySupportedAppearanceRoundTripsWithoutLosingChoices()
    {
        for(int body=0;body<2;body++) for(int skin=0;skin<6;skin++) for(int hair=0;hair<4;hair++) for(int color=0;color<6;color++)
        {
            var appearance=new StudentAppearance {Body=body,Skin=skin,Hair=hair,HairColor=color};
            Assert.IsTrue(StudentAppearance.TryParse(appearance.AvatarId,out var restored));Assert.AreEqual(appearance,restored);
        }
    }
    [TestCase(null)] [TestCase("")] [TestCase("hero")] [TestCase("pirate-v3:0:0:0:0")]
    [TestCase("pirate-v1:-1:0:0:0")] [TestCase("pirate-v1:2:0:0:0")]
    [TestCase("pirate-v1:0:6:0:0")] [TestCase("pirate-v1:0:0:4:0")]
    [TestCase("pirate-v1:0:0:0:6")] [TestCase("pirate-v1:0:0:0:0:1")]
    [TestCase("pirate-v1:999999999999:0:0:0")]
    [TestCase("pirate-v2:0:0:4:0")] [TestCase("pirate-v2:1:0:-1:0")]
    [TestCase("pirate-v2:0:0:0:6")]
    public void LegacyOrMalformedProfilesGetASafeClothedDefault(string id)
    {
        Assert.IsFalse(StudentAppearance.TryParse(id,out _));Assert.AreEqual(StudentAppearance.Default,StudentAppearance.FromAvatarId(id));
    }
    [Test]
    public void OldProfilesKeepTheirBodyAndCompatibleHairInsteadOfBecomingBald()
    {
        int[][] migrated={new[]{1,2,4,5},new[]{5,4,2,3}};
        for(int body=0;body<2;body++) for(int skin=0;skin<6;skin++) for(int hair=0;hair<4;hair++) for(int color=0;color<6;color++)
        {
            var old=StudentAppearance.FromAvatarId($"pirate-v1:{body}:{skin}:{hair}:{color}");
            Assert.AreEqual(body,old.Body);Assert.AreEqual(skin,old.Skin);Assert.AreEqual(color,old.HairColor);
            Assert.AreEqual(migrated[body][hair],old.Hair);Assert.AreEqual(old,StudentAppearance.FromAvatarId(old.AvatarId));
        }
        Assert.AreEqual(1,StudentAppearance.FromAvatarId("pirate-v1:0:2:0:1").Hair);
        Assert.AreEqual(2,StudentAppearance.FromAvatarId("pirate-v1:0:2:1:1").Hair);
        var girl=StudentAppearance.FromAvatarId("pirate-v1:1:2:2:1");Assert.AreEqual(1,girl.Body);Assert.AreEqual(2,girl.Hair);
        var longBoy=StudentAppearance.FromAvatarId("pirate-v1:0:2:2:1");Assert.AreEqual(4,longBoy.Hair);
        Assert.AreEqual(longBoy,StudentAppearance.FromAvatarId(longBoy.AvatarId));
        Assert.AreEqual(0,StudentAppearance.FromAvatarId("pirate-v2:1:2:0:1").Hair);
    }
}
