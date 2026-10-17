using System.Collections.Generic;
using System.Linq;
using Game.Shared.Content;

namespace Game.Shared.Combat
{
    /// <summary>Who a damaging skill may target and who it hits (CMB-12, CMB-13, CMB-16, D-78).</summary>
    public static class Targeting
    {
        public static Side Opposite(Side side) => side == Side.Heroes ? Side.Enemies : Side.Heroes;

        /// <summary>
        /// Enemies the actor may pick. Melee: nearest enemy (CMB-12; in M2a that is the enemy Front,
        /// flank sides arrive with Margin/Gutter in M2b). Ranged or Dive: any enemy on the field.
        /// </summary>
        public static IReadOnlyList<FightUnit> Valid(Fight fight, FightUnit actor, SkillDef skill)
        {
            var foes = fight.Line(Opposite(actor.Side));
            if (skill.Reach == Reach.Ranged || skill.Dive) return foes.ToList();
            if (skill.Reach == Reach.Melee) return foes.Take(1).ToList();
            return new List<FightUnit>();
        }

        /// <summary>
        /// Units hit when the skill lands on <paramref name="primary"/>. Single = target;
        /// Splash = target + the enemy behind it, else the one in front (D-78, never allies); Row = all enemies.
        /// </summary>
        public static IReadOnlyList<FightUnit> Hits(Fight fight, SkillDef skill, FightUnit primary)
        {
            var line = fight.Line(primary.Side);
            switch (skill.Area)
            {
                case Area.Single:
                    return new[] { primary };
                case Area.Splash:
                    int behind = primary.Line + 1, front = primary.Line - 1;
                    if (behind < line.Count) return new[] { primary, line[behind] };
                    if (front >= 0) return new[] { primary, line[front] };
                    return new[] { primary };
                case Area.Row:
                    return line.ToList();
                default:
                    throw new System.NotSupportedException($"Area {skill.Area} is not an enemy-side area (T5)");
            }
        }

        /// <summary>
        /// CMB-15 Exposed = hit from the target's rear. Needs units behind a line
        /// (Margin entry, Bleed-through, Gutter: CMB-17/18, M2b); in M2a nobody stands there.
        /// </summary>
        public static bool IsExposed(FightUnit attacker, FightUnit target) => false;
    }
}
