using Game.Shared.Champions;
using Game.Shared.Combat;
using Game.Shared.Content;

namespace Game.Shared.Tests.Combat;

public class FightTests
{
    private static Fight NewFight(string[] heroes, string[] enemies, ITurnOrder? order = null) =>
        new(TestContent.Db, new FightSetup
        {
            Heroes = heroes.Select(id => new HeroEntry { ChampionId = id }).ToList(),
            Enemies = enemies.Select(id => new EnemyEntry { EnemyId = id }).ToList(),
            Seed = 42,
        }, order);

    private static void Idle(Fight f, FightUnit u) { }

    private static FightUnit[] Order(Fight f) => SpeedTurnOrder.Instance
        .Build(f.Line(Side.Heroes).Concat(f.Line(Side.Enemies))).ToArray();

    [Fact]
    public void Build_UsesStatCalculators()
    {
        var f = new Fight(TestContent.Db, new FightSetup
        {
            Heroes = { new HeroEntry { ChampionId = "c01", Level = 5, Rank = 2 } },
            Enemies = { new EnemyEntry { EnemyId = "C2", Level = 4 } },
        });
        var expect = StatCalculator.At(TestContent.Db, TestContent.Db.Champion("c01"), 5, 2);
        Assert.Equal(expect.Hp, f.Line(Side.Heroes)[0].Hp);
        Assert.Equal(413, f.Line(Side.Enemies)[0].MaxHp); // BAL-17 table
        Assert.NotEmpty(f.Line(Side.Heroes)[0].Skills);
    }

    [Fact]
    public void Lines_FrontFirst_CMB11()
    {
        var f = NewFight(["c01", "c02", "c03"], ["C1", "C2"]);
        Assert.Equal(["c01", "c02", "c03"], f.Line(Side.Heroes).Select(u => u.DefId));
        Assert.Equal([0, 1], f.Line(Side.Enemies).Select(u => u.Line));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(4, 1)]
    [InlineData(1, 0)]
    public void Setup_NeedsOneToThreePerSide(int heroes, int enemies) =>
        Assert.Throws<ArgumentException>(() => NewFight(
            Enumerable.Repeat("c01", heroes).ToArray(), Enumerable.Repeat("C1", enemies).ToArray()));

    [Fact]
    public void Order_SpeedHighFirst_CMB25()
    {
        var f = NewFight(["c01", "c02"], ["C1", "C2"]);
        var (h0, h1) = (f.Line(Side.Heroes)[0], f.Line(Side.Heroes)[1]);
        var (e0, e1) = (f.Line(Side.Enemies)[0], f.Line(Side.Enemies)[1]);
        h0.Stats.Speed = 90; h1.Stats.Speed = 120; e0.Stats.Speed = 100; e1.Stats.Speed = 110;
        Assert.Equal([h1, e1, e0, h0], Order(f));
    }

    [Fact]
    public void Order_TieChampionThenFrontMost_CMB43()
    {
        var f = NewFight(["c01", "c02"], ["C1", "C2"]);
        foreach (var u in f.Units) u.Stats.Speed = 100;
        var h = f.Line(Side.Heroes);
        var e = f.Line(Side.Enemies);
        Assert.Equal([h[0], h[1], e[0], e[1]], Order(f));
    }

    [Fact]
    public void Round_EveryoneActsOnceInOrder()
    {
        var f = NewFight(["c01", "c02"], ["C1"]);
        var acted = new List<FightUnit>();
        while (f.Round < 2) f.Step((_, u) => acted.Add(u));
        Assert.Equal(Order(f), acted.Take(3));
        Assert.Equal(4, acted.Count); // 3 in round 1 + first of round 2
    }

    [Fact]
    public void Next_PeeksWithoutTakingTheTurn()
    {
        var f = NewFight(["c01"], ["C1"]);
        var (h, e) = (f.Line(Side.Heroes)[0], f.Line(Side.Enemies)[0]);
        h.Stats.Speed = 120; e.Stats.Speed = 100;
        Assert.Equal(h, f.Next());
        Assert.Equal(h, f.Next());
        Assert.Equal((1, 0), (f.Round, f.Turn));
        FightUnit? acted = null;
        f.Step((_, u) => acted = u);
        Assert.Equal(h, acted);
        Assert.Equal(e, f.Next());
    }

    [Fact]
    public void Order_RebuiltAtRoundStart_CMB43()
    {
        var f = NewFight(["c01"], ["C1"]);
        var (h, e) = (f.Line(Side.Heroes)[0], f.Line(Side.Enemies)[0]);
        h.Stats.Speed = 120; e.Stats.Speed = 100;
        var acted = new List<FightUnit>();
        f.Step((_, u) => { acted.Add(u); e.Stats.Speed = 130; }); // round 1: hero first
        f.Step((_, u) => acted.Add(u));                             // round 1: enemy (no re-sort mid-round)
        f.Step((_, u) => acted.Add(u));                             // round 2: rebuilt, enemy now faster
        Assert.Equal([h, e, e], acted);
        Assert.Equal(2, f.Round);
    }

