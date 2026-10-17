namespace Game.Shared.Content
{
    public enum Stat { Hp, Attack, Defense, Speed, CritChance, CritDamage, Accuracy }

    /// <summary>CLS-1: exactly six classes. Demo content defines four.</summary>
    public enum HeroClass { Guardian, Warrior, Assassin, Ranger, Support, Mage }

    /// <summary>CHM-1</summary>
    public enum Rarity { Common, Rare, Epic, Legendary }

    /// <summary>BAL-5 stat shape. Unset = missing in content (rejected by the loader).</summary>
    public enum StatTier { Unset, Low, Medium, High }
}

namespace Game.Shared.Content
{
    // Combat content (M2a-T1). Unset = missing in content (rejected by the loader).

    /// <summary>CMB-27: S1 basic, S2/S3 active.</summary>
    public enum SkillSlot { Unset, S1, S2, S3 }

    /// <summary>CMB-27 Reach. None = self/ally skill that hits no enemy.</summary>
    public enum Reach { Unset, Melee, Ranged, None }

    /// <summary>CMB-13 enemy-side areas, plus Self / one Ally / AllAllies for support skills.</summary>
    public enum Area { Unset, Single, Splash, Row, Self, Ally, AllAllies }

    /// <summary>CMB-40 status effects.</summary>
    public enum StatusEffect
    {
        Unset, Stun, Bleed, Seal, Ward,
        AttackUp, AttackDown, DefenseUp, DefenseDown, SpeedUp, SpeedDown, AccuracyUp, AccuracyDown,
    }

    /// <summary>Who an effect lands on: the skill's targets, or the caster.</summary>
    public enum EffectOn { Target, Self }

    /// <summary>Whose max HP a heal scales with (BAL-4 Support = caster; Corrector = target).</summary>
    public enum HealBasis { Unset, Caster, Target }

    /// <summary>Kit-specific actions that aren't shared status effects. Fade = Pell S3. Ring = Herald countdown (ENM-9).</summary>
    public enum SpecialType { None, Fade, Ring }

    /// <summary>CMB-41: when the enemy AI may pick a special skill.</summary>
    public enum AiWhen { Always, SelfDamaged, AllyHpAtOrBelow }

    /// <summary>ENM roles (gdd/10-enemies.md).</summary>
    public enum EnemyRole { Unset, FrontLine, Ranged, Herald, Healer }

    public enum RowSlot { Unset, Front, Back }
}
