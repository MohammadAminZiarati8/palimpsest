using System.Collections.Generic;
using Game.Shared.Content;

namespace Game.Shared.Combat
{
    public enum Side { Heroes, Enemies }

    /// <summary>One champion or enemy in a fight. Position is owned by <see cref="Fight"/>.</summary>
    public sealed class FightUnit
    {
        /// <summary>Unique within the fight, in setup order (heroes first).</summary>
        public int Id { get; }
        public Side Side { get; }
        /// <summary>Champion or enemy content id.</summary>
        public string DefId { get; }
        public string Name { get; }
        /// <summary>Base stats from StatCalculator / EnemyStatCalculator. Effects (T4) modify on top.</summary>
        public StatSet<int> Stats { get; }
        public IReadOnlyList<SkillDef> Skills { get; }
        public int MaxHp { get; }
        public int Hp { get; internal set; }
        public bool IsDown => Hp <= 0;
        /// <summary>CMB-11: 0 = Front (next to the centre), counting back. -1 once Down (slot freed, CMB-14).</summary>
        public int Line { get; internal set; }

        /// <summary>BAL-3: Speed only sets turn order. Status effects (T4) will adjust this.</summary>
        public int Speed => Stats.Speed;

        internal FightUnit(int id, Side side, string defId, string name, StatSet<int> stats, IReadOnlyList<SkillDef> skills)
        {
            Id = id;
            Side = side;
            DefId = defId;
            Name = name;
            Stats = stats;
            Skills = skills;
            MaxHp = stats.Hp;
            Hp = stats.Hp;
        }

        public override string ToString() => $"{Name}#{Id}({Side} L{Line} {Hp}/{MaxHp})";
    }
}
