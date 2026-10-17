using System.Collections.Generic;
using System.Linq;

namespace Game.Shared.Combat
{
    /// <summary>
    /// BAL-3 / D-51: the one swappable turn-order rule. Called at every round start (CMB-43)
    /// with the units on the field; returns who acts this round, in order.
    /// </summary>
    public interface ITurnOrder
    {
        IReadOnlyList<FightUnit> Build(IEnumerable<FightUnit> onField);
    }

    /// <summary>CMB-25 / CMB-43: Speed high → low; ties champion first, then Front-most.</summary>
    public sealed class SpeedTurnOrder : ITurnOrder
    {
        public static readonly SpeedTurnOrder Instance = new SpeedTurnOrder();

        public IReadOnlyList<FightUnit> Build(IEnumerable<FightUnit> onField) =>
            onField
                .OrderByDescending(u => u.Speed)
                .ThenBy(u => u.Side == Side.Heroes ? 0 : 1)
                .ThenBy(u => u.Line)
                .ToList();
    }
}
