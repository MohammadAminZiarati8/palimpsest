using System;

namespace Game.Shared.Content
{
    /// <summary>One value of type T per <see cref="Stat"/>. Missing JSON fields stay default.</summary>
    public sealed class StatSet<T>
    {
        public T Hp { get; set; } = default!;
        public T Attack { get; set; } = default!;
        public T Defense { get; set; } = default!;
        public T Speed { get; set; } = default!;
        public T CritChance { get; set; } = default!;
        public T CritDamage { get; set; } = default!;
        public T Accuracy { get; set; } = default!;

        public T this[Stat stat]
        {
            get => stat switch
            {
                Stat.Hp => Hp,
                Stat.Attack => Attack,
                Stat.Defense => Defense,
                Stat.Speed => Speed,
                Stat.CritChance => CritChance,
                Stat.CritDamage => CritDamage,
                Stat.Accuracy => Accuracy,
                _ => throw new ArgumentOutOfRangeException(nameof(stat)),
            };
            set
            {
                switch (stat)
                {
                    case Stat.Hp: Hp = value; break;
                    case Stat.Attack: Attack = value; break;
                    case Stat.Defense: Defense = value; break;
                    case Stat.Speed: Speed = value; break;
                    case Stat.CritChance: CritChance = value; break;
                    case Stat.CritDamage: CritDamage = value; break;
                    case Stat.Accuracy: Accuracy = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(stat));
                }
            }
        }
    }

    public static class Stats
    {
        public static readonly Stat[] All =
            { Stat.Hp, Stat.Attack, Stat.Defense, Stat.Speed, Stat.CritChance, Stat.CritDamage, Stat.Accuracy };

        /// <summary>Stats enemies take from a tier (BAL-16); the rest are fixed defaults (BAL-17).</summary>
        public static readonly Stat[] EnemyTiered = { Stat.Hp, Stat.Attack, Stat.Defense, Stat.Speed };

        /// <summary>HP/Atk/Def: scaled by rarity (BAL-6), level growth (BAL-8) and rank (BAL-9).</summary>
        public static bool IsCore(Stat stat) => stat == Stat.Hp || stat == Stat.Attack || stat == Stat.Defense;
    }
}
