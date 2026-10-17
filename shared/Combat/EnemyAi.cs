using System.Collections.Generic;
using System.Linq;
using Game.Shared.Content;

namespace Game.Shared.Combat
{
    /// <summary>What an enemy will do this round (CMB-26 intent icon).</summary>
    public sealed class Intent
    {
        public SkillDef Skill { get; }
        public FightUnit Target { get; }
        public Intent(SkillDef skill, FightUnit target) { Skill = skill; Target = target; }
    }

    /// <summary>
    /// CMB-41 / D-74 enemy AI. Intent is fixed at round start. Special skill (S3, then S2) when ready
    /// and its <see cref="AiRule"/> holds, else S1. Melee → nearest hero; ranged/Dive → lowest HP %,
    /// tie → Front-most. Heals pick the lowest HP % ally under the threshold. If the target is invalid
    /// when acting, the same skill retargets by the same rule (else a fresh plan).
    /// </summary>
    public static class EnemyAi
    {
        public static Intent? Plan(Fight fight, FightUnit unit)
        {
            foreach (var skill in unit.Skills.Where(s => s.Slot != SkillSlot.S1).OrderByDescending(s => s.Slot))
            {
                if (!unit.CanUse(skill) || !Wants(fight, unit, skill)) continue;
                var target = PickTarget(fight, unit, skill);
                if (target != null) return new Intent(skill, target);
            }
            var s1 = unit.Skill(SkillSlot.S1);
            if (!unit.CanUse(s1)) return null; // e.g. a Herald that already rang (D-97)
            var t1 = PickTarget(fight, unit, s1);
            return t1 == null ? null : new Intent(s1, t1);
        }

        /// <summary>Plays the unit's turn from its intent. Returns the hits (empty if nothing to do).</summary>
        public static IReadOnlyList<Hit> Act(Fight fight, FightUnit unit)
        {
            var plan = fight.IntentOf(unit);
            if (plan != null && !IsValid(fight, unit, plan))
            {
                var retarget = unit.CanUse(plan.Skill) ? PickTarget(fight, unit, plan.Skill) : null;
                plan = retarget != null ? new Intent(plan.Skill, retarget) : Plan(fight, unit);
            }
            plan ??= Plan(fight, unit);
            return plan == null ? new List<Hit>() : fight.UseSkill(unit, plan.Skill, plan.Target);
        }

        private static bool IsValid(Fight fight, FightUnit unit, Intent plan) =>
            !plan.Target.IsDown && unit.CanUse(plan.Skill) && Targeting.Valid(fight, unit, plan.Skill).Contains(plan.Target);

        private static bool Wants(Fight fight, FightUnit unit, SkillDef skill)
        {
            switch (skill.Ai?.When ?? AiWhen.Always)
            {
                case AiWhen.SelfDamaged: return unit.Hp < unit.MaxHp;
                case AiWhen.AllyHpAtOrBelow: return HealCandidates(fight, unit, skill).Any();
                default: return true;
            }
        }

        private static FightUnit? PickTarget(Fight fight, FightUnit unit, SkillDef skill)
        {
            var valid = Targeting.Valid(fight, unit, skill);
            switch (skill.Area)
            {
                case Area.Self:
                case Area.AllAllies:
                    return unit;
                case Area.Ally:
                    return LowestHp(HealCandidates(fight, unit, skill));
            }
            if (skill.Reach == Reach.Melee && !skill.Dive) return valid.FirstOrDefault(); // nearest
            return LowestHp(valid);
        }

        /// <summary>
        /// Allies a heal may pick: under the AI threshold (if any). The caster only if
        /// combat.json aiHealSelf (D-89: true).
        /// </summary>
        private static IEnumerable<FightUnit> HealCandidates(Fight fight, FightUnit unit, SkillDef skill)
        {
            var rule = skill.Ai;
            return Targeting.Valid(fight, unit, skill).Where(a =>
                (fight.Db.Combat.AiHealSelf || a != unit) &&
                (rule?.When != AiWhen.AllyHpAtOrBelow || a.Hp * 100L <= (long)rule.Percent * a.MaxHp));
        }

        private static FightUnit? LowestHp(IEnumerable<FightUnit> units) =>
            units.OrderBy(u => (decimal)u.Hp / u.MaxHp).ThenBy(u => u.Line).FirstOrDefault();
    }
}
