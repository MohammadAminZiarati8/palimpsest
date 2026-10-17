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

Planned: `skills.json`, `enemies.json`, `gear.json`, `stages.json`, `chapters.json`, `dungeons.json`, `items.json`.

## Player data (server)
| Entity | Key fields | Status |
|---|---|---|
| — | (none yet) | — |
