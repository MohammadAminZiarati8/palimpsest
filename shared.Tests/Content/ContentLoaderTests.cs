using Game.Shared.Content;

namespace Game.Shared.Tests.Content;

public class ContentLoaderTests
{
    [Fact]
    public void RealContent_Loads()
    {
        var db = TestContent.Db;
        Assert.True(db.Version >= 1);
        Assert.Equal(["c01", "c02", "c03", "c04"], db.Champions.Keys.Order());
        Assert.Equal(HeroClass.Warrior, db.Champion("c01").Class);
        Assert.Equal(5, db.Champion("c04").TweakPercent.Hp);
    }

    [Fact]
    public void UnknownChampion_Throws() =>
        Assert.Throws<ContentException>(() => TestContent.Db.Champion("nope"));

    [Theory]
    [InlineData("champions", "\"class\": \"Mage\"", "\"class\": \"Ranger\"", "class Ranger has no content")]
    [InlineData("champions", "\"hp\": 5", "\"hp\": 15", "beyond +-10")]
    [InlineData("champions", "\"id\": \"c02\"", "\"id\": \"c01\"", "duplicate champion id 'c01'")]
    [InlineData("champions", "\"critDamage\": \"Medium\", \"accuracy\": \"Medium\"", "\"critDamage\": \"Medium\", \"accuracy\": \"Unset\"", "missing shape.Accuracy")]
    [InlineData("stats", "\"Legendary\": 130", "\"Mythic\": 130", "stats.json")]
    [InlineData("classes", "\"growthPercent\"", "\"growthPrecent\"", "classes.json")]
    public void BadContent_FailsWithClearMessage(string file, string find, string replace, string expected)
    {
        var source = new TestContent.Patched(file, json =>
        {
            Assert.Contains(find, json); // guard: the patch must actually apply
            return json.Replace(find, replace);
        });

        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(source));
        Assert.Contains(expected, e.Message);
    }
}
