using Game.Shared.Combat;
using Game.Shared.Content;

namespace Game.Shared.Tests.Combat;

public class EnemyAiTests
{
    private static Fight NewFight(string[] heroes, string[] enemies, ulong seed = 21) =>
        new(TestContent.Db, new FightSetup
        {
            Heroes = heroes.Select(id => new HeroEntry { ChampionId = id }).ToList(),
            Enemies = enemies.Select(id => new EnemyEntry { EnemyId = id }).ToList(),
            Seed = seed,
        });

    private static FightUnit Enemy(Fight f, string id) => f.Line(Side.Enemies).First(u => u.DefId == id);

    /// <summary>Heroes stand still; enemies follow the AI.</summary>
    private static void EnemiesOnly(Fight f, FightUnit u)
    {
        if (u.Side == Side.Enemies) EnemyAi.Act(f, u);
    }

    /// <summary>Starts round 1 without anyone acting: step a hero that does nothing.</summary>
    private static void StartRound1(Fight f)
    {
        foreach (var u in f.Units) u.Stats.Speed = u.Side == Side.Heroes ? 120 : 100;
        f.Step((_, _) => { });
    }

    [Fact]
    public void Melee_TargetsNearestHero()
    {
        var f = NewFight(["c01", "c02"], ["C1"]);
        StartRound1(f);
        var intent = f.IntentOf(Enemy(f, "C1"))!;
        Assert.Equal((SkillSlot.S1, "c01"), (intent.Skill.Slot, intent.Target.DefId));
    }

    [Fact]
    public void Ranged_TargetsLowestHpPercent_TieFrontMost()
    {
        var f = NewFight(["c01", "c02", "c03"], ["C2"]);
        var heroes = f.Line(Side.Heroes);
        Assert.Equal(heroes[0], EnemyAi.Plan(f, Enemy(f, "C2"))!.Target); // all full -> Front-most
        f.Damage(heroes[2], heroes[2].MaxHp / 2);
        f.Damage(heroes[1], heroes[1].MaxHp / 4);
        Assert.Equal(heroes[2], EnemyAi.Plan(f, Enemy(f, "C2"))!.Target);
    }

    [Fact]
    public void SpecialWhenReady_AlwaysRule_ElseS1()
    {
        var f = NewFight(["c01"], ["C2"]);
        var ink = Enemy(f, "C2"); // S2 Splatter, no AI rule = whenever ready
        Assert.Equal(SkillSlot.S2, EnemyAi.Plan(f, ink)!.Skill.Slot);
        EnemyAi.Act(f, ink);
        Assert.Equal(SkillSlot.S1, EnemyAi.Plan(f, ink)!.Skill.Slot); // on cooldown
    }

    [Fact]
    public void BlotterSoak_OnlyOnceDamaged()
    {
        var f = NewFight(["c01"], ["C1"]);
        var blotter = Enemy(f, "C1");
        Assert.Equal(SkillSlot.S1, EnemyAi.Plan(f, blotter)!.Skill.Slot);
        f.Damage(blotter, 1);
        var plan = EnemyAi.Plan(f, blotter)!;
        Assert.Equal((SkillSlot.S2, blotter), (plan.Skill.Slot, plan.Target));
        EnemyAi.Act(f, blotter);
        Assert.True(blotter.Has(StatusEffect.Ward));
    }

    [Fact]
    public void Corrector_HealsOnlyAllyAtOrBelow75Percent()
    {
        var f = NewFight(["c01"], ["C1", "C4"]);
        var (blotter, corrector) = (Enemy(f, "C1"), Enemy(f, "C4"));
        f.Damage(blotter, blotter.MaxHp / 5); // 80%: not yet
        Assert.Equal(SkillSlot.S1, EnemyAi.Plan(f, corrector)!.Skill.Slot);
        f.Damage(blotter, blotter.MaxHp / 20); // 75%
        var plan = EnemyAi.Plan(f, corrector)!;
        Assert.Equal((SkillSlot.S2, blotter), (plan.Skill.Slot, plan.Target));
    }

