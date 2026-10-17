using Game.Shared.Combat;
using Game.Shared.Content;

namespace Game.Shared.Tests.Combat;

public class KitTests
{
    private static Fight NewFight(string[] heroes, string[] enemies) =>
        new(TestContent.Db, new FightSetup
        {
            Heroes = heroes.Select(id => new HeroEntry { ChampionId = id }).ToList(),
            Enemies = enemies.Select(id => new EnemyEntry { EnemyId = id }).ToList(),
            Seed = 11,
        });

    private static FightUnit Hero(Fight f, string id) => f.Line(Side.Heroes).Single(u => u.DefId == id);
    private static void Idle(Fight f, FightUnit u) { }

    // --- White Snake (c04): heal all + cleanse, Ward all + Defense up (BAL-4) ---

    [Fact]
    public void HerbalRemedy_HealsAll20PercentOfCasterMaxHp_AndCleansesOne()
    {
        var f = NewFight(["c01", "c04"], ["C1"]);
        var (margo, snake, e) = (Hero(f, "c01"), Hero(f, "c04"), f.Line(Side.Enemies)[0]);
        f.Damage(margo, 500);
        f.Damage(snake, 10);
        f.ApplyEffect(margo, StatusEffect.AttackDown, 2, e);
        f.ApplyEffect(margo, StatusEffect.Bleed, 2, e);
        int heal = (int)Math.Round(snake.MaxHp * 0.2m, MidpointRounding.AwayFromZero);

        f.UseSkill(snake, snake.Skill(SkillSlot.S2), snake);

        Assert.Equal(margo.MaxHp - 500 + heal, margo.Hp);
        Assert.Equal(snake.MaxHp, snake.Hp); // capped at max
        Assert.Equal([StatusEffect.AttackDown], margo.Effects.Select(x => x.Type)); // newest (Bleed) removed
        Assert.Equal(3, snake.Cooldown(SkillSlot.S2));
    }

    [Fact]
    public void CoiledGuard_WardsAll15PercentOfCasterMaxHp_AndDefenseUp()
    {
        var f = NewFight(["c01", "c04"], ["C1"]);
        var (margo, snake) = (Hero(f, "c01"), Hero(f, "c04"));
        f.UseSkill(snake, snake.Skill(SkillSlot.S3), margo);
        int ward = (int)Math.Round(snake.MaxHp * 0.15m, MidpointRounding.AwayFromZero);
        foreach (var u in new[] { margo, snake })
        {
            var w = u.Effect(StatusEffect.Ward)!;
            Assert.Equal((ward, TestContent.Db.Combat.Effects.WardRounds), (w.Amount, w.Rounds));
            Assert.Equal(2, u.Effect(StatusEffect.DefenseUp)!.Rounds);
        }
    }

    [Fact]
    public void SupportSkill_CannotTargetEnemies()
    {
        var f = NewFight(["c04"], ["C1"]);
        var snake = Hero(f, "c04");
        Assert.Throws<ArgumentException>(() => f.UseSkill(snake, snake.Skill(SkillSlot.S2), f.Line(Side.Enemies)[0]));
    }

    // --- Corrector (C4): heal one ally 25% of the target's max HP ---

    [Fact]
    public void CorrectionFluid_HealsOneAlly_OfTargetMaxHp()
    {
        var f = NewFight(["c01"], ["C1", "C4"]);
        var (blotter, corrector) = (f.Line(Side.Enemies)[0], f.Line(Side.Enemies)[1]);
        f.Damage(blotter, 400);
        f.Damage(corrector, 50);
        f.UseSkill(corrector, corrector.Skill(SkillSlot.S2), blotter);
        Assert.Equal(blotter.MaxHp - 400 + (int)Math.Round(blotter.MaxHp * 0.25m, MidpointRounding.AwayFromZero), blotter.Hp);
        Assert.Equal(corrector.MaxHp - 50, corrector.Hp); // only the one target
    }

