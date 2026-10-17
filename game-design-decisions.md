# Hero Collector Game – Design Decisions (so far)

> **Purpose of this file:** This is the source summary of all design decisions made so far.
> It is meant to be used as input to create a full Game Design Document (GDD) in a new repository.
>
> **Notes for whoever builds the GDD (e.g. Claude Code):**
> - Treat sections marked **Decided** as final.
> - Treat sections marked **Open** as not decided yet. Do not invent final answers for them; list them as open questions in the GDD.
> - All numbers marked *(placeholder)* are examples for balancing later, not final values.
> - All class names, item names and champion names are placeholders. The game's setting/universe is not decided yet.
> - Suggested GDD structure: one file per system (e.g. `01-overview.md`, `02-campaign.md`, `03-champions.md`, `04-awakening.md`, `05-classes.md`, `06-gear.md`, `99-open-questions.md`) plus a `README.md` index.

---

## 1. Game Overview — Decided

- **Genre:** 2D hero-collecting (gacha) RPG.
- **Combat:** Same mechanics as *Assassin's Creed Rebellion* (possibly exactly the same).
- **Setting / universe:** **Not** Assassin's Creed. Setting is not decided yet (Open).
- **Design goal:** Keep champion building **simple and easy to understand**. Avoid complexity and random (RNG) systems where possible.
- **Game modes:** Campaign, Dungeons, Events, and more later.
- **Getting champions:**
  - Mainly from **summoning**.
  - Some champions are **rewards** for completing missions (example: get 3 stars on every campaign stage).

---

## 2. Campaign — Decided

- **No energy system.**
- Each stage gives a **1, 2 or 3 star** result. Rewards depend on how many stars the player got.
- Each **chapter** has a bigger, more valuable reward.
  - Chapter reward model is Open: either for clearing all stages with full stars, or milestone rewards based on total stars collected.
- Players **cannot finish the campaign in one day**. They need to make their champions stronger to progress.
- **Good tactics** should still let players push further than raw power alone.
- The campaign is **one-time content**: once all stars are collected, it is finished.
- Repeatable resource farming happens in **Dungeons** (to be designed).

### Campaign ideas — Proposed (not final)
- 3-star conditions based on tactics, not only HP (examples: "no champion dies", "win within X turns").
- Some stages have **class requirements** (example: "needs at least 1 Support", "no Guardians allowed").
- Some stages use **enemy types** that reward certain classes (example: shielded enemy → Warrior; enemy healer in back row → Assassin or Ranger; hard-hitting boss → Guardian).
- One **Legendary set piece** as an early chapter reward to introduce sets (only if sets are approved, see Gear).

---

## 3. Champion Building — Decided

Champions have these systems:

| System | Job | How it works |
|---|---|---|
| **Rarity** | The ceiling | Fixed at summon. Sets base stats and potential. Never changes. |
| **Level** | Everyday progress | Use XP potions (main) + a little XP from battles. |
| **Rank** | The gate | Costs **class-specific rank materials + gold**. Each rank raises max level by 10. |
| **Skills** | Grow with rank | Ranking up upgrades skills (see Skills). |
| **Awakening** | Duplicate reward | Duplicates become Soul Stones → add stars (see Awakening). |
| **Gear** | Customization | 4 slots of pre-designed gear (see Gear). |

### Rarity *(names are placeholders)*
Common (Gray) → Rare (Blue) → Epic (Purple) → Legendary (Gold)

### Level
- XP potions from campaign rewards and an XP dungeon. *(Example: Small potion = 100 XP, Big potion = 1,000 XP — placeholder.)*
- Champions in the team also get a small amount of XP from battles.
- No feeding/sacrificing other champions for XP.

### Rank
- Each rank-up costs class materials + gold. *(Example: Rank 2 → 3 = 10 Warrior Badges + 5,000 gold — placeholder.)*
- Each rank raises the max level by 10 and gives a stat jump.
- No sacrificing champions to rank up.

Example rank table *(placeholder)*:

| Rank | Max level |
|---|---|
| 1 | 10 |
| 2 | 20 |
| 3 | 30 |
| 4 | 40 |
| 5 | 50 |
| 6 | 60 |

---

## 4. Skills — Decided (Option 5: Hybrid)

