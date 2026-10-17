using Game.Shared.Combat;
using Game.Shared.Content;

namespace Game.Shared.Tests.Combat;

public class EffectTests
{
    private static Fight NewFight(string[] heroes, string[] enemies) =>
        new(TestContent.Db, new FightSetup
        {
            Heroes = heroes.Select(id => new HeroEntry { ChampionId = id }).ToList(),
            Enemies = enemies.Select(id => new EnemyEntry { EnemyId = id }).ToList(),
            Seed = 5,
        });

    /// <summary>1 hero vs 1 enemy, hero always acts first (Speed 120 vs 100).</summary>
    private static (Fight F, FightUnit H, FightUnit E) Duel(string hero = "c01", string enemy = "C5")
    {
        var f = NewFight([hero], [enemy]);
        var (h, e) = (f.Line(Side.Heroes)[0], f.Line(Side.Enemies)[0]);
        h.Stats.Speed = 120; e.Stats.Speed = 100;
        return (f, h, e);
    }

    private static void Idle(Fight f, FightUnit u) { }

    // --- stat effects (CMB-40, balancing.md) ---

    [Fact]
    public void StatEffects_ChangeEffectiveStats()
    {
        var (f, h, e) = Duel();
        h.Stats.Attack = 200; h.Stats.Defense = 100; h.Stats.Speed = 100;
        f.ApplyEffect(h, StatusEffect.AttackUp, 1, e);
        f.ApplyEffect(h, StatusEffect.DefenseDown, 1, e);
        f.ApplyEffect(h, StatusEffect.SpeedDown, 1, e);
        Assert.Equal((250, 70, 85), (h.Attack, h.Defense, h.Speed));
        f.ApplyEffect(h, StatusEffect.AttackDown, 1, e); // up and down cancel
        Assert.Equal(200, h.Attack);
        Assert.Equal(200, h.Stats.Attack); // base unchanged
    }

    [Fact]
    public void SameEffect_Refreshes_NoStack_BAL15()
    {
        var (f, h, e) = Duel();
        f.ApplyEffect(h, StatusEffect.DefenseUp, 1, e);
        f.ApplyEffect(h, StatusEffect.DefenseUp, 3, e);
        var only = Assert.Single(h.Effects);
        Assert.Equal(3, only.Rounds);
        Assert.Equal((int)Math.Round(h.Stats.Defense * 1.3m, MidpointRounding.AwayFromZero), h.Defense);
    }

    [Fact]
    public void Duration_TicksAtEndOfAffectedUnitsTurn_BAL15()
    {
        var (f, h, e) = Duel();
        f.Step((fight, _) => fight.ApplyEffect(e, StatusEffect.AttackDown, 2, h)); // hero turn
        Assert.Equal(2, e.Effect(StatusEffect.AttackDown)!.Rounds);
        f.Step(Idle); // enemy turn 1
        Assert.Equal(1, e.Effect(StatusEffect.AttackDown)!.Rounds);
        f.Step(Idle); f.Step(Idle); // hero, enemy turn 2
        Assert.False(e.Has(StatusEffect.AttackDown));
    }

    [Fact]
    public void SelfEffect_LastsThroughNextOwnTurn_D84()
    {
        var (f, h, _) = Duel();
        f.Step((fight, u) => fight.ApplyEffect(u, StatusEffect.DefenseDown, 1, u)); // Margo S3 style
        f.Step(Idle); // enemy acts while Margo is still Defense down
        Assert.True(h.Has(StatusEffect.DefenseDown));
        f.Step(Idle); // Margo's next turn ends -> gone
        Assert.False(h.Has(StatusEffect.DefenseDown));
    }

    // --- Stun, Bleed, Ward, Seal ---

    [Fact]
    public void Stun_SkipsNextTurn_ThenWearsOff()
    {
        var (f, h, e) = Duel();
        var acted = new List<FightUnit>();
        f.Step((fight, u) => { acted.Add(u); fight.ApplyEffect(e, StatusEffect.Stun, 1, u); });
        for (int i = 0; i < 3; i++) f.Step((_, u) => acted.Add(u));
        Assert.Equal([h, h, e], acted); // enemy's round-1 turn skipped
        Assert.Equal(4, f.Turn);         // the skipped turn still counts
    }

    [Fact]
    public void Bleed_TicksAtStartOfTurn_30PercentOfApplierAttack_IgnoresDefense()
    {
        var (f, h, e) = Duel();
        h.Stats.Attack = 101;
        e.Stats.Defense = 500;
        f.Step((fight, u) => fight.ApplyEffect(e, StatusEffect.Bleed, 2, u));
        h.Stats.Attack = 999; // snapshot at apply time
        int hp = e.Hp;
        f.Step((_, _) => Assert.Equal(hp - 30, e.Hp)); // 30.3 -> 30, before the enemy acts
        f.Step(Idle); f.Step(Idle);
        Assert.Equal(hp - 60, e.Hp);
        f.Step(Idle); f.Step(Idle); // expired after 2 ticks
        Assert.Equal(hp - 60, e.Hp);
    }

