using Game.Shared.Champions;
using Game.Shared.Content;

namespace Game.Shared.Tests.Champions;

public class StatCalculatorTests
{
    private static StatSet<int> Lv1(string id) => StatCalculator.Level1(TestContent.Db, TestContent.Db.Champion(id));
    private static StatSet<int> At(string id, int level, int rank) =>
        StatCalculator.At(TestContent.Db, TestContent.Db.Champion(id), level, rank);

    // BAL-5..7: "Resulting Lv1 stats" table in balancing.md (Accuracy 10 for all).
    [Theory]
    [InlineData("c01", 1495, 144, 58, 100, 5, 50)]
    [InlineData("c02", 800, 125, 35, 108, 20, 70)]
    [InlineData("c03", 800, 125, 35, 108, 10, 50)]
    [InlineData("c04", 1570, 92, 58, 108, 5, 40)]
    public void Level1_MatchesBalancingTable(string id, int hp, int atk, int def, int spd, int crit, int critDmg)
    {
        var s = Lv1(id);
        Assert.Equal((hp, atk, def, spd, crit, critDmg, 10),
            (s.Hp, s.Attack, s.Defense, s.Speed, s.CritChance, s.CritDamage, s.Accuracy));
    }

    [Fact]
    public void Level1_EqualsAtLevel1Rank1()
    {
        var a = Lv1("c01");
        var b = At("c01", 1, 1);
        foreach (var stat in Stats.All)
            Assert.Equal(a[stat], b[stat]);
    }

    // BAL-8 example: "Margo Atk Lv10 ≈ 261".
    [Fact]
    public void Margo_Lv10_Attack_Is261() => Assert.Equal(261, At("c01", 10, 1).Attack);

    // BAL-8: 144 x (1 + 19 x 9%) = 390.24; BAL-9: x 1.10 = 429.26 -> 429.
    [Fact]
    public void Margo_Lv20_Rank2_Attack() => Assert.Equal(429, At("c01", 20, 2).Attack);

    // BAL-8: Speed, Crit and Accuracy don't grow per level.
    [Fact]
    public void NonCoreStats_DoNotGrowWithLevel()
    {
        var lv1 = Lv1("c02");
        var lv10 = At("c02", 10, 1);
        Assert.Equal((lv1.Speed, lv1.CritChance, lv1.CritDamage, lv1.Accuracy),
            (lv10.Speed, lv10.CritChance, lv10.CritDamage, lv10.Accuracy));
        Assert.True(lv10.Hp > lv1.Hp);
    }

    // BAL-9 class bonus per rank-up.
    [Theory]
    [InlineData("c01", Stat.CritDamage, 55)]  // Warrior +5% Crit Dmg
    [InlineData("c02", Stat.CritChance, 22)]  // Assassin +2% Crit Chance
    [InlineData("c03", Stat.Speed, 110)]      // Mage +2 Spd
    [InlineData("c04", Stat.Speed, 110)]      // Support +2 Spd
    public void Rank2_AddsClassBonus(string id, Stat stat, int expected) =>
        Assert.Equal(expected, At(id, 11, 2)[stat]);

    // CHM-7, BAL-10: Rank 1 max Lv10; demo max Rank 2.
    [Theory]
    [InlineData(11, 1)]
    [InlineData(0, 1)]
    [InlineData(21, 2)]
    [InlineData(1, 3)]
    [InlineData(1, 0)]
    public void OutOfCapLevelOrRank_Throws(int level, int rank) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => At("c01", level, rank));
}
