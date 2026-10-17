# Data — Content & Player Schema
> Read when: adding content types or changing persisted player state. Keep this the single schema reference; update it in the same commit as the code.

## Content data (`content/`, static, versioned)
Loaded by `Game.Shared.Content.ContentLoader` (Newtonsoft, unknown fields = error, then validation). Models: `shared/Content/ContentModels.cs`. Percents are whole numbers.
| File | Holds | Status |
|---|---|---|
| `version.json` | `contentVersion` (A8) | M1 |
| `stats.json` | Stat tiers Low/Medium/High (BAL-5), rarity % (BAL-6), rank % + rank-up costs (BAL-9), levelsPerRank (CHM-7), maxRank (BAL-10), xpPerLevel (BAL-11), maxTweakPercent (BAL-7) | M1 |
| `classes.json` | Per class: badge item, growth % per level (BAL-8), bonus per rank-up (BAL-9) | M1 |
| `champions.json` | id, name, class, rarity, stat shape (tier per stat), tweakPercent | M1 |
| `combat.json` | defenseK, spreadPercent (BAL-2), exposedCritPercent (BAL-20), critCapPercent, roundCap (CMB-43), aiHealSelf (CMB-41, D-86); `effects`: bleedAttackPercent, attack/defensePercent, speed, accuracy (CMB-40), wardRounds | M2a |
| `skills.json` | `kits[]`: champion id + `skills[]` S1..S3 (all required) | M2a |
| `enemies.json` | tiers hp/atk/def/spd (BAL-16; 0/missing = undefined), growthPercent, eliteHp/AttackPercent (BAL-17), critChance/critDamage defaults (D-76); `enemies[]`: id, name, role, elite, slot Front/Back, shape (4 tiers), optional critChance/critDamage override, `skills[]` (S1 required) | M2a |

**Skill** (champions + enemies, CMB-27): slot S1/S2/S3 · reach Melee/Ranged/None · area Single/Splash/Row (enemy side, needs Melee/Ranged) or Self/Ally/AllAllies (reach None) · dive · cooldown (S1 = 0, S2/S3 1..roundCap) · damagePercent · critChanceBonus · healPercent + healOf Caster/Target · wardPercent (caster max HP) · cleanse · `effects[]` {type (CMB-40, not Ward), rounds, on Target/Self} · special {type Fade, rounds} · ai {when Always/SelfDamaged/AllyHpAtOrBelow, percent} (enemies, CMB-41). Enum fields start at Unset; missing = load error.

Planned: `gear.json`, `stages.json`, `chapters.json`, `dungeons.json`, `items.json`.

## Player data (server)
| Entity | Key fields | Status |
|---|---|---|
| — | (none yet) | — |
