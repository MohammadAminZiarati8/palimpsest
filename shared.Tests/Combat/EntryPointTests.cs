using Game.Shared.Combat;
using Game.Shared.Content;
using Game.Shared.Stages;

namespace Game.Shared.Tests.Combat;

/// <summary>M2b-T5: Margin / Gutter / Bleed-through (CMB-17/18), melee both sides (CMB-12), Exposed (CMB-15).</summary>
public class EntryPointTests
{
    private static readonly ContentDb Db = TestContent.Db;
    private static readonly SkillDef Melee = new() { Name = "m", Reach = Reach.Melee, Area = Area.Single, DamagePercent = 1 };
    private static readonly SkillDef Ranged = new() { Name = "r", Reach = Reach.Ranged, Area = Area.Single, DamagePercent = 1 };
    private static readonly SkillDef Splash = new() { Name = "s", Reach = Reach.Ranged, Area = Area.Splash, DamagePercent = 1 };

    private static (string Id, Spot Spot) M(string id) => (id, Spot.Main);
    private static (string Id, Spot Spot) B(string id) => (id, Spot.Behind);
    private static (string Id, Spot Spot) G(string id) => (id, Spot.Gutter);

    private static Fight NewFight((string Id, Spot Spot)[] heroes, (string Id, Spot Spot)[] enemies,
        ulong seed = 5, List<ArrivalEntry>? arrivals = null, List<HeroEntry>? bench = null) =>
        new(Db, new FightSetup
        {
            Heroes = heroes.Select(h => new HeroEntry { ChampionId = h.Id, Spot = h.Spot }).ToList(),
            Enemies = enemies.Select(e => new EnemyEntry { EnemyId = e.Id, Spot = e.Spot }).ToList(),
            Arrivals = arrivals ?? [],
            Bench = bench ?? [],
            Seed = seed,
        });

    private static string[] RowIds(Fight f) => f.Row.Select(u => u.DefId).ToArray();
    private static FightUnit U(Fight f, string id) => f.Row.Single(u => u.DefId == id);
    private static void Idle(Fight f, FightUnit u) { }

    [Fact]
    public void Margin_FlankerStandsBehindEnemyBack_CMB17()
    {
        var f = NewFight([M("c01"), B("c02")], [M("C1"), M("C2")]);
        Assert.Equal(["c01", "C1", "C2", "c02"], RowIds(f));
        Assert.Equal(Spot.Behind, U(f, "c02").Spot);
        Assert.Equal([U(f, "c01"), U(f, "c02")], f.Line(Side.Heroes)); // Main first, then flankers
    }

    [Fact]
    public void SoloMarginFlanker_StaysBehind_D101()
    {
        var f = NewFight([B("c02")], [M("C1"), M("C2")]);
        Assert.Equal(["C1", "C2", "c02"], RowIds(f));
        Assert.Equal(Spot.Behind, U(f, "c02").Spot);
    }

    [Fact]
    public void Melee_NearestEnemyOnEitherSide_CMB12()
    {
        var f = NewFight([M("c01"), G("c02")], [M("C1"), M("C2"), M("C4")]);
        Assert.Equal(["c01", "C1", "c02", "C2", "C4"], RowIds(f));
        Assert.Equal([U(f, "C1")], Targeting.Valid(f, U(f, "c01"), Melee));
        Assert.Equal([U(f, "C1"), U(f, "C2")], Targeting.Valid(f, U(f, "c02"), Melee));
        // Enemies can reach the Gutter hero from both sides too.
        Assert.Equal([U(f, "c02")], Targeting.Valid(f, U(f, "C2"), Melee));
        Assert.Equal([U(f, "c01"), U(f, "c02")], Targeting.Valid(f, U(f, "C1"), Melee));
    }

    [Fact]
    public void Exposed_MarginHitsFromBehind_MeleeAndRanged_D102()
    {
        var f = NewFight([M("c01"), B("c02")], [M("C1"), M("C2")]);
        var (main, flank, front, back) = (U(f, "c01"), U(f, "c02"), U(f, "C1"), U(f, "C2"));
        Assert.True(Targeting.IsExposed(flank, back));
        Assert.True(Targeting.IsExposed(flank, front)); // ranged from behind counts too
        Assert.False(Targeting.IsExposed(main, front));
        Assert.False(Targeting.IsExposed(back, flank));  // the flanker faces the enemies
        Assert.False(Targeting.IsExposed(front, main));
    }

    [Fact]
    public void BleedThrough_MirrorsMargin_CMB18()
    {
        var f = NewFight([M("c01"), M("c02")], [M("C1"), B("C2")]);
        Assert.Equal(["C2", "c02", "c01", "C1"], RowIds(f));
        var (bleed, ourBack) = (U(f, "C2"), U(f, "c02"));
        Assert.True(Targeting.IsExposed(bleed, ourBack));
        Assert.True(Targeting.IsExposed(bleed, U(f, "c01")));
        Assert.False(Targeting.IsExposed(ourBack, bleed));
        Assert.Equal([bleed, U(f, "C1")], Targeting.Valid(f, ourBack, Melee)); // c01 is an ally: doesn't block
        Assert.Equal([bleed, U(f, "C1")], Targeting.Valid(f, U(f, "c01"), Melee));
    }

    [Fact]
    public void Gutter_NoSafeSide_D103()
    {
        var f = NewFight([M("c01"), G("c02")], [M("C1"), M("C2"), M("C4")]);
        var (gutter, front, mid) = (U(f, "c02"), U(f, "C1"), U(f, "C2"));
        Assert.True(Targeting.IsExposed(front, gutter));
        Assert.True(Targeting.IsExposed(mid, gutter));
        Assert.True(Targeting.IsExposed(gutter, front));  // the Front has its back to the Gutter
        Assert.False(Targeting.IsExposed(gutter, mid));   // the ones behind face it
    }