- **Ranking up upgrades skills.** Every player gets stronger skills just by playing.
- **Awakening adds an extra "Awakened" bonus** on top of a skill (see Awakening).
- No separate skill-upgrade currency.
- No random skill-ups.

Skill levels should mix number upgrades with new effects. Example *(placeholder)*:

| Level | Shadow Strike |
|---|---|
| 1 | 100% damage |
| 2 | 115% damage |
| 3 | Adds Bleed for 2 turns |
| 4 | Cooldown −1 turn |
| 5 | 130% damage |

- Which ranks upgrade/unlock which skills: Open (to be defined per balancing).

---

## 5. Awakening — Decided (Option A: Simple Stars)

### How it works
- A duplicate champion becomes a **Soul Stone** for that specific champion (example: "Kaveh's Soul Stone").
- Soul Stones go to the **inventory**. Nothing happens automatically.
- The **player decides** when (or if) to use it.
- **1 Soul Stone = 1 star.** Max **5 stars**.
- Inspired by *Watcher of Realms*.

### What each star gives
Every champion follows the same pattern:

| Star | Reward |
|---|---|
| ⭐1 | + stats |
| ⭐2 | + stats |
| ⭐3 | + stats, **one skill becomes Awakened** |
| ⭐4 | + stats |
| ⭐5 | + stats, **Ultimate becomes Awakened** |

Example *(placeholder)*: Kaveh ⭐3 → Shadow Strike now hits 2 enemies. ⭐5 → Ultimate also stuns for 1 turn.

### Extra Soul Stones
- **Universal Soul Stone:** rare reward (chapter rewards, events). Works on any champion of the matching rarity. Helps free players.
- **After ⭐5:** extra Soul Stones for that champion can be exchanged for a currency used in a shop. Duplicates are never wasted.

### Design rules
- Awakening is a **bonus, not required**. A champion with 0 stars must still be useful.

### Awakening — Open
- Can players exchange an unused Soul Stone **before** reaching ⭐5?
- Should stars be limited by rank (example: ⭐3 requires Rank 3)?

---

## 6. Classes — Decided

5 classes. **Names are placeholders** and will change once the setting is chosen.

| Class | Job | Main stats | Weapon type |
|---|---|---|---|
| **Guardian** | Tank, protects the team | HP, Defense | Sword & Shield |
| **Warrior** | Melee damage, can take hits | Attack, HP | Axe |
| **Assassin** | Burst damage on one target | Attack, Crit | Daggers |
| **Ranger** | Ranged and area damage | Attack, Accuracy | Bow |
| **Support** | Heal, buff, debuff | HP, Speed | Staff |

### Class rules
- **Weapon must match the class.**
- Each class has **different automatic stat growth** when leveling.
- Each class has its **own rank material** (example: Guardian Badge, Assassin Badge).
- Some campaign stages can have **class requirements**.

### Not included (for now)
- No class advantage / counter system (no rock-paper-scissors).
- No team limits (example: "max 2 Assassins").
- No class team bonuses (example: "3 Warriors = +10% attack").

---

## 7. Gear — Decided

### Slots

| Slot | Who can wear | Main stat |
|---|---|---|
| Weapon | **Class-locked** | Attack |
| Armor | Open (see below) | Defense |
| Helmet | Open (see below) | HP |
| Accessory | Open (see below) | Speed, Crit or Accuracy (depends on the item) |

### Pre-designed gear (like Assassin's Creed Rebellion)
- **Every gear item is pre-designed with fixed stats.** No random stats.
- Example: every "Shiny Battle Axe" has Attack (main) + Crit Chance + Crit Damage (subs), always the same.
- Each item has a main stat + 2 fixed substats.
- Items for the same class/slot should have **different focuses**, not just be "better" (avoid one best-in-slot item). Example for Warriors *(placeholder)*:
  - Shiny Battle Axe: Crit Chance + Crit Damage (big hits)
  - Iron Cleaver: Attack % + Accuracy (steady damage)
  - Blood Axe: Lifesteal + HP (survival)

### Gear rarity *(placeholder)*
Common (Gray) → Rare (Blue) → Epic (Purple) → Legendary (Gold)

