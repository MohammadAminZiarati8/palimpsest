using Game.Shared.Combat;
using Game.Shared.Content;

namespace Game.Shared.Tests.Combat;

public class DamageTests
{
    private static readonly CombatRules Rules = TestContent.Db.Combat;
    private static readonly ContentDb Db = TestContent.Db;

    private static Fight NewFight(string[] heroes, string[] enemies, ulong seed = 7) =>
        new(Db, new FightSetup
        {
            Heroes = heroes.Select(id => new HeroEntry { ChampionId = id }).ToList(),
            Enemies = enemies.Select(id => new EnemyEntry { EnemyId = id }).ToList(),
            Seed = seed,
        });

    private static SkillDef Skill(string enemyId, SkillSlot slot) => Db.Enemy(enemyId).Skills.Single(s => s.Slot == slot);

    // BAL-2 table: Def 50 / 100 / 200 blocks 33% / 50% / 67%.
    [Theory]
    [InlineData(50, 67)]
    [InlineData(100, 50)]
    [InlineData(200, 33)]
    public void DefenseTable_BAL2(int defense, int expected) =>
        Assert.Equal(expected, DamageCalc.Compute(Rules, 100, 100, defense, false, 0, 0));

    [Fact]
    public void SkillPercent_Crit_Spread_BAL2()
    {
        Assert.Equal(140, DamageCalc.Compute(Rules, 200, 70, 0, false, 0, 0));     // 200 x 70%
        Assert.Equal(150, DamageCalc.Compute(Rules, 100, 100, 0, true, 50, 0));    // crit x (1 + 50%)
        Assert.Equal(105, DamageCalc.Compute(Rules, 100, 100, 0, false, 0, 500));  // +5%
        Assert.Equal(95, DamageCalc.Compute(Rules, 100, 100, 0, false, 0, -500));  // -5%
    }

    [Fact]
    public void Rounding_HalfAwayFromZero_Min1_D79()
    {
        Assert.Equal(3, DamageCalc.Compute(Rules, 5, 50, 0, false, 0, 0)); // 2.5 -> 3
        Assert.Equal(1, DamageCalc.Compute(Rules, 1, 10, 1000, false, 0, 0));
    }

    [Fact]
    public void CritChance_ExposedAdditive_Capped_BAL20()
    {
        Assert.Equal(10, DamageCalc.CritChance(Rules, 10, 0, false));
        Assert.Equal(45, DamageCalc.CritChance(Rules, 10, 10, true));
        Assert.Equal(100, DamageCalc.CritChance(Rules, 90, 0, true));
    }

    [Fact]
    public void InkslingerHit_About10PercentOfPellHp()
    {
        var f = NewFight(["c02"], ["C2"]);
        var (pell, ink) = (f.Line(Side.Heroes)[0], f.Line(Side.Enemies)[0]);
        int dmg = DamageCalc.Compute(Rules, ink.Stats.Attack, Skill("C2", SkillSlot.S1).DamagePercent,
            pell.Stats.Defense, false, 0, 0);
        double share = (double)dmg / pell.MaxHp;
        Assert.InRange(share, 0.08, 0.12);
    }

    [Fact]
    public void Attack_RollsWithinSpread_AndApplies()
    {
        var f = NewFight(["c02"], ["C1"]);
        var (pell, blotter) = (f.Line(Side.Heroes)[0], f.Line(Side.Enemies)[0]);
        var s1 = Skill("C1", SkillSlot.S1);
        var hit = Assert.Single(f.Attack(blotter, s1, pell));
        int baseDmg = DamageCalc.Compute(Rules, blotter.Stats.Attack, s1.DamagePercent, pell.Stats.Defense,
            hit.Crit, blotter.Stats.CritDamage, 0);
        Assert.InRange(hit.Amount, baseDmg * 0.95 - 1, baseDmg * 1.05 + 1);
        Assert.Equal(pell.MaxHp - hit.Amount, pell.Hp);
    }

    [Fact]
    public void SameSeed_SameHits_A3()
    {
        int[] Play(ulong seed)
        {
            var f = NewFight(["c01", "c02", "c03"], ["C2"], seed);
            var ink = f.Line(Side.Enemies)[0];
            return Enumerable.Range(0, 20).Select(_ => f.Attack(ink, Skill("C2", SkillSlot.S1), f.Line(Side.Heroes)[0]))
                .TakeWhile(_ => !f.IsOver).SelectMany(h => h).Select(h => h.Amount * (h.Crit ? -1 : 1)).ToArray();
        }
        Assert.Equal(Play(99), Play(99));
    }

