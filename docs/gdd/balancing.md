# Balancing — all values PLACEHOLDER
> Read when: seeding content data. Once values live in `content/`, the data files win and this file is just the origin.
> Demo numbers (GDD-5) Decided for the demo (D-51..61); all may change after the demo.

## Demo — combat math
- **BAL-1** Scale: Medium. Rare Lv1 average ≈ HP 1,000 · Atk 100 · Def 50 · Spd 100 (D-51).
- **BAL-2** Damage = Attack × skill % × 100 / (100 + Defense) (D-52). `100` = config `defenseK`. Crit: × (1 + Crit Damage). Random spread ±5% (uniform). Round half-away-from-zero, min 1 (D-79).
  | Defense | 50 | 100 | 200 |
  |---|---|---|---|
  | Blocked | 33% | 50% | 67% |
- **BAL-20** Exposed (CMB-15): +25% crit chance, additive, cap 100% (D-75).
- **BAL-3** Speed only sets turn order (CMB-25); keep values ~90–120. Turn-order rule lives in one swappable component; may change after the demo (e.g. turn meter → wider Speed range) (D-51).
- **BAL-4** Splash hits both targets at the listed %. Heals/Wards from Support scale with caster max HP.

## Demo — hero stats
- **BAL-5** Stat shape → Lv1 value (Rare = base) (D-53):
  | Stat | Low | Medium | High |
  |---|---|---|---|
  | HP | 800 | 1,000 | 1,300 |
  | Attack | 80 | 100 | 125 |
  | Defense | 35 | 50 | 70 |
  | Speed | 94 | 100 | 108 |
  | Crit Chance | 5% | 10% | 20% |
  | Crit Damage | +40% | +50% | +70% |
  | Accuracy | 0 | 10 | 20 |
- **BAL-6** Rarity multiplies HP/Atk/Def only: Common ×0.85 · Rare ×1.0 · Epic ×1.15 · Legendary ×1.3 (D-53).
- **BAL-7** Per-hero tweak: any stat ±10% max, in content data. Demo uses one: White Snake HP +5% (D-54).
- Resulting Lv1 stats (Accuracy 10 for all):
  | Hero | HP | Atk | Def | Spd | Crit % | Crit Dmg |
  |---|---|---|---|---|---|---|
  | c01 Margo (Warrior, Epic) | 1,495 | 144 | 58 | 100 | 5% | +50% |
  | c02 Pell (Assassin, Rare) | 800 | 125 | 35 | 108 | 20% | +70% |
  | c03 Libby (Mage, Rare) | 800 | 125 | 35 | 108 | 10% | +50% |
  | c04 White Snake (Support, Epic) | 1,570 | 92 | 58 | 108 | 5% | +40% |

## Demo — level & rank
- **BAL-8** Class growth per level, linear, % of Lv1 value (Q-18 placeholder, D-55). Speed/Crit don't grow per level.
  | Class | HP | Atk | Def |
  |---|---|---|---|
  | Warrior | +9% | +9% | +7% |
  | Assassin | +6% | +10% | +6% |
  | Mage | +6% | +10% | +6% |
  | Support | +10% | +6% | +8% |
  - Formula: stat(L) = Lv1 × (1 + (L−1) × growth) × rank multiplier. E.g. Margo Atk Lv10 ≈ 261.
- **BAL-9** Rank-up (D-56): HP/Atk/Def ×1.10 per rank (compounding). Class bonus per rank: Warrior +5% Crit Dmg · Assassin +2% Crit Chance · Mage +2 Spd · Support +2 Spd.
  | Rank-up | Class badges | Gold | New max lvl |
  |---|---|---|---|
  | R1 → R2 | 5 | 1,000 | 20 |
  | R2 → R3 | 10 | 5,000 | 30 (not in demo) |
- **BAL-10** Demo cap: Rank 2 / Lv 20 (D-56).
- **BAL-11** XP to next level = 100 × current level (Lv10 total 4,500; Lv15 total 10,500) (D-57).
- **BAL-12** Story heroes (rescue, tutorial Recall) join at the level and rank of the player's lowest hero. Summoned heroes start Lv1 (D-58).

## Demo — stage rewards
- **BAL-13** Stage XP goes to every hero in the stage team (incl. waiting at the door or Down). First clear (D-57, D-59):
  | Stage | XP | Gold | Badges | Team ends ~ |
  |---|---|---|---|---|
  | Prologue 1 / 2 | 150 / 200 | 100 / 100 | — | Lv 3 |
  | Seedbed 1 / 2 / 3 | 1,000 / 1,300 / 1,900 | 800 / 1,000 / 1,200 | 2 of each class | Lv 5 / 7 / 10 → rank up |
  | Seedbed 4 / 5 / 6 | 1,800 / 2,000 / 2,200 | 1,000 each | 2 of each class | Lv 11 / 13 / 15 |
  - "Each class" = Warrior, Assassin, Support, Mage badge.