    [Fact]
    public void DownUnit_SkipsItsTurn_AndLeavesNextRound()
    {
        var f = NewFight(["c01"], ["C1", "C2"]);
        foreach (var u in f.Units) u.Stats.Speed = 100;
        var c2 = f.Line(Side.Enemies)[1];
        var acted = new List<FightUnit>();
        f.Step((fight, u) => { acted.Add(u); fight.Damage(c2, c2.Hp); }); // hero downs C2 before it acts
        while (f.Round < 2 || f.Pending.Any()) f.Step((_, u) => acted.Add(u));
        Assert.DoesNotContain(c2, acted);
        Assert.Equal(4, acted.Count); // hero, C1, hero, C1
    }

    [Fact]
    public void Down_ClosesGapTowardCentre_CMB14()
    {
        var f = NewFight(["c01", "c02", "c03"], ["C1", "C2", "C4"]);
        var front = f.Line(Side.Enemies)[0];
        var (mid, back) = (f.Line(Side.Enemies)[1], f.Line(Side.Enemies)[2]);
        f.Damage(front, front.Hp);
        Assert.True(front.IsDown);
        Assert.Equal(-1, front.Line);
        Assert.Equal([mid, back], f.Line(Side.Enemies));
        Assert.Equal((0, 1), (mid.Line, back.Line));

        var heroMid = f.Line(Side.Heroes)[1];
        f.Damage(heroMid, heroMid.Hp + 999); // HP clamps at 0
        Assert.Equal(0, heroMid.Hp);
        Assert.Equal(["c01", "c03"], f.Line(Side.Heroes).Select(u => u.DefId));
    }

    [Fact]
    public void PartialDamage_KeepsSlot()
    {
        var f = NewFight(["c01"], ["C1"]);
        var e = f.Line(Side.Enemies)[0];
        f.Damage(e, 1);
        Assert.Equal((e.MaxHp - 1, 0, false), (e.Hp, e.Line, e.IsDown));
    }

    [Fact]
    public void Win_AllEnemiesDown_CMB43()
    {
        var f = NewFight(["c01", "c02"], ["C1", "C2"]);
        var outcome = f.Run((fight, u) =>
        {
            if (u.Side == Side.Heroes) fight.Damage(fight.Line(Side.Enemies)[0], 1000);
        });
        Assert.Equal((FightOutcome.Won, LossReason.None), (outcome, f.LossReason));
        Assert.All(f.Units.Where(u => u.Side == Side.Enemies), u => Assert.True(u.IsDown));
        Assert.False(f.Step(Idle));
    }

    [Fact]
    public void Loss_AllHeroesDown()
    {
        var f = NewFight(["c01", "c02"], ["C1"]);
        var outcome = f.Run((fight, u) =>
        {
            if (u.Side == Side.Enemies) fight.Damage(fight.Line(Side.Heroes)[0], 100_000);
        });
        Assert.Equal((FightOutcome.Lost, LossReason.HeroesDown), (outcome, f.LossReason));
    }

    [Fact]
    public void Loss_RoundCap_CMB43()
    {
        var f = NewFight(["c01"], ["C1"]);
        int turns = 0;
        var outcome = f.Run((_, _) => turns++);
        Assert.Equal((FightOutcome.Lost, LossReason.RoundCap), (outcome, f.LossReason));
        Assert.Equal(50, TestContent.Db.Combat.RoundCap);
        Assert.Equal(50, f.Round);
        Assert.Equal(100, turns); // every round played in full
    }

    [Fact]
    public void WinOnLastRound_BeatsRoundCap()
    {
        var f = NewFight(["c01"], ["C1"]);
        var outcome = f.Run((fight, u) =>
        {
            if (fight.Round == 50 && u.Side == Side.Heroes) fight.Damage(fight.Line(Side.Enemies)[0], 100_000);
        });
        Assert.Equal(FightOutcome.Won, outcome);
    }

    private sealed class ReverseSetupOrder : ITurnOrder
    {
        public IReadOnlyList<FightUnit> Build(IEnumerable<FightUnit> onField) => onField.OrderByDescending(u => u.Id).ToList();
    }

    [Fact]
    public void TurnOrder_IsSwappable_BAL3()
    {
        var f = NewFight(["c01", "c02"], ["C1"], new ReverseSetupOrder());
        var acted = new List<int>();
        for (int i = 0; i < 3; i++) f.Step((_, u) => acted.Add(u.Id));
        Assert.Equal([2, 1, 0], acted);
    }

    [Fact]
    public void SameSeed_SameRandom_A3()
    {
        int Roll(Fight f) => f.Random.NextInt(0, 1_000_000);
        Assert.Equal(Roll(NewFight(["c01"], ["C1"])), Roll(NewFight(["c01"], ["C1"])));
    }
}