    // --- Pell (c02) Fade: untargetable by Single, next attack sure crit ---

    [Fact]
    public void Fade_UntargetableBySingle_NotBySplashOrRow()
    {
        var f = NewFight(["c02", "c01"], ["C2"]);
        var (pell, margo, ink) = (Hero(f, "c02"), Hero(f, "c01"), f.Line(Side.Enemies)[0]);
        f.UseSkill(pell, pell.Skill(SkillSlot.S3), pell);
        Assert.True(pell.IsFaded);
        var single = new SkillDef { Reach = Reach.Ranged, Area = Area.Single };
        var splash = new SkillDef { Reach = Reach.Ranged, Area = Area.Splash };
        Assert.Equal([margo], Targeting.Valid(f, ink, single));
        Assert.Equal([pell, margo], Targeting.Valid(f, ink, splash));
    }

    [Fact]
    public void Fade_DoesNotBlockMelee_D85()
    {
        var f = NewFight(["c02", "c01"], ["C1"]);
        var (pell, margo, blotter) = (Hero(f, "c02"), Hero(f, "c01"), f.Line(Side.Enemies)[0]);
        f.UseSkill(pell, pell.Skill(SkillSlot.S3), pell);
        Assert.Equal([margo], Targeting.Valid(f, blotter, blotter.Skill(SkillSlot.S1)));
    }

    [Fact]
    public void Fade_NextAttackSureCrit_ThenSpent()
    {
        var f = NewFight(["c02"], ["C5"]);
        var (pell, editor) = (Hero(f, "c02"), f.Line(Side.Enemies)[0]);
        pell.Stats.CritChance = 0;
        var s1 = pell.Skill(SkillSlot.S1);
        f.UseSkill(pell, pell.Skill(SkillSlot.S3), pell);
        Assert.True(Assert.Single(f.UseSkill(pell, s1, editor)).Crit);
        // sure crit is spent on that attack
        Assert.False(pell.SureCrit);
    }

    [Fact]
    public void Fade_LastsUntilEndOfPellsNextTurn()
    {
        var f = NewFight(["c02"], ["C5"]);
        var (pell, editor) = (Hero(f, "c02"), f.Line(Side.Enemies)[0]);
        pell.Stats.Speed = 120; editor.Stats.Speed = 100;
        f.Step((fight, u) => fight.UseSkill(u, u.Skill(SkillSlot.S3), u));
        f.Step((fight, u) => Assert.Empty(Targeting.Valid(fight, u, u.Skill(SkillSlot.S1)))); // Editor can't hit her
        f.Step(Idle);
        Assert.False(pell.IsFaded);
    }

    // --- Margo (c01) S3 self Defense down (D-84) ---

    [Fact]
    public void ExclamationLeap_Dives_AndLowersOwnDefense()
    {
        var f = NewFight(["c01"], ["C1", "C2"]);
        var (margo, back) = (Hero(f, "c01"), f.Line(Side.Enemies)[1]);
        var s3 = margo.Skill(SkillSlot.S3);
        Assert.Contains(back, Targeting.Valid(f, margo, s3));
        int def = margo.Defense;
        f.UseSkill(margo, s3, back);
        Assert.True(margo.Defense < def);
    }

    // --- Shift (CMB-16) ---

    [Fact]
    public void Shift_SwapsWithAdjacentAlly()
    {
        var f = NewFight(["c01", "c02", "c03"], ["C1"]);
        var (margo, pell, libby) = (Hero(f, "c01"), Hero(f, "c02"), Hero(f, "c03"));
        f.Shift(pell, margo);
        Assert.Equal([pell, margo, libby], f.Line(Side.Heroes));
        Assert.Equal((0, 1), (pell.Line, margo.Line));
        Assert.Throws<ArgumentException>(() => f.Shift(pell, libby)); // not adjacent
        Assert.Throws<InvalidOperationException>(() => f.Shift(f.Line(Side.Enemies)[0], f.Line(Side.Enemies)[0]));
    }
}
