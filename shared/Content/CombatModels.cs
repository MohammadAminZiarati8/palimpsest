using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Shared.Content
{
    // JSON shapes of combat content (M2a-T1). Schema reference: docs/tech/data.md.

    /// <summary>content/combat.json: BAL-2, BAL-20, CMB-43 round cap, CMB-40 strengths.</summary>
    public sealed class CombatRules
    {
        [JsonProperty("_doc")] public string? Doc { get; set; }
        public int DefenseK { get; set; }
        public int SpreadPercent { get; set; }
        public int ExposedCritPercent { get; set; }
        public int CritCapPercent { get; set; }
        public int RoundCap { get; set; }
        public EffectStrengths Effects { get; set; } = new EffectStrengths();
    }

    /// <summary>CMB-40 table in balancing.md.</summary>
    public sealed class EffectStrengths
    {
        public int BleedAttackPercent { get; set; }
        public int AttackPercent { get; set; }
        public int DefensePercent { get; set; }
        public int Speed { get; set; }
        public int Accuracy { get; set; }
        public int WardRounds { get; set; }
    }

    /// <summary>One skill, shared by champions and enemies (CMB-27, BAL-18).</summary>
    public sealed class SkillDef
    {
        public SkillSlot Slot { get; set; }
        public string Name { get; set; } = "";
        public Reach Reach { get; set; }
        public Area Area { get; set; }
        /// <summary>CMB-16: melee may target any enemy.</summary>
        public bool Dive { get; set; }
        /// <summary>Rounds (CMB-42). 0 for S1.</summary>
        public int Cooldown { get; set; }
        public int DamagePercent { get; set; }
        public int CritChanceBonus { get; set; }
        public int HealPercent { get; set; }
        public HealBasis HealOf { get; set; }
        /// <summary>% of caster max HP (BAL-4); lasts combat.effects.wardRounds.</summary>
        public int WardPercent { get; set; }
        /// <summary>Debuffs removed per target (newest first, CMB-43).</summary>
        public int Cleanse { get; set; }
        public List<EffectApply> Effects { get; set; } = new List<EffectApply>();
        public SpecialDef? Special { get; set; }
        /// <summary>Enemies only (CMB-41). Null = always usable when ready.</summary>
        public AiRule? Ai { get; set; }
    }

    public sealed class EffectApply
    {
        public StatusEffect Type { get; set; }
        public int Rounds { get; set; }
        public EffectOn On { get; set; }
    }

    /// <summary>Fade: untargetable by Single skills for Rounds; next attack is a sure crit.</summary>
    public sealed class SpecialDef
    {
        public SpecialType Type { get; set; }
        public int Rounds { get; set; }
    }

    public sealed class AiRule
    {
        public AiWhen When { get; set; }
        /// <summary>HP % threshold for AllyHpAtOrBelow.</summary>
        public int Percent { get; set; }
    }

    /// <summary>content/skills.json: champion kits.</summary>
    public sealed class SkillFile
    {
        [JsonProperty("_doc")] public string? Doc { get; set; }
        public List<ChampionKit> Kits { get; set; } = new List<ChampionKit>();
    }

    public sealed class ChampionKit
    {
        public string Champion { get; set; } = "";
        public List<SkillDef> Skills { get; set; } = new List<SkillDef>();
    }

    /// <summary>content/enemies.json: BAL-16, BAL-17, BAL-18, ENM-15.</summary>
    public sealed class EnemyFile
    {
        [JsonProperty("_doc")] public string? Doc { get; set; }
        /// <summary>Hp/Attack/Defense/Speed only. A tier value of 0 = not defined (Speed High).</summary>
        public StatSet<TierValues> Tiers { get; set; } = new StatSet<TierValues>();
        public int GrowthPercent { get; set; }
        public int EliteHpPercent { get; set; }
        public int EliteAttackPercent { get; set; }
        public int CritChance { get; set; }
        public int CritDamage { get; set; }
        public List<EnemyDef> Enemies { get; set; } = new List<EnemyDef>();
    }

    public sealed class EnemyDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public EnemyRole Role { get; set; }
        public bool Elite { get; set; }
        public RowSlot Slot { get; set; }
        /// <summary>Tier per Hp/Attack/Defense/Speed (gdd/10-enemies.md).</summary>
        public StatSet<StatTier> Shape { get; set; } = new StatSet<StatTier>();
        /// <summary>D-76 per-enemy overrides; null = file default.</summary>
        public int? CritChance { get; set; }
        public int? CritDamage { get; set; }
        public List<SkillDef> Skills { get; set; } = new List<SkillDef>();
    }
}