    [Fact]
    public void Bleed_CanDownUnit_BeforeItActs()
    {
        var (f, h, e) = Duel();
        f.Damage(e, e.Hp - 1);
        bool enemyActed = false;
        f.Step((fight, u) => fight.ApplyEffect(e, StatusEffect.Bleed, 2, u));
        f.Step((_, _) => enemyActed = true);
        Assert.False(enemyActed);
        Assert.Equal(FightOutcome.Won, f.Outcome);
    }

    [Fact]
    public void Ward_AbsorbsDamageAndBleed_ThenBreaks()
    {
        var (f, h, e) = Duel();
        f.ApplyEffect(e, StatusEffect.Ward, 2, e, amount: 50);
        int hp = e.Hp;
        Assert.Equal(0, f.Damage(e, 30));
        Assert.Equal(20, e.Effect(StatusEffect.Ward)!.Amount);
        Assert.Equal(10, f.Damage(e, 30)); // 20 absorbed, ward broken
        Assert.False(e.Has(StatusEffect.Ward));
        Assert.Equal(hp - 10, e.Hp);
    }

    [Fact]
    public void Seal_LocksOneSkill()
    {
        var (f, h, e) = Duel();
        var s2 = h.Skill(SkillSlot.S2);
        f.ApplyEffect(h, StatusEffect.Seal, 1, e, sealedSlot: SkillSlot.S2);
        Assert.False(h.CanUse(s2));
        Assert.True(h.CanUse(h.Skill(SkillSlot.S3)));
        Assert.Throws<InvalidOperationException>(() => f.Attack(h, s2, e));
        Assert.Throws<ArgumentException>(() => f.ApplyEffect(h, StatusEffect.Seal, 1, e, sealedSlot: SkillSlot.S1));
    }

    [Fact]
    public void Cleanse_RemovesNewestDebuffFirst_KeepsBuffs()
    {
        var (f, h, e) = Duel();
        f.ApplyEffect(h, StatusEffect.AttackDown, 2, e);
        f.ApplyEffect(h, StatusEffect.DefenseUp, 2, h);
        f.ApplyEffect(h, StatusEffect.Bleed, 2, e);
        Assert.Equal(1, f.Cleanse(h, 1));
        Assert.False(h.Has(StatusEffect.Bleed));
        Assert.True(h.Has(StatusEffect.AttackDown));
        Assert.Equal(1, f.Cleanse(h, 5));
        Assert.Equal([StatusEffect.DefenseUp], h.Effects.Select(x => x.Type));
    }

    // --- skill effects + cooldowns (CMB-42) ---

    [Fact]
    public void SkillEffects_LandOnTargetsAndSelf()
    {
        var (f, pell, e) = Duel("c02");
        f.Attack(pell, pell.Skill(SkillSlot.S2), e); // Strikethrough + Bleed 2
        Assert.Equal(2, e.Effect(StatusEffect.Bleed)!.Rounds);

        var (g, margo, e2) = Duel("c01");
        g.Attack(margo, margo.Skill(SkillSlot.S3), e2); // Leap + self Defense down 1
        Assert.True(margo.Has(StatusEffect.DefenseDown));
        Assert.False(e2.Has(StatusEffect.DefenseDown));
    }

    [Fact]
    public void Cooldown_UsedTurn1_ReadyTurn1PlusN_D77()
    {
        var (f, h, e) = Duel();
        var s2 = h.Skill(SkillSlot.S2); // cd 3
        Assert.Equal(3, s2.Cooldown);
        Assert.True(h.CanUse(s2)); // ready at fight start
        var ready = new List<bool>();
        f.Step((fight, u) => fight.Attack(u, s2, e)); // own turn 1
        for (int t = 2; t <= 4; t++)
        {
            f.Step(Idle);                              // enemy
            f.Step((_, u) => ready.Add(u.CanUse(s2))); // own turn t
        }
        Assert.Equal([false, false, true], ready);
    }

    [Fact]
    public void Cooldown_TicksOnStunnedTurns_D77()
    {
        var (f, h, e) = Duel();
        var s2 = h.Skill(SkillSlot.S2);
        f.Step((fight, u) => fight.Attack(u, s2, e));         // turn 1: cd 3 -> 2
        f.Step((fight, _) => fight.ApplyEffect(h, StatusEffect.Stun, 1, e));
        f.Step((_, _) => Assert.Fail("stunned"));             // turn 2 skipped: 2 -> 1
        f.Step(Idle);
        f.Step(Idle);                                         // turn 3: 1 -> 0
        Assert.True(h.CanUse(s2));
    }
}