    [Fact]
    public void CritsHappen_AtRoughlyTheirChance()
    {
        var f = NewFight(["c01"], ["C5"], 3); // Editor: 1,800 HP outlasts 400 pokes
        var (margo, blotter) = (f.Line(Side.Heroes)[0], f.Line(Side.Enemies)[0]);
        blotter.Stats.Defense = 0;
        margo.Stats.CritChance = 50;
        var tiny = new SkillDef { Name = "poke", Reach = Reach.Melee, Area = Area.Single, DamagePercent = 1 };
        int crits = 0;
        for (int i = 0; i < 400; i++) crits += f.Attack(margo, tiny, blotter).Count(h => h.Crit);
        Assert.InRange(crits, 160, 240);
    }

    // --- Targeting (CMB-12, CMB-13, CMB-16, D-78) ---

    private static readonly SkillDef Melee = new() { Name = "m", Reach = Reach.Melee, Area = Area.Single, DamagePercent = 1 };
    private static readonly SkillDef Ranged = new() { Name = "r", Reach = Reach.Ranged, Area = Area.Single, DamagePercent = 1 };
    private static readonly SkillDef Dive = new() { Name = "d", Reach = Reach.Melee, Area = Area.Single, Dive = true, DamagePercent = 1 };

    [Fact]
    public void Melee_OnlyNearestEnemy_RangedAndDiveAny()
    {
        var f = NewFight(["c01", "c03"], ["C1", "C2", "C4"]);
        var (hero, e) = (f.Line(Side.Heroes)[1], f.Line(Side.Enemies));
        Assert.Equal([e[0]], Targeting.Valid(f, hero, Melee)); // allies don't block, enemies do
        Assert.Equal(e, Targeting.Valid(f, hero, Ranged));
        Assert.Equal(e, Targeting.Valid(f, hero, Dive));
        Assert.Throws<ArgumentException>(() => f.Attack(hero, Melee, e[2]));
    }

    [Fact]
    public void Melee_ReachesNewFront_AfterGapClose()
    {
        var f = NewFight(["c01"], ["C1", "C2"]);
        var (front, back) = (f.Line(Side.Enemies)[0], f.Line(Side.Enemies)[1]);
        f.Damage(front, front.Hp);
        Assert.Equal([back], Targeting.Valid(f, f.Line(Side.Heroes)[0], Melee));
    }

    [Fact]
    public void Splash_BehindElseFront_NeverAllies_D78()
    {
        var f = NewFight(["c01"], ["C1", "C2", "C4"]);
        var e = f.Line(Side.Enemies);
        var splash = new SkillDef { Area = Area.Splash };
        Assert.Equal([e[0], e[1]], Targeting.Hits(f, splash, e[0]));
        Assert.Equal([e[1], e[2]], Targeting.Hits(f, splash, e[1]));
        Assert.Equal([e[2], e[1]], Targeting.Hits(f, splash, e[2])); // nobody behind -> other side
        var solo = NewFight(["c01"], ["C1"]);
        Assert.Single(Targeting.Hits(solo, splash, solo.Line(Side.Enemies)[0]));
    }

    [Fact]
    public void Row_HitsAllEnemies_Single_HitsOne()
    {
        var f = NewFight(["c01"], ["C1", "C2", "C4"]);
        var e = f.Line(Side.Enemies);
        Assert.Equal(e, Targeting.Hits(f, new SkillDef { Area = Area.Row }, e[1]));
        Assert.Equal([e[1]], Targeting.Hits(f, new SkillDef { Area = Area.Single }, e[1]));
    }

    [Fact]
    public void Splash_RollsAllHitsBeforeGapClose()
    {
        var f = NewFight(["c03"], ["C2", "C2", "C2"]);
        var e = f.Line(Side.Enemies).ToArray();
        var hero = f.Line(Side.Heroes)[0];
        f.Damage(e[0], e[0].Hp - 1); // primary goes Down on the hit
        var splash = new SkillDef { Name = "s", Reach = Reach.Ranged, Area = Area.Splash, DamagePercent = 10 };
        var hits = f.Attack(hero, splash, e[0]);
        Assert.Equal([e[0], e[1]], hits.Select(h => h.Target));
        Assert.Equal(e[2].MaxHp, e[2].Hp);
    }
}
