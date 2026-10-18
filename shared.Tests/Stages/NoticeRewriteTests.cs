using Game.Shared.Combat;
using Game.Shared.Content;
using Game.Shared.Stages;

namespace Game.Shared.Tests.Stages;

/// <summary>M2b-T6: Notice meter (CMB-23, D-106, D-108) and Rewrite (CMB-20, D-107).</summary>
public class NoticeRewriteTests
{
    private static readonly ContentDb Db = TestContent.Db;
    private static readonly StageFile Rules = Db.StageRules;

    private static SceneDef Scene(Alert alert, params string[] enemies) =>
        new() { Capacity = 3, Alert = alert, Enemies = enemies.ToList(), Reinforcements = enemies.Contains("C3") ? ["C2"] : [] };

    private static StageRun Run(params SceneDef[] scenes) =>
        new(Db, new StageDef { Id = "T", Name = "Test", EnemyLevel = 1, Heroes = ["c01", "c02", "c04"], Scenes = scenes.ToList() },
            new[] { "c01", "c02", "c04" }.Select(id => new HeroEntry { ChampionId = id, Level = 10 }).ToList(), 3);

    private static void KillAll(Fight f)
    {
        foreach (var e in f.Line(Side.Enemies).ToList()) f.Damage(e, 1_000_000);
    }

    /// <summary>Clash the current scene and win it at once.</summary>
    private static void ClashAndWin(StageRun r)
    {
        KillAll(r.Enter(["c01", "c02"]));
        r.EndScene();
    }

    private static void Ai(Fight f, FightUnit u) { if (u.Side == Side.Enemies) EnemyAi.Act(f, u); }

    [Fact]
    public void Fight_AddsNoticePerFight_D108()
    {
        var r = Run(Scene(Alert.Watching, "C1"), Scene(Alert.Watching, "C1"));
        ClashAndWin(r);
        Assert.Equal(Rules.NoticePerFight, r.Notice);
    }

    [Fact]
    public void StealthClear_AddsNoNotice_CMB23()
    {
        for (ulong seed = 1; seed < 40; seed++)
        {
            var r = new StageRun(Db, new StageDef { Id = "T", EnemyLevel = 1, Heroes = ["c02"], Scenes = [Scene(Alert.Reading, "C1"), Scene(Alert.Watching, "C1")] },
                [new HeroEntry { ChampionId = "c02", Level = 10 }], seed);
            if (r.Enter(["c02"], RoomOption.BetweenTheLines).Fight != null) continue;
            Assert.Equal(0, r.Notice);
            return;
        }
        Assert.Fail("no stealth success in 40 seeds");
    }

    [Fact]
    public void HeraldRing_AddsNoticePerRing_CMB23()
    {
        var r = Run(Scene(Alert.Watching, "C3"), Scene(Alert.Watching, "C1"));
        var f = r.Enter(["c01"]);
        while (f.Rings == 0) f.Step(Ai);
        KillAll(f);
        r.EndScene();
        Assert.Equal(Rules.NoticePerFight + Rules.NoticePerRing, r.Notice);
    }

    [Fact]
    public void HighNotice_ReadingStartsWatching_ThenPatrolJoins_D106()
    {
        var scenes = Enumerable.Range(0, 6).Select(_ => Scene(Alert.Reading, "C1")).ToArray();
        var r = Run(scenes);
        while (r.Notice < Rules.NoticeWatchingAt)
        {
            Assert.Equal(Alert.Reading, r.SceneAlert);
            ClashAndWin(r);
        }
        Assert.Equal(Alert.Watching, r.SceneAlert);
        Assert.DoesNotContain(RoomOption.MarginStrike, r.Allowed(["c01"]));
        Assert.False(r.ScenePatrol);
        while (r.Notice < Rules.NoticePatrolAt) ClashAndWin(r);

        Assert.True(r.ScenePatrol);
        var f = r.Enter(["c01", "c02"]);
        Assert.Equal(1, f.IncomingCount); // the patrol (D-106)
        while (f.Round <= Rules.NoticePatrol.InRounds) f.Step((_, _) => { });
        Assert.Contains(f.Line(Side.Enemies), u => u.DefId == Rules.NoticePatrol.Enemy);
    }

    [Fact]
    public void Rewrite_Calm_MakesSceneReading_OneChargePerStage_D107()
    {
        var r = Run(Scene(Alert.Watching, "C1"), Scene(Alert.Watching, "C1"));
        Assert.True(r.RewriteLeft);
        r.Rewrite(RewriteUse.Calm);
        Assert.Equal(Alert.Reading, r.SceneAlert);
        Assert.Contains(RoomOption.MarginStrike, r.Allowed(["c01"]));
        Assert.False(r.RewriteLeft);
        Assert.Throws<InvalidOperationException>(() => r.Rewrite(RewriteUse.Reveal));
        ClashAndWin(r);
        Assert.Null(r.SceneRewrite);                  // only for that scene
        Assert.Equal(Alert.Watching, r.SceneAlert);
    }

    [Fact]
    public void Rewrite_Silence_HeraldNeverRings_D107()
    {
        var r = Run(Scene(Alert.Watching, "C3"));
        r.Rewrite(RewriteUse.Silence);
        var f = r.Enter(["c01"]);
        while (f.Round < 6) f.Step(Ai);
        Assert.Equal(0, f.Rings);
    }

    [Fact]
    public void Rewrite_Reveal_OpensMarginEntry_D107()
    {
        var r = Run(Scene(Alert.Watching, "C1", "C2"));
        Assert.Throws<ArgumentException>(() => r.Enter(["c01", "c02"], RoomOption.Clash, marginHero: "c02"));
        r.Rewrite(RewriteUse.Reveal);
        var f = r.Enter(["c01", "c02"], RoomOption.Clash, marginHero: "c02").Fight!;
        Assert.Equal(Spot.Behind, f.Row.Single(u => u.DefId == "c02").Spot);
    }

    [Fact]
    public void Rewrite_ThatDoesNothing_IsRefused_D107()
    {
        var r = Run(Scene(Alert.Reading, "C1"));
        Assert.Throws<ArgumentException>(() => r.Rewrite(RewriteUse.Calm));    // already Reading
        Assert.Throws<ArgumentException>(() => r.Rewrite(RewriteUse.Silence)); // no Herald
        Assert.True(r.RewriteLeft);
    }
}
