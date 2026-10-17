using Game.Shared.Combat;
using Game.Shared.Content;
using Game.Shared.Stages;

namespace Game.Shared.Tests.Stages;

public class RoomOptionTests
{
    private static readonly ContentDb Db = TestContent.Db;

    private static StageRun Run(string stage, string[] team, int level, ulong seed = 3) =>
        new(Db, Db.Stage(stage), team.Select(id => new HeroEntry { ChampionId = id, Level = level, Rank = level > 10 ? 2 : 1 }).ToList(), seed);

    private static void Idle(Fight f, FightUnit u) { }

    // --- CMB-7 Noise + requirements (D-99) ---

    [Fact]
    public void Noise_LimitsOptionsByPartySize_CMB7()
    {
        var r = Run("S1", ["c01", "c02", "c04"], 3); // S1 scene 1 is Reading
        Assert.Equal([RoomOption.Clash, RoomOption.MarginStrike, RoomOption.StrikeOut, RoomOption.BetweenTheLines],
            r.Allowed(["c02"])); // solo Assassin
        Assert.Equal([RoomOption.Clash, RoomOption.MarginStrike, RoomOption.SpinATale], r.Allowed(["c01", "c04"]));
        Assert.Equal([RoomOption.Clash, RoomOption.SpinATale], r.Allowed(["c01", "c02", "c04"]));
        Assert.DoesNotContain(RoomOption.StrikeOut, r.Allowed(["c01"])); // not an Assassin
    }

    [Fact]
    public void WatchingScene_NoStealthOptions_D99()
    {
        var r = Run("P1", ["c01", "c02"], 1); // Watching
        Assert.Equal([RoomOption.Clash], r.Allowed(["c02"]));
        Assert.Throws<ArgumentException>(() => r.Enter(["c02"], RoomOption.StrikeOut, 1));
    }

    // --- BAL-22 chance ---

    [Theory]
    [InlineData(new[] { "c02" }, RoomOption.StrikeOut, 3, 60)]          // 50 - 5 + 15
    [InlineData(new[] { "c01" }, RoomOption.BetweenTheLines, 3, 45)]    // no class bonus
    [InlineData(new[] { "c01", "c04" }, RoomOption.SpinATale, 3, 60)]   // avg 3, Support +15
    [InlineData(new[] { "c02" }, RoomOption.StrikeOut, 20, 95)]         // clamp
    [InlineData(new[] { "c01" }, RoomOption.BetweenTheLines, 1, 35)]
    [InlineData(new[] { "c01" }, RoomOption.Clash, 1, 100)]
    public void Chance_BAL22(string[] party, RoomOption option, int level, int expected) =>
        Assert.Equal(expected, Run("S1", ["c01", "c02", "c04"], level).Chance(option, party));

    [Fact]
    public void Chance_ClampsLow()
    {
        var r = new StageRun(Db, Db.Stage("S6"), [new HeroEntry { ChampionId = "c01", Level = 1 }], 1);
        Assert.Equal(5, r.Chance(RoomOption.BetweenTheLines, ["c01"])); // 50 - 65 -> 5
    }

    // --- outcomes ---

    [Fact]
    public void MarginStrike_HeroesActAloneFirst_WithCritBonus_BAL23()
    {
        var r = Run("S1", ["c01", "c02"], 3);
        var start = r.Enter(["c01", "c02"], RoomOption.MarginStrike);
        var f = start.Fight!;
        var acted = new List<FightUnit>();
        f.Step((_, u) => acted.Add(u));
        Assert.True(f.InOpening);
        Assert.Equal(0, f.Round);
        f.Step((_, u) => acted.Add(u));
        Assert.All(acted, u => Assert.Equal(Side.Heroes, u.Side));
        f.Step((_, u) => acted.Add(u));
        Assert.Equal(1, f.Round);
        Assert.False(f.InOpening);
    }

    [Fact]
    public void MarginStrike_OpeningCritsMoreOften()
    {
        int Crits(bool margin)
        {
            int total = 0;
            for (ulong seed = 1; seed <= 60; seed++)
            {
                var f = new Fight(Db, new FightSetup
                {
                    Heroes = { new HeroEntry { ChampionId = "c01" } },
                    Enemies = { new EnemyEntry { EnemyId = "C5" } },
                    Opening = margin ? Side.Heroes : null,
                    OpeningCritBonus = margin ? 50 : 0,
                    Seed = seed,
                });
                var h = f.Line(Side.Heroes)[0];
                h.Stats.Speed = 200;
                f.Step((fight, u) => total += fight.Attack(u, u.Skill(SkillSlot.S1), fight.Line(Side.Enemies)[0]).Count(x => x.Crit));
            }
            return total;
        }
        Assert.True(Crits(true) > Crits(false) + 15);
    }

