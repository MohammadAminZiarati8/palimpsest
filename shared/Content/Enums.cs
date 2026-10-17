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
