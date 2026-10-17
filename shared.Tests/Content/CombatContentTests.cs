using Game.Shared.Content;

namespace Game.Shared.Tests.Content;

/// <summary>M2a-T1: combat config, hero kits, enemies load and are checked.</summary>
public class CombatContentTests
{
    private static ContentDb Db => TestContent.Db;

    [Fact]
    public void CombatConfig_Loads()
    {
        var c = Db.Combat;
        Assert.Equal((100, 5, 25, 100, 50), (c.DefenseK, c.SpreadPercent, c.ExposedCritPercent, c.CritCapPercent, c.RoundCap));
        var e = c.Effects;
        Assert.Equal((30, 25, 30, 15, 20, 2), (e.BleedAttackPercent, e.AttackPercent, e.DefensePercent, e.Speed, e.Accuracy, e.WardRounds));
    }

    [Fact]
    public void EveryChampion_HasS1ToS3_WithBalancingCooldowns()
    {
        foreach (var id in Db.Champions.Keys)
        {
            var skills = Db.Kit(id).Skills;
            Assert.Equal([SkillSlot.S1, SkillSlot.S2, SkillSlot.S3], skills.Select(s => s.Slot));
            Assert.Equal([0, 3, 4], skills.Select(s => s.Cooldown));
        }
    }

    [Fact]
    public void HeroKits_MatchBalancingTable()
    {
        var margoS3 = Db.Kit("c01").Skills[2];
        Assert.True(margoS3.Dive);
        Assert.Equal(200, margoS3.DamagePercent);
        Assert.Equal((StatusEffect.DefenseDown, 1, EffectOn.Self), (margoS3.Effects[0].Type, margoS3.Effects[0].Rounds, margoS3.Effects[0].On));

        var pell = Db.Kit("c02").Skills;
        Assert.Equal(15, pell[0].CritChanceBonus);
        Assert.Equal((SpecialType.Fade, 1), (pell[2].Special!.Type, pell[2].Special!.Rounds));
        Assert.Equal(0, pell[2].DamagePercent);

        Assert.Equal(Area.Row, Db.Kit("c03").Skills[1].Area);
        Assert.Equal([StatusEffect.Stun, StatusEffect.SpeedDown], Db.Kit("c03").Skills[2].Effects.Select(e => e.Type));

        var snake = Db.Kit("c04").Skills;
        Assert.Equal((20, HealBasis.Caster, 1), (snake[1].HealPercent, snake[1].HealOf, snake[1].Cleanse));
        Assert.Equal((Area.AllAllies, 15), (snake[2].Area, snake[2].WardPercent));
    }

    [Fact]
    public void Enemies_MatchBal18AndCmb41()
    {
        Assert.Equal(["C1", "C2", "C4", "C5"], Db.Enemies.Keys.Order());
        Assert.Equal(AiWhen.SelfDamaged, Db.Enemy("C1").Skills[1].Ai!.When);
        var fluid = Db.Enemy("C4").Skills[1];
        Assert.Equal((AiWhen.AllyHpAtOrBelow, 75, HealBasis.Target, 25, 2),
            (fluid.Ai!.When, fluid.Ai.Percent, fluid.HealOf, fluid.HealPercent, fluid.Cooldown));
        Assert.True(Db.Enemy("C5").Elite);
        Assert.Equal(Area.Splash, Db.Enemy("C2").Skills[1].Area);
    }

    [Fact]
    public void UnknownEnemy_Throws() =>
        Assert.Throws<ContentException>(() => Db.Enemy("C3"));

    [Theory]
    [InlineData("combat", "\"roundCap\": 50", "\"roundCap\": 0", "roundCap must be >= 1")]
    [InlineData("combat", "\"spreadPercent\": 5", "\"spreadPercent\": 80", "spreadPercent 80 outside 0..50")]
    [InlineData("skills", "\"champion\": \"c04\"", "\"champion\": \"c09\"", "kit 'c09': unknown champion")]
    [InlineData("skills", "\"champion\": \"c04\"", "\"champion\": \"c09\"", "champion 'c04': missing kit")]
    [InlineData("skills", "\"slot\": \"S3\", \"name\": \"Fade\"", "\"slot\": \"S2\", \"name\": \"Fade\"", "kit 'c02': missing skill S3")]
    [InlineData("skills", "\"damagePercent\": 90, \"cooldown\": 3", "\"damagePercent\": 90, \"cooldown\": 0", "kit 'c01' S2: cooldown 0 outside 1..50")]
    [InlineData("skills", "\"critChanceBonus\": 15", "\"critChanceBonus\": 150", "critChanceBonus 150 outside 0..100")]
    [InlineData("skills", "\"healOf\": \"Caster\", ", "", "heal needs healOf")]
    [InlineData("skills", "\"area\": \"Row\"", "\"area\": \"Column\"", "skills.json")]
    [InlineData("skills", "\"reach\": \"Ranged\", \"area\": \"Row\"", "\"reach\": \"None\", \"area\": \"Row\"", "reach None doesn't fit area Row")]
    [InlineData("enemies", "\"speed\": \"Low\" }", "\"speed\": \"High\" }", "enemy 'C1': shape.Speed High has no tier value")]
    [InlineData("enemies", "\"id\": \"C2\"", "\"id\": \"C1\"", "duplicate enemy id 'C1'")]
    [InlineData("enemies", "\"cooldown\": 2,", "\"cooldown\": 0,", "enemy 'C4' S2: cooldown 0 outside 1..50")]
    [InlineData("enemies", "\"percent\": 75", "\"percent\": 100", "ai.percent 100 outside 1..99")]
    [InlineData("enemies", "\"growthPercent\": 9", "\"growthPrecent\": 9", "enemies.json")]
    public void BadCombatContent_FailsWithClearMessage(string file, string find, string replace, string expected)
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
