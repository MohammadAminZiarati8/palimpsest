using Game.Shared.Content;
using Game.Shared.Enemies;

namespace Game.Shared.Tests.Enemies;

public class EnemyStatCalculatorTests
{
    private static StatSet<int> At(string id, int level) =>
        EnemyStatCalculator.At(TestContent.Db, TestContent.Db.Enemy(id), level);

    // BAL-16 tiers x BAL-17 linear growth (+9%/level, D-81) x Elite HP x3 / Atk x1.5; Speed fixed.
    [Theory]
    [InlineData("C2", 1, 250, 100, 20, 99)]
    [InlineData("C2", 4, 318, 127, 25, 99)]   // 250x1.27=317.5 · 20x1.27=25.4
    [InlineData("C1", 1, 600, 100, 35, 92)]
    [InlineData("C4", 14, 868, 152, 76, 99)]  // x2.17: 70 -> 151.9 · 35 -> 75.95
    [InlineData("C5", 1, 1800, 195, 35, 99)]
    [InlineData("C5", 10, 3258, 353, 63, 99)] // 600x1.81x3 · 130x1.81x1.5=352.95 · 35x1.81=63.35
    public void At_MatchesBal16And17(string id, int level, int hp, int atk, int def, int spd)
    {
        var s = At(id, level);
        Assert.Equal((hp, atk, def, spd), (s.Hp, s.Attack, s.Defense, s.Speed));
    }

    [Fact]
    public void Crit_UsesDefaults_D76()
    {
        var s = At("C1", 5);
        Assert.Equal((5, 50, 0), (s.CritChance, s.CritDamage, s.Accuracy));
    }

    [Fact]
    public void Crit_PerEnemyOverride_D76()
    {
        var e = TestContent.Db.Enemy("C2");
        var copy = new EnemyDef { Id = e.Id, Shape = e.Shape, CritChance = 12, CritDamage = 80 };
        var s = EnemyStatCalculator.At(TestContent.Db, copy, 1);
        Assert.Equal((12, 80), (s.CritChance, s.CritDamage));
    }

    [Fact]
    public void LevelBelow1_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => At("C1", 0));
}
