using Game.Shared.Combat;
using Game.Shared.Content;
using Game.Shared.Stages;

namespace Game.Shared.Tests.Stages;

public class StageRunTests
{
    private static readonly ContentDb Db = TestContent.Db;

    private static StageRun Run(string stage, string[] team, int level = 5, ulong seed = 3) =>
        new(Db, Db.Stage(stage), team.Select(id => new HeroEntry { ChampionId = id, Level = level }).ToList(), seed);

    /// <summary>Heroes wipe the enemy Front each turn; enemies do nothing.</summary>
    private static void HeroesWin(Fight f, FightUnit u)
    {
        if (u.Side == Side.Heroes) f.Damage(f.Line(Side.Enemies)[0], 1_000_000);
    }

    /// <summary>Enemies wipe the hero Front each turn.</summary>
    private static void EnemiesWin(Fight f, FightUnit u)
    {
        if (u.Side == Side.Enemies && f.Line(Side.Heroes).Count > 0) f.Damage(f.Line(Side.Heroes)[0], 1_000_000);
    }

    [Fact]
    public void Team_MustBeAllowedByStage()
    {
        Assert.Throws<ArgumentException>(() => Run("P1", ["c01", "c04"])); // White Snake not in Prologue
        Assert.Throws<ArgumentException>(() => Run("S1", ["c01", "c01"]));
        Assert.Equal(3, Run("S1", ["c01", "c02", "c04"]).Team.Count);
    }

    [Fact]
    public void Party_LimitedBySceneCapacity_CMB8()
    {
        var r = Run("S3", ["c01", "c02", "c04"]); // scene 1 = stairs, capacity 1
        Assert.Throws<ArgumentException>(() => r.Enter(["c01", "c02"]));
        var f = r.Enter(["c02"]);
        Assert.Single(f.Line(Side.Heroes));
        Assert.Equal(8, Db.Stage("S3").EnemyLevel);
    }

    [Fact]
    public void ClearAllScenes_ThreeStars_CMB36()
    {
        var r = Run("S1", ["c01", "c02", "c04"]);
        r.Enter(["c01", "c02"]).Run(HeroesWin);
        r.EndScene();
        Assert.Equal((StageState.ChooseParty, 1), (r.State, r.SceneIndex));
        r.Enter(["c01", "c02", "c04"]).Run(HeroesWin);
        r.EndScene();
        Assert.Equal(StageState.Cleared, r.State);
        Assert.True(r.ThreeStars);
        Assert.Equal(3, r.Stars);
    }

    [Fact]
    public void HpCarries_AndWaitingHeroesHealAtTheDoor_CMB9_33()
    {
        var r = Run("S1", ["c01", "c02", "c04"]);
        var f = r.Enter(["c01", "c02"]);
        var margo = f.Line(Side.Heroes)[0];
        f.Damage(margo, 300);
        f.Run(HeroesWin);
        r.EndScene();
        var hero = r.Hero("c01");
        Assert.Equal(hero.MaxHp - 300, hero.Hp);

        // Scene 2: Margo waits, heals 15% of max (BAL-21)
        var f2 = r.Enter(["c02", "c04"]);
        int heal = (int)Math.Round(hero.MaxHp * 0.15m, MidpointRounding.AwayFromZero);
        Assert.Equal(Math.Min(hero.MaxHp, hero.MaxHp - 300 + heal), hero.Hp);
        Assert.DoesNotContain(f2.Line(Side.Heroes), u => u.DefId == "c01");
    }

    [Fact]
    public void CarriedHp_StartsTheNextFight()
    {
        var r = Run("S1", ["c01", "c02"]);
        var f = r.Enter(["c01"]);
        f.Damage(f.Line(Side.Heroes)[0], 200);
        f.Run(HeroesWin);
        r.EndScene();
        var f2 = r.Enter(["c01", "c02"]);
        Assert.Equal(r.Hero("c01").Hp, f2.Line(Side.Heroes)[0].Hp);
        Assert.Equal(r.Hero("c01").MaxHp - 200, f2.Line(Side.Heroes)[0].Hp);
    }

    [Fact]
    public void Down_StaysDownForTheStage_NoThreeStars_CMB34()
    {
        var r = Run("S1", ["c01", "c02", "c04"]);
        var f = r.Enter(["c01", "c02"]);
        f.Damage(f.Line(Side.Heroes)[0], 1_000_000); // Margo Down
        f.Run(HeroesWin);
        r.EndScene();
        Assert.True(r.Hero("c01").IsDown);
        Assert.Throws<ArgumentException>(() => r.Enter(["c01", "c04"]));
        r.Enter(["c02", "c04"]).Run(HeroesWin);
        r.EndScene();
        Assert.Equal(StageState.Cleared, r.State);
        Assert.False(r.ThreeStars);
        Assert.Equal(2, r.Stars);          // one Down (D-109)
        Assert.Equal(0, r.Hero("c01").Hp); // Down heroes don't heal at the door
    }

    [Fact]
    public void LostFight_FailsStage()
    {
        var r = Run("S1", ["c01", "c02"]);
        r.Enter(["c01", "c02"]).Run(EnemiesWin); // nobody waiting at the door
        r.EndScene();
        Assert.Equal(StageState.Failed, r.State);
        Assert.True(r.IsOver);
        Assert.Equal(0, r.Stars);
    }

    [Fact]
    public void Retreat_AnyTime_CMB35()
    {
        var r = Run("S1", ["c01"]);
        r.Enter(["c01"]);
        r.Retreat();
        Assert.Equal((StageState.Retreated, (Fight?)null), (r.State, r.Fight));
        Assert.Throws<InvalidOperationException>(() => r.Retreat());
    }

    [Fact]
    public void EndScene_NeedsAFinishedFight()
    {
        var r = Run("S1", ["c01"]);
        Assert.Throws<InvalidOperationException>(() => r.EndScene());
        r.Enter(["c01"]);
        Assert.Throws<InvalidOperationException>(() => r.EndScene());
    }

    [Fact]
    public void SameStageSeed_SameSceneSeeds_A3()
    {
        int Roll(ulong seed) => Run("S1", ["c01"], seed: seed).Enter(["c01"]).Random.NextInt(0, 1_000_000);
        Assert.Equal(Roll(8), Roll(8));
    }
}
