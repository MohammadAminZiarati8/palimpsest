using Game.Shared.Champions;

namespace Game.Shared.Tests.Champions;

public class ProgressionTests
{
    // CHM-7
    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    public void MaxLevel_Is10PerRank(int rank, int expected) =>
        Assert.Equal(expected, Progression.MaxLevel(TestContent.Db, rank));

    // BAL-11: 100 x current level; Lv10 total 4,500; Lv15 total 10,500.
    [Theory]
    [InlineData(1, 100)]
    [InlineData(9, 900)]
    public void XpToNextLevel(int level, int expected) =>
        Assert.Equal(expected, Progression.XpToNextLevel(TestContent.Db, level));

    [Theory]
    [InlineData(1, 0)]
    [InlineData(10, 4500)]
    [InlineData(15, 10500)]
    public void TotalXpToReach_MatchesBalancing(int level, int expected) =>
        Assert.Equal(expected, Progression.TotalXpToReach(TestContent.Db, level));
}