    [Fact]
    public void Gutter_WalksBackAsNewFront_WhenEnemyFrontDown_D104()
    {
        var f = NewFight([M("c01"), G("c02")], [M("C1"), M("C2")]);
        f.Damage(U(f, "C1"), 1_000_000);
        var gutter = U(f, "c02");
        Assert.Equal(["c01", "c02", "C2"], RowIds(f));
        Assert.Equal(Spot.Main, gutter.Spot);
        Assert.Equal(gutter, f.Line(Side.Heroes)[0]);
        Assert.False(Targeting.IsExposed(U(f, "C2"), gutter));
    }

    [Fact]
    public void BleedThrough_WalksBack_WhenOurLineGone_D104()
    {
        var f = NewFight([M("c01"), B("c02")], [M("C1"), B("C2")]);
        f.Damage(U(f, "c01"), 1_000_000);
        Assert.Equal(["C2", "C1", "c02"], RowIds(f));
        Assert.Equal(Spot.Main, U(f, "C2").Spot);
        Assert.Equal(U(f, "C2"), f.Line(Side.Enemies)[0]); // new enemy Front
        Assert.Equal(Spot.Behind, U(f, "c02").Spot);       // Margin flanker stays
    }

    [Fact]
    public void MarginFlanker_Stays_WhenEnemyBackDown_CMB14()
    {
        var f = NewFight([M("c01"), B("c02")], [M("C1"), M("C2")]);
        f.Damage(U(f, "C2"), 1_000_000);
        Assert.Equal(["c01", "C1", "c02"], RowIds(f));
        Assert.Equal(Spot.Behind, U(f, "c02").Spot);
        Assert.True(Targeting.IsExposed(U(f, "c02"), U(f, "C1")));
    }

    [Fact]
    public void Arrival_LandsBehindMarginFlanker_AndHitsItExposed_CMB22()
    {
        var f = NewFight([M("c01"), B("c02")], [M("C1"), M("C2")],
            arrivals: [new ArrivalEntry { EnemyId = "C4", InRounds = 1 }]);
        while (f.Round < 2) f.Step(Idle);
        Assert.Equal(["c01", "C1", "C2", "c02", "C4"], RowIds(f));
        Assert.True(Targeting.IsExposed(U(f, "C4"), U(f, "c02")));
        Assert.Equal([U(f, "C2"), U(f, "C4")], Targeting.Valid(f, U(f, "c02"), Melee));
    }

    [Fact]
    public void Join_EntersInsideBleedThrough_D105()
    {
        var f = NewFight([M("c01")], [M("C1"), B("C2")], bench: [new HeroEntry { ChampionId = "c02" }]);
        f.Join("c02");
        Assert.Equal(["C2", "c02", "c01", "C1"], RowIds(f));
        Assert.Equal(Spot.Main, U(f, "c02").Spot);
    }

    [Fact]
    public void Splash_FarSideFromAttacker_NeverAllies_D78()
    {
        var f = NewFight([M("c01"), B("c02")], [M("C1"), M("C2")]);
        Assert.Equal([U(f, "C1"), U(f, "C2")], Targeting.Hits(f, Splash, U(f, "C1"), U(f, "c01")));
        Assert.Equal([U(f, "C2"), U(f, "C1")], Targeting.Hits(f, Splash, U(f, "C2"), U(f, "c02")));

        var g = NewFight([M("c01"), G("c02")], [M("C1"), M("C2")]);
        Assert.Equal([U(g, "C1")], Targeting.Hits(g, Splash, U(g, "C1"), U(g, "c01"))); // allies on both sides
    }

    [Fact]
    public void Shift_NotForFlankers()
    {
        var f = NewFight([M("c01"), B("c02")], [M("C1")]);
        Assert.Throws<ArgumentException>(() => f.Shift(U(f, "c01"), U(f, "c02")));
    }

    [Fact]
    public void Exposed_RaisesCritRate_BAL20()
    {
        int Crits(Spot spot) => Enumerable.Range(0, 300).Count(seed =>
        {
            var f = NewFight([(id: "c01", spot)], [M("C1")], (ulong)seed);
            return f.Attack(U(f, "c01"), Ranged, U(f, "C1"))[0].Crit;
        });
        Assert.True(Crits(Spot.Behind) > Crits(Spot.Main) + 40); // +25% of 300 ≈ +75
    }

    [Fact]
    public void StageRun_MarginHero_NeedsMarginPathAndParty_D101()
    {
        StageRun Run(string stage) => new(Db, Db.Stage(stage),
            new[] { "c01", "c02", "c04" }.Select(id => new HeroEntry { ChampionId = id, Level = 6 }).ToList(), 3);

        var start = Run("S2").Enter(["c01", "c02"], RoomOption.Clash, marginHero: "c02"); // S2 scene 1 has a Margin Path
        var f = start.Fight!;
        Assert.Equal(Spot.Behind, U(f, "c02").Spot);
        Assert.Equal(f.Row.Count - 1, U(f, "c02").Pos);

        Assert.Throws<ArgumentException>(() => Run("S2").Enter(["c01"], RoomOption.Clash, marginHero: "c02"));
        Assert.Throws<ArgumentException>(() => Run("S1").Enter(["c01", "c02"], RoomOption.Clash, marginHero: "c02"));
    }
}