- **BAL-14** Replay: 50% XP, 50% gold, no badges. Stars = score only in the demo (D-59).

## Demo — status effects (CMB-40, D-60)
| Effect | Strength |
|---|---|
| Stun | Skip next turn |
| Bleed | Per round: 30% of the applier's Attack, ignores Defense |
| Seal | One skill (S2 or S3) locked |
| Ward | Absorbs damage (size per skill); 2 rounds or until broken |
| Attack up/down | ±25% |
| Defense up/down | ±30% |
| Speed up/down | ±15 Speed |
| Accuracy up/down | ±20 (unused in demo) |
- **BAL-15** Same effect doesn't stack; re-applying refreshes duration. Durations tick at the end of the affected unit's turn.

## Demo — hero skills (base level, D-30, D-61)
| Hero | S1 | S2 (cd 3) | S3 (cd 4) |
|---|---|---|---|
| Margo | Nib Slash 100% | Bold Stroke 90% Splash | Exclamation Leap! 200% Dive + self Defense down 1 |
| Pell | Fine Line 90%, +15% crit chance | Strikethrough 150% + Bleed 2 | Fade: untargetable by Single 1 round; next attack sure crit (no damage, uses turn) |
| Libby | Seed Scatter 60% Splash | Bramble Line 70% Row + Bleed 2 | Rootbind 40% Splash + Stun 1 + Speed down 2 |
| White Snake | Jade Needle 80% + Attack down 1 | Herbal Remedy: heal all 20% of her max HP + cleanse 1 debuff each | Coiled Guard: Ward 15% of her max HP on all + Defense up 2 |

## Demo — Censors (10-enemies.md, D-61)
- **BAL-16** Tier → Lv1 value:
  | Tier | HP | Atk | Def | Spd |
  |---|---|---|---|---|
  | ▼ | 250 | 70 | 20 | 92 |
  | ● | 400 | 100 | 35 | 99 |
  | ▲ | 600 | 130 | 50 | — |
- **BAL-17** Enemy growth +9% HP/Atk/Def per level, linear like BAL-8 (D-81). Elite: extra HP ×3, Atk ×1.5. Evasion 0 in the demo. All enemies Crit Chance 5%, Crit Damage +50% (per-enemy override in content, D-76).
- **BAL-18** Kits:
  | Censor | Skills |
  |---|---|
  | C1 Blotter | Smear 100% · Soak: Ward self 20% max HP (cd 3) |
  | C2 Inkslinger | Ink shot 100% · Splatter 70% Splash (cd 3) |
  | C3 Herald | Ring in 3 rounds → 1–2 Censors written in (per stage) + Notice +2 |
  | C4 Corrector | Hit 80% · Correction fluid: heal ally 25% of their max HP (cd 2) |
  | C5 Editor (Elite) | Cut 130% · Final say: all allies Attack up 2 (cd 4) |
- **BAL-19** Enemy level = expected team level + 1: Prologue 1/2 → Lv 1/2 · Seedbed 1–6 → Lv 4/6/8/10/12/14.
- Target feel (hand math, tune in playtest): Seedbed 1 Inkslinger dies in ~1 team round, Blotter in 2–3; Inkslinger hit ≈ 10% of Pell's HP.

## Demo — stage flow (M2b)
- **BAL-21** Door recovery (CMB-9): each scene a hero waits at the door heals 15% of its max HP (D-91, config).
- **BAL-22** Room option % (CMB-19): 50% + 5% per level the hero is above the scene level (−5% per level below; party options use the party's average level). Assassin +15% on Strike Out / Between the Lines, Support +15% on Spin a Tale. Clamp 5–95%. Rank and stars don't count in the demo (D-92, config).

## Demo — not reviewed yet (Proposed, config values)

## Post-demo examples (older placeholders)
- XP potions: Small = 100 XP, Big = 1,000 XP.
- Champion rank → max level: R1 10, R2 20, R3 30, R4 40, R5 50, R6 60.

Skill example (Shadow Strike): L1 100% dmg · L2 115% · L3 adds Bleed 2 turns · L4 cooldown −1 · L5 130%.
Awakening example (Kaveh): ⭐3 Shadow Strike hits 2 enemies · ⭐5 a second skill also stuns 1 turn.

Gear example — Shiny Battle Axe (Epic, Warrior):
| Rank | How | Max lvl | Attack | Crit % | Crit Dmg |
|---|---|---|---|---|---|
| ★1 | Drop | 10 | 100 | 5% | 20% |
| ★2 | Merge | 20 | 130 | 7% | 25% |
| ★3 | Merge | 30 | 170 | 9% | 30% |
| ★4 | Merge | 40 | 220 | 11% | 40% |
| ★5 | Merge | 50 | 280 | 14% | 50% |

Warrior axe focuses: Shiny Battle Axe (Crit % + Crit Dmg) · Iron Cleaver (Attack % + Accuracy) · Blood Axe (Lifesteal + HP).
