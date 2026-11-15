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
    [TestCase(null)] [TestCase("")] [TestCase("hero")] [TestCase("pirate-v2:0:0:0:0")]
    [TestCase("pirate-v1:-1:0:0:0")] [TestCase("pirate-v1:2:0:0:0")]
    [TestCase("pirate-v1:0:6:0:0")] [TestCase("pirate-v1:0:0:4:0")]
    [TestCase("pirate-v1:0:0:0:6")] [TestCase("pirate-v1:0:0:0:0:1")]
    [TestCase("pirate-v1:999999999999:0:0:0")]
    public void LegacyOrMalformedProfilesGetASafeClothedDefault(string id)
    {
        Assert.IsFalse(StudentAppearance.TryParse(id,out _));Assert.AreEqual(StudentAppearance.Default,StudentAppearance.FromAvatarId(id));
    }
}
