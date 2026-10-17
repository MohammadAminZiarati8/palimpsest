using Game.Shared.Combat;
using Game.Shared.Content;
using Game.Shared.Stages;

namespace Game.Shared.Tests.Combat;

public class JoinArrivalTests
{
    private static readonly ContentDb Db = TestContent.Db;

    private static Fight NewFight(string[] heroes, string[] enemies, string[]? bench = null, int capacity = 3,
        ArrivalEntry[]? arrivals = null, string[]? reinforcements = null) =>
        new(Db, new FightSetup
        {
            Heroes = heroes.Select(id => new HeroEntry { ChampionId = id }).ToList(),
            Enemies = enemies.Select(id => new EnemyEntry { EnemyId = id }).ToList(),
            Bench = (bench ?? []).Select(id => new HeroEntry { ChampionId = id }).ToList(),
            HeroCapacity = capacity,
            Arrivals = (arrivals ?? []).ToList(),
            Reinforcements = (reinforcements ?? []).Select(id => new EnemyEntry { EnemyId = id }).ToList(),
            Seed = 4,
        });

    private static void Idle(Fight f, FightUnit u) { }

    private static void PlayRounds(Fight f, int rounds)
    {
        while (f.Round < rounds || f.Pending.Any()) f.Step(Idle);
    }

    // --- Join / Fall back (CMB-10) ---

    [Fact]
    public void Join_EntersAtOurBack_ActsNextRound()
    {
        var f = NewFight(["c01"], ["C1"], bench: ["c04"]);
        f.Step(Idle); // round 1 started
        var snake = f.Join("c04");
        Assert.Equal(1, snake.Line); // Main Door end = behind Margo
        Assert.DoesNotContain(snake, f.Pending);
        Assert.Empty(f.Bench);
        var acted = new List<FightUnit>();
        while (f.Round < 2 || f.Pending.Any()) f.Step((_, u) => acted.Add(u));
        Assert.Contains(snake, acted); // round 2
    }

    [Fact]
    public void Join_RespectsSceneCapacity_CMB8()
    {
        var f = NewFight(["c01", "c02"], ["C1"], bench: ["c04"], capacity: 2);
        Assert.Throws<InvalidOperationException>(() => f.Join("c04"));
        Assert.Throws<ArgumentException>(() => f.Join("c03"));
    }

    [Fact]
    public void FallBack_LeavesToTheDoor_KeepsHp_CanJoinAgain()
    {
        var f = NewFight(["c01", "c02"], ["C1"]);
        var margo = f.Line(Side.Heroes)[0];
        f.Damage(margo, 100);
        f.FallBack(margo);
        Assert.Equal(["c02"], f.Line(Side.Heroes).Select(u => u.DefId)); // gap closed
        Assert.Equal(margo.MaxHp - 100, f.Bench.Single().Hp);
        var back = f.Join("c01");
        Assert.Equal(margo.MaxHp - 100, back.Hp);
    }

    [Fact]
    public void AllOnFieldDown_ButSomeoneAtTheDoor_FightGoesOn_CMB34()
    {
        var f = NewFight(["c01"], ["C1"], bench: ["c02"]);
        f.Damage(f.Line(Side.Heroes)[0], 1_000_000);
        Assert.False(f.IsOver);
        f.Join("c02");
        f.Damage(f.Line(Side.Heroes)[0], 1_000_000);
        Assert.Equal((FightOutcome.Lost, LossReason.HeroesDown), (f.Outcome, f.LossReason));
    }

    // --- nearby arrivals (CMB-22) ---

    [Fact]
    public void Arrival_JoinsEndOfEnemyLine_AfterItsRounds()
    {
        var f = NewFight(["c01"], ["C1"], arrivals: [new ArrivalEntry { EnemyId = "C2", InRounds = 2 }]);
        PlayRounds(f, 2);
        Assert.Single(f.Line(Side.Enemies));
        f.Step(Idle); // round 3 starts: arrival enters and is in the order
        Assert.Equal(["C1", "C2"], f.Line(Side.Enemies).Select(u => u.DefId));
        Assert.Equal(0, f.IncomingCount);
    }

    [Fact]
    public void Arrival_WaitsForAFreeSlot_D97()
    {
        var f = NewFight(["c01"], ["C1", "C1", "C1"], arrivals: [new ArrivalEntry { EnemyId = "C2", InRounds = 1 }]);
        PlayRounds(f, 2);
        Assert.Equal(3, f.Line(Side.Enemies).Count);
        Assert.Equal(1, f.IncomingCount);
        f.Damage(f.Line(Side.Enemies)[0], 1_000_000);
        PlayRounds(f, 3);
        Assert.Equal("C2", f.Line(Side.Enemies)[^1].DefId);
    }

    [Fact]
    public void Win_CancelsPendingArrivals_CMB43()
    {
        var f = NewFight(["c01"], ["C1"], arrivals: [new ArrivalEntry { EnemyId = "C2", InRounds = 3 }]);
        f.Damage(f.Line(Side.Enemies)[0], 1_000_000);
        Assert.Equal(FightOutcome.Won, f.Outcome);
    }

    // --- Herald Ring (ENM-9, D-97: once) ---

    [Fact]
    public void Herald_RingsOnItsSecondTurn_Once_D97()
    {
        var f = NewFight(["c01"], ["C1", "C3"], reinforcements: ["C2"]);
        var herald = f.Line(Side.Enemies)[1];
        Assert.Equal(2, herald.RingIn);
        void Ai(Fight fight, FightUnit u) { if (u.Side == Side.Enemies) EnemyAi.Act(fight, u); }

        while (f.Round < 2 || f.Pending.Any()) f.Step(Ai); // Herald acted twice: rang
        Assert.Equal((0, 1, 1), (herald.RingIn, f.Rings, f.IncomingCount));
        Assert.Null(EnemyAi.Plan(f, herald)); // does nothing after ringing

        f.Step(Ai); // round 3: Inkslinger written in at the end of the line
        Assert.Equal(["C1", "C3", "C2"], f.Line(Side.Enemies).Select(u => u.DefId));
        while (f.Round < 5) f.Step(Ai);
        Assert.Equal(1, f.Rings);
    }

    // --- stage wiring ---

    [Fact]
    public void StageScene_PassesBenchArrivalsAndHerald()
    {
        var run = new StageRun(Db, Db.Stage("S2"),
            new[] { "c01", "c02", "c04" }.Select(id => new HeroEntry { ChampionId = id, Level = 5 }).ToList(), 1);
        run.Enter(["c01", "c02", "c04"]).Run((f, u) => { if (u.Side == Side.Heroes) f.Damage(f.Line(Side.Enemies)[0], 1_000_000); });
        run.EndScene();
        var fight = run.Enter(["c01"]); // scene 2: Herald scene
        Assert.Equal(["c02", "c04"], fight.Bench.Select(b => b.ChampionId));
        Assert.Equal(2, fight.Line(Side.Enemies).Single(u => u.DefId == "C3").RingIn);
        fight.Join("c02");
        fight.Join("c04");
        Assert.Equal(3, fight.Line(Side.Heroes).Count);
        Assert.Empty(fight.Bench);
    }
}