### Upgrading (same pattern as champions: Level + Rank)
- **Level:** gold + **Upgrade Stones**. Small stat increase each level. Upgrades always succeed (no failure chance).
- **Rank (merge):** merge 2 of the **same item** to raise its rank. Big stat boost and higher max level. Max rank **★5**.
- **Forge Stone:** rare item that can replace a duplicate in a merge (events, shop).

Example: Shiny Battle Axe (Epic, Warrior only) *(placeholder numbers)*:

| Rank | How | Max level | Attack | Crit Chance | Crit Damage |
|---|---|---|---|---|---|
| ★1 | Drop | 10 | 100 | 5% | 20% |
| ★2 | Merge 1 copy | 20 | 130 | 7% | 25% |
| ★3 | Merge 1 copy | 30 | 170 | 9% | 30% |
| ★4 | Merge 1 copy | 40 | 220 | 11% | 40% |
| ★5 | Merge 1 copy | 50 | 280 | 14% | 50% |

### Legendary weapons
- Have a small **passive**, one per weapon type. Examples *(placeholder)*:
  - Daggers: crits cause Bleed for 1 turn
  - Bow: first attack each battle can't miss
  - Axe: +10% damage to enemies below 50% HP
  - Sword & Shield: start battle with a small shield
  - Staff: heals also remove 1 debuff

### Getting gear
- Gear dungeon(s) (main source). Each dungeon stage drops only a few specific items so players can farm what they want.
- Campaign rewards, events, shop.

### Unwanted gear
- **Dismantle** for Upgrade Stones.
- Dismantling **refunds all upgrade materials** spent on that item.

### Gear — Open
- **Sets:** proposed only for Legendary gear (Armor + Helmet + Accessory, 2-piece and 3-piece bonuses, weapon not included). Not decided.
  - Example set ideas *(placeholder)*: Shadow (crit), Fortress (defense/shield), Storm (speed/extra turn), Life (HP/regen), Fury (attack/debuff damage).
- **Class restrictions on Armor / Helmet / Accessory:** not decided.

---

## 8. Resources — Proposed (not final)

Each system needs a resource, and each resource needs a place to farm it. This will shape the Dungeons design.

| System | Resource | Where it comes from |
|---|---|---|
| Champion Level | XP potions | Campaign, XP dungeon, battles (small) |
| Champion Rank | Class rank materials + gold | Rank dungeon(s) |
| Gear Level | Upgrade Stones + gold | Gear dungeon, dismantling |
| Gear Rank | Duplicate gear, Forge Stones | Gear dungeon, events, shop |
| Awakening | Soul Stones, Universal Soul Stones | Summons, events, mission/chapter rewards |

---

## 9. Open Questions (to decide later)

**Setting & combat**
- Game setting / universe and final names for classes, items and currencies.
- Detailed write-up of the combat mechanics taken from Assassin's Creed Rebellion (team size, mission/room structure, what the player controls).

**Modes**
- Dungeons: types, rewards, any entry limits.
- Events: types and rewards.
- Summoning: banners, rates, pity.
- Other modes (PvP, etc.).

**Campaign**
- Chapter reward model: full-star clear vs star milestones.
- Exact 3-star conditions.

**Champions**
- Which ranks upgrade/unlock which skills.
- Resource refund/reset option for champions (proposed: yes, to make experimenting safe).

**Awakening**
- Exchange Soul Stones before ⭐5?
- Star limited by rank?

**Gear**
- Legendary sets (yes/no, which slots).
- Class restrictions on Armor / Helmet / Accessory.

**Economy**
- Currencies (gold, premium currency, shop currencies) and their sources.

---

## 10. Glossary

- **Rarity:** Fixed quality tier of a champion or item.
- **Rank:** Upgrade step that raises max level. For champions: uses class materials. For gear: uses merging duplicates.
- **Awakening / Stars:** Champion upgrade from duplicates (Soul Stones), max 5 stars.
- **Soul Stone:** Item created from a duplicate champion, used to awaken that champion.
- **Universal Soul Stone:** Rare Soul Stone usable on any champion of a matching rarity.
- **Awakened skill:** A skill with an extra bonus effect unlocked at ⭐3 (one skill) or ⭐5 (Ultimate).
- **Upgrade Stone:** Material for leveling gear.
- **Forge Stone:** Rare material that replaces a duplicate when merging gear.
- **Merge:** Combining two copies of the same gear item to raise its rank.