    [Fact]
    public void BetweenTheLines_Success_PassesSceneWithoutFight()
    {
        StageRun? pass = null; // level 20 = 95%
        for (ulong seed = 1; pass == null; seed++)
        {
            var t = Run("S1", ["c02", "c01"], 20, seed);
            if (t.Enter(["c02"], RoomOption.BetweenTheLines).Success) pass = t;
        }
        Assert.Equal((StageState.ChooseParty, 1, (Fight?)null), (pass.State, pass.SceneIndex, pass.Fight));
    }

    [Fact]
    public void FailedStealth_EnemiesActFirst()
    {
        for (ulong seed = 1; seed < 200; seed++)
        {
            var r = new StageRun(Db, Db.Stage("S6"), [new HeroEntry { ChampionId = "c02", Level = 1 }], seed); // 5%
            var start = r.Enter(["c02"], RoomOption.BetweenTheLines);
            if (start.Success) continue;
            var f = start.Fight!;
            FightUnit? first = null;
            f.Step((_, u) => first = u);
            Assert.Equal((Side.Enemies, 0), (first!.Side, f.Round));
            return;
        }
        Assert.Fail("no failed roll found");
    }

    [Fact]
    public void StrikeOut_Success_RemovesTarget_LastOneClearsScene_BAL24()
    {
        for (ulong seed = 1; seed < 200; seed++)
        {
            var r = Run("S3", ["c02"], 20, seed); // S3 scene 1: one Inkslinger, Reading
            var start = r.Enter(["c02"], RoomOption.StrikeOut, 0);
            if (!start.Success) continue;
            Assert.Null(start.Fight);
            Assert.Equal(1, r.SceneIndex);
            return;
        }
        Assert.Fail("no successful roll found");
    }

    [Fact]
    public void StrikeOut_Failure_OnlyTargetActsFirst()
    {
        for (ulong seed = 1; seed < 400; seed++)
        {
            var r = new StageRun(Db, Db.Stage("S1"), [new HeroEntry { ChampionId = "c02", Level = 1 }], seed); // 50-15+15=50%
            var start = r.Enter(["c02"], RoomOption.StrikeOut, 1);
            if (start.Success) continue;
            var f = start.Fight!;
            var target = f.Line(Side.Enemies)[1];
            var opening = new List<FightUnit>();
            while (f.Round == 0 && !f.IsOver) f.Step((fight, u) => { if (fight.InOpening) opening.Add(u); });
            Assert.Equal([target], opening); // only the target strikes first
            return;
        }
        Assert.Fail("no failed roll found");
    }

    [Fact]
    public void StrikeOut_CantTargetElite_BAL24()
    {
        var r = new StageRun(Db, Db.Stage("S6"), [new HeroEntry { ChampionId = "c02", Level = 14, Rank = 2 }], 1);
        r.Enter(["c02"], RoomOption.BetweenTheLines); // pass or fight scene 1
        if (r.Fight != null) { r.Fight.Run((f, u) => { if (u.Side == Side.Heroes && f.Line(Side.Enemies).Count > 0) f.Damage(f.Line(Side.Enemies)[0], 1_000_000); }); r.EndScene(); }
        Assert.Equal(1, r.SceneIndex); // finale is Watching anyway: no Strike Out
        Assert.DoesNotContain(RoomOption.StrikeOut, r.Allowed(["c02"]));
    }

    [Fact]
    public void SpinATale_Success_EnemiesSkip_Failure_EnemiesFirst()
    {
        bool sawSuccess = false, sawFail = false;
        for (ulong seed = 1; seed < 100 && !(sawSuccess && sawFail); seed++)
        {
            var r = Run("S1", ["c01", "c04"], 3, seed); // 60%
            var start = r.Enter(["c01", "c04"], RoomOption.SpinATale);
            FightUnit? first = null;
            start.Fight!.Step((_, u) => first = u);
            Assert.Equal(start.Success ? Side.Heroes : Side.Enemies, first!.Side);
            if (start.Success) sawSuccess = true; else sawFail = true;
        }
        Assert.True(sawSuccess && sawFail);
    }

    [Fact]
    public void SameSeedSameRolls_A3()
    {
        bool Roll(ulong seed) => Run("S1", ["c02"], 3, seed).Enter(["c02"], RoomOption.StrikeOut, 0).Success;
        for (ulong s = 1; s < 10; s++) Assert.Equal(Roll(s), Roll(s));
    }
}
