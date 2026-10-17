# 02 Combat — v2 Decided (D-10, D-11)
> Read when: any combat work. Inspired by *Assassin's Creed Rebellion* (ACR), adapted and themed per `00-universe.md`. Numbers → `balancing.md`.

## Core
- **CMB-1** Structure from ACR (stage of rooms, room options, turn-based), adapted below (D-10).
- **CMB-2** Player picks 3 champions per stage.
- **CMB-3** A stage is a series of **scenes** (rooms). Before each scene the player picks which champions enter: 1, 2 or 3. The rest wait at the door.

## Stage map
- **CMB-4** Side-view map of scenes joined by doors; some stages offer alternative routes (Margin Path, Blot shortcut).
- **CMB-5** Scene preview: enemies (Reading 📖 / Watching 👁) + intents, nearby enemies, obstacles, entry points, success % of room options.
- **CMB-6** Each stage has an objective (free a legend, defeat a boss, reach the exit…).

## Party & Noise
- **CMB-7** Noise: more champions = louder.
  | Party | Room options allowed |
  |---|---|
  | 1 | All |
  | 2 | Clash, Margin Strike, Spin a Tale |
  | 3 | Clash, Spin a Tale |
- **CMB-8** Scene capacity 1–3 (narrow stairs/rooftops 1, corridor 2, hall/boss 3).
- **CMB-9** Champions who don't enter a scene recover some HP at the door.
- **CMB-10** **Join:** a waiting champion enters mid-fight at the Main Door end, acts next round (if capacity allows). **Fall back:** a champion leaves to the door (costs its turn).

## Row & lines
- **CMB-11** The fight is one row of slots. Default: `ours [Back][Middle][Front] ⚔ [Front][Middle][Back] enemies`. A boss may take 2 slots.
- **CMB-12** **Melee hits the nearest enemy on either side** (allies don't block, enemies do). **Ranged** hits anyone. Every champion can always act.
- **CMB-13** Skill **Area**: Single · Splash (target + neighbour) · Row (all enemies).
- **CMB-14** When a slot empties, units close the gap.
- **CMB-15** **Exposed:** a unit hit from its rear takes bonus crit chance. Applies to both sides.
- **CMB-16** Line-breaking skills: **Dive** (melee reaches enemy Back), **Pull**, **Push**, **Guard** (take hits for an ally), **Shift** (any champion: swap with adjacent ally, costs turn).

## Deployment & entry points
- **CMB-17** Before a fight, drag champions onto the scene's entry points:
  | Entry | Position | Note |
  |---|---|---|
  | **Main Door** | Normal, facing enemy Front | Always available |
  | **Margin** | Behind enemy Back | Scene needs a Margin Path; flanker is alone |
  | **Gutter** | Between enemy Front and Back | Only if those enemies are Reading; hit from both sides |
- **CMB-18** Enemies mirror it: Bleed-through (behind us); some bosses appear in our Gutter.

## Room options
| Option | Who | Effect | On failure |
|---|---|---|---|
| **Clash** | Anyone | Normal fight | — |
| **Margin Strike** | vs Reading enemies | Free crit-boosted opening action | — |
| **Strike Out** | Assassin, solo | Silent kill (cross the target out of the story), shows % | Target strikes first, fight starts |
| **Between the Lines** | Solo (Assassin best, Ranger good) | Pass unseen, no fight, shows % | Spotted; enemies act first |
| **Spin a Tale** | Support in party | Enthral: enemies skip round 1, shows % | Enemies act first |
- **CMB-19** Room option % = power (level, rank, stars) vs scene level + class modifier.
- **CMB-20** Optional — **Rewrite** (Last Quill): 1 free charge per stage, not purchasable. Before a scene: turn one Watching enemy to Reading, silence a Herald, or reveal a Margin Path.

## Alert & gathering
- **CMB-21** When a fight starts, every enemy in the scene becomes Watching.
- **CMB-22** **Nearby enemies** (posts, patrols, balconies) show "arrives in N rounds", then join the end of their line on the side they come from (can land behind a Margin flanker).
- **CMB-23** **Notice** meter per stage: Clash +1, failed stealth +1, Herald alarm +2, stealth clear 0. Thresholds: later scenes start more Watching, extra patrols. Usable as a star condition.
- Counters: Strike Out before the fight, fast kills, Cover on doors, Ranger shooting arrivals.

## Turns
- **CMB-24** Enemies never start a fight.
- **CMB-25** A **round** = every unit acts once in **Speed** order (ties → champion first).
- **CMB-26** Enemies show their next action (**intent**).

## Skills & stats
- **CMB-27** 3 skills per champion: **S1** basic (no cooldown) · **S2**, **S3** active (cooldown in rounds) or passive. Each skill tagged Reach (Melee/Ranged) + Area.
- **CMB-28** Awakening: ⭐3 awakens S2, ⭐5 awakens S3 (AWK-5/6).
- **CMB-29** Stats: HP, Attack, Defense, Speed, Crit Chance, Crit Damage, Accuracy. Accuracy counters enemy-only **Evasion**.
- **CMB-30** Lifesteal and % stats are gear substats only.
- **CMB-31** Damage = Attack × skill % × Defense reduction (formula → balancing).
- **CMB-40** Status effects (shared by champions and enemies, D-16): **Stun** (skip turn) · **Bleed** (damage per round) · **Seal** (one skill locked = Redaction) · **Ward** (absorbs damage) · **Attack / Defense / Speed / Accuracy up or down**. More only after the demo.

## Obstacles & scene features
| Feature | Effect | Answer |
|---|---|---|
| **Herald** (bell / *kus* / gong) | Rings → reinforcements written in, Notice +2 | Kill/shoot first (Ranger, Assassin) |
| **Bleed-through** | Enemies appear behind us | Guardian holds the rear |
| **Trap** | Damage on entry | Ranger disarms |
| **Redaction** (censor bars, fog, div sorcery) | Seals one skill for the scene | Support restores |
| **Blot** (ink blot, barricade) | Blocks a shortcut | Warrior smashes |
| **Lost Page** (sealed chest/door) | Bonus loot, maybe Pages | Assassin unseals |
| **Margin Path** | Alternative route; enables Margin entry | Anyone |
| **Hazard slot** (ink pool, brazier, pit) | Hurts whoever stands there | Push/Pull enemies into it |
| **Prop** (chandelier, bell, *kus*, djinn lamp) | One-time scene effect | Anyone uses |
| **Cover** (bookshelf, barricade) | Blocks melee through its slot | Warrior smashes |
- **CMB-32** Teach gradually per Volume: ch1 lines + party · ch2 Bleed-through · ch3 Margin · ch4 Gutter, hazards, props.

## Health, death, stars
- **CMB-33** HP carries across scenes; full heal after the stage. No rest timers.
- **CMB-34** 0 HP = **Down** for the stage; fail when all 3 are Down. Some Support skills revive.
- **CMB-35** Retreat any time, free.
- **CMB-36** 3 stars = nobody Down. Round count and Notice also usable as conditions.

## Randomness & auto
- **CMB-37** Combat RNG allowed (room option %, crits); seeded for server re-sim.
- **CMB-38** No consumables in stages.
- **CMB-39** Auto-battle incl. auto-deploy.

## Tech note
Deterministic, engine-free combat sim in `shared/`; server issues the seed and can re-simulate. See `tech/architecture.md`.
