using System.Collections.Generic;
using System.Linq;
using Game.Shared.Content;

namespace Game.Shared.Combat
{
    /// <summary>Who a skill may target and who it hits (CMB-12, CMB-13, CMB-16, D-78).</summary>
    public static class Targeting
    {
        public static Side Opposite(Side side) => side == Side.Heroes ? Side.Enemies : Side.Heroes;

        /// <summary>
        /// Units the actor may pick. Melee: the nearest enemy on each side of the actor in the row
        /// (CMB-12: allies don't block, enemies do). Ranged or Dive: any enemy on the field.
        /// Single skills skip Faded units, and a Faded unit doesn't block melee (D-85).
        /// Reach None: Self = caster; Ally / AllAllies = any unit on the caster's side (caster included).
        /// </summary>
        public static IReadOnlyList<FightUnit> Valid(Fight fight, FightUnit actor, SkillDef skill)
        {
            switch (skill.Area)
            {
                case Area.Self: return new[] { actor };
                case Area.Ally:
                case Area.AllAllies: return fight.Line(actor.Side).ToList();
            }
            IEnumerable<FightUnit> foes = fight.Line(Opposite(actor.Side));
            if (skill.Area == Area.Single) foes = foes.Where(u => !u.IsFaded);
            if (skill.Reach == Reach.Ranged || skill.Dive) return foes.ToList();
            if (skill.Reach != Reach.Melee) return new List<FightUnit>();
            var list = foes.ToList();
            var nearest = new List<FightUnit>();
            var left = list.Where(u => u.Pos < actor.Pos).OrderByDescending(u => u.Pos).FirstOrDefault();
            var right = list.Where(u => u.Pos > actor.Pos).OrderBy(u => u.Pos).FirstOrDefault();
            if (left != null) nearest.Add(left);
            if (right != null) nearest.Add(right);
            return nearest;
        }

        /// <summary>
        /// Units hit when the skill lands on <paramref name="primary"/>. Single = target;
        /// Splash = target + the next unit behind it (far side from the attacker), else the next on the
        /// near side, only if on the target's side (D-78, never allies); Row = all enemies.
        /// Without an attacker, "behind" = behind the target's back.
        /// </summary>
        public static IReadOnlyList<FightUnit> Hits(Fight fight, SkillDef skill, FightUnit primary, FightUnit? actor = null)
        {
            var line = fight.Line(primary.Side);
            switch (skill.Area)
            {
                case Area.Single:
                    return new[] { primary };
                case Area.Splash:
                    int dir = actor != null && actor.Side != primary.Side
                        ? (primary.Pos > actor.Pos ? 1 : -1)
                        : -primary.Facing;
                    var row = fight.Row;
                    foreach (int p in new[] { primary.Pos + dir, primary.Pos - dir })
                        if (p >= 0 && p < row.Count && row[p].Side == primary.Side)
                            return new[] { primary, row[p] };
                    return new[] { primary };
                case Area.Row:
                case Area.AllAllies:
                    return line.ToList();
                case Area.Self:
                case Area.Ally:
                    return new[] { primary };
                default:
                    throw new System.NotSupportedException($"Skill area {skill.Area} is not set");
            }
        }

        /// <summary>
        /// CMB-15 Exposed = the attacker stands behind the target's back, melee and ranged alike (D-102).
        /// A Gutter unit has no safe side: every enemy hit on it is Exposed (D-103).
        /// </summary>
        public static bool IsExposed(FightUnit attacker, FightUnit target)
        {
            if (attacker.Side == target.Side || attacker.Pos < 0 || target.Pos < 0) return false;
            if (target.Spot == Spot.Gutter) return true;
            return (attacker.Pos > target.Pos ? 1 : -1) == -target.Facing;
        }
    }
}