    [Fact]
    public void Corrector_HealsItself_D89()
    {
        Assert.True(TestContent.Db.Combat.AiHealSelf);
        var f = NewFight(["c01"], ["C4"]);
        var corrector = Enemy(f, "C4");
        f.Damage(corrector, corrector.MaxHp / 2);
        var plan = EnemyAi.Plan(f, corrector)!;
        Assert.Equal((SkillSlot.S2, corrector), (plan.Skill.Slot, plan.Target));
    }

    [Fact]
    public void Editor_FinalSay_BuffsAllAllies()
    {
        var f = NewFight(["c01"], ["C5", "C2"]);
        EnemyAi.Act(f, Enemy(f, "C5"));
        Assert.All(f.Line(Side.Enemies), u => Assert.True(u.Has(StatusEffect.AttackUp)));
    }

    [Fact]
    public void Intent_FixedAtRoundStart_RetargetsWhenTargetDown()
    {
        var f = NewFight(["c01", "c02"], ["C1"]);
        StartRound1(f); // Margo (Front) took the first turn; Blotter's intent = Margo
        var blotter = Enemy(f, "C1");
        var margo = f.Line(Side.Heroes)[0];
        Assert.Equal(margo, f.IntentOf(blotter)!.Target);

        f.Damage(margo, margo.MaxHp); // Margo goes Down before Blotter acts
        var pell = f.Line(Side.Heroes)[0];
        f.Step((fight, u) => { }); // Pell's turn
        Assert.Equal(margo, f.IntentOf(blotter)!.Target); // intent not re-planned mid-round
        var hits = new List<Hit>();
        f.Step((fight, u) => hits.AddRange(EnemyAi.Act(fight, u)));
        Assert.Equal(pell, Assert.Single(hits).Target); // retargeted by the same rule
    }

    [Fact]
    public void Intent_RebuiltEachRound()
    {
        var f = NewFight(["c01"], ["C1"]);
        StartRound1(f);
        var blotter = Enemy(f, "C1");
        Assert.Equal(SkillSlot.S1, f.IntentOf(blotter)!.Skill.Slot);
        f.Damage(blotter, 1); // now Soak qualifies, but only from next round
        Assert.Equal(SkillSlot.S1, f.IntentOf(blotter)!.Skill.Slot);
        f.Step(EnemiesOnly); // Blotter acts (S1)
        f.Step((_, _) => { }); // round 2 starts, Margo idles
        Assert.Equal(SkillSlot.S2, f.IntentOf(blotter)!.Skill.Slot);
    }

    [Fact]
    public void NoValidTarget_SkipsAction()
    {
        var f = NewFight(["c02"], ["C1"]);
        var pell = f.Line(Side.Heroes)[0];
        f.UseSkill(pell, pell.Skill(SkillSlot.S3), pell); // Fade: Blotter's Single melee has no target
        Assert.Null(EnemyAi.Plan(f, Enemy(f, "C1")));
        Assert.Empty(EnemyAi.Act(f, Enemy(f, "C1")));
    }

    // --- A3: same seed + inputs = same result ---

    /// <summary>Full fight: heroes use S1 on their first valid target, enemies use the AI. Returns a log.</summary>
    private static (FightOutcome, string) Simulate(ulong seed)
    {
        var f = NewFight(["c01", "c02", "c03"], ["C1", "C2", "C4"], seed);
        var log = new System.Text.StringBuilder();
        var outcome = f.Run((fight, u) =>
        {
            IReadOnlyList<Hit> hits;
            if (u.Side == Side.Enemies) hits = EnemyAi.Act(fight, u);
            else
            {
                var s1 = u.Skill(SkillSlot.S1);
                var target = Targeting.Valid(fight, u, s1).FirstOrDefault();
                hits = target == null ? new List<Hit>() : fight.UseSkill(u, s1, target);
            }
            log.Append($"{fight.Round}.{u.Id}:");
            foreach (var h in hits) log.Append($"{h.Target.Id}={h.Amount}{(h.Crit ? "!" : "")},");
            log.Append(';');
        });
        return (outcome, log.ToString());
    }

    [Fact]
    public void SameSeedAndInputs_SameFight_A3()
    {
        var a = Simulate(1234);
        var b = Simulate(1234);
        Assert.Equal(a, b);
        Assert.NotEqual(FightOutcome.Ongoing, a.Item1);
        Assert.NotEqual(a.Item2, Simulate(9999).Item2); // the seed matters
    }
}
