# 10 Enemies — Decided (D-16); boss list Open (Q-25)
> Read when: designing stages, bosses, enemy AI, stage content data. Rules → `02-combat.md`, lore → `00-universe.md`. All stats are relative tiers here; demo numbers → `balancing.md` BAL-16..19, later `content/`.

## Tiers
- **ENM-1** Two tiers (D-15). **No enemy or boss is ever recruitable** (D-14). Legends are never fought: they are rescued (stage objective, CMB-6) and join as champions.
  | Tier | Who | Where | Strike Out? |
  |---|---|---|---|
  | **Censor** | Faceless ink-soldiers, every Volume (reskinned) | Every scene | Yes (except Elites) |
  | **Boss** | Tyrants (willing allies of the Redactor) and other villains — list Open (Q-25) | Chapter / Volume finals | No |
- **ENM-2** **Elite** = tag on a Censor (captain): mid-boss, immune to Strike Out.
- **ENM-3** Every enemy = role + Reach + slot + stat tiers + 1–3 skills (each with an intent icon, CMB-26).

## Roles (demo)
- **ENM-5** The demo uses exactly 4 enemy roles. More roles are chosen after the demo (backlog below, same approach as D-13).
  | Role | Job | Answer |
  |---|---|---|
  | **ENM-6 Front-line** | Holds Front, soaks hits, blocks melee from our Back targets (CMB-12) | Burst it down, or go around: Dive, Margin entry |
  | **ENM-7 Ranged** | Hits anyone from the Back | Ranged, Dive, Margin entry |
  | **ENM-8 Healer** | Heals allies from the Back | Kill first: Assassin Dive, Ranger |
  | **ENM-9 Herald** | Intent "Rings in N". On ring: reinforcements written in + Notice +2 (CMB-23) | Kill/shoot first, Rewrite silence (CMB-20), Stun, Strike Out before the fight |
- **ENM-10** Each boss gets one signature mechanic (candidates below). These are boss kits, not roles.
- **ENM-11** Status effects → CMB-40.

## Censors (demo)
Stat tiers: ▲ high · ● mid · ▼ low (HP / Atk / Def / Spd).
| # | Censor | Role | Reach · Slot | HP/Atk/Def/Spd | Kit | Teaches (CMB-32) |
|---|---|---|---|---|---|---|
| C1 | **Blotter** | Front-line | Melee · Front | ▲/●/●/▼ | Smear (hit) · Soak (Ward self) | ch1 |
| C2 | **Inkslinger** | Ranged | Ranged · Back | ▼/●/▼/● | Ink shot · Splatter (Splash) | ch1 |
| C3 | **Herald** (Bell-ringer / *Kus*-drummer / Gong-crier) | Herald | — · Back | ▼/▼/▼/● | Ring (countdown 2–3) | ch2 |
| C4 | **Corrector** | Healer | Ranged · Back | ●/▼/●/● | Correction fluid (heal) | ch3 |
| C5 | **Editor** | Front-line, Elite | Melee · Front | ▲/▲/●/● | Cut (hit) · Final say (allies Attack up) | chapter mid-boss |
- **ENM-12** Each Volume reskins Censors (Nocturne fog-grey greatcoats + lanterns; Book of Kings ink lamellar; Thousand Nights palace livery) and names its Herald by the Volume's alarm (UNI Volumes table).
- Bleed-through (CMB-18) is a scene feature: any Censor can enter that way, no extra role needed.

## Boss candidates — Open (Q-25)
Ideas only; the boss list is not decided. None are recruitable (ENM-1).
| Volume | Candidate | Slots | Signature mechanic | Answer |
|---|---|---|---|---|
| Nocturne | **Dorian Gray** (Wilde, 1890 — would add Wilde to Nocturne sources) | 1 + Portrait | The **Portrait** sits in the enemy Back behind Cover; Dorian can't drop below 1 HP while it stands | Ranged/Dive/Margin onto the Portrait, Warrior smashes Cover |
| Nocturne | **Headless Horseman** (Irving) | 2 | Gallop: re-forms in our **Gutter** (CMB-18); Hurl the Head (Ranged Splash) | Alt objective: reach the church bridge (CMB-6), no kill needed |
| Book of Kings | **White Div** (Rostam's 7th labour) | 2 | Cave darkness: scene starts with Row Accuracy down | Support cleanse, burst |
| Book of Kings | **Zahhak** | 2 | Two serpent parts (bite = Melee, venom = Ranged Row). A cut serpent regrows in 2 rounds unless both are cut in the same round → Zahhak Stunned | Splash/Row, timing. At 0 HP he is **bound** (not killed), true to Shahnameh |
| Thousand Nights | **Sorcerer of the Lamp** (Aladdin's magician, unnamed in text) | 1 + Djinn (2) | Commands an enslaved, near-unkillable Djinn. The Lamp is a **Prop** in his slot: an adjacent champion seizes it (costs turn) → Djinn is freed and leaves | Reach his Back (Margin, Dive, Pull), free the Djinn |
| Thousand Nights | **Captain of the Thieves** | 1 | Marks a champion; all thieves focus it | Kill the captain first, Guard the marked one |

- **ENM-13** Demo uses only the bosses of the demo Volume (GDD-3).
- **ENM-14** Respect (UNI IP rules): no ethnic depiction of Zahhak or the Sorcerer (villains by deed only), no gore (Zahhak's serpents "feed" = heal), source-text looks only.
- **ENM-15** Enemy definitions are content data (`content/enemies/`): id, tier, role, skills, intents. Schema → `tech/data.md` once stage content is built.

## After the demo — backlog (not designed, not in demo)
Candidate roles: Shielded (shield vs front, Warrior rewarded) · Evasive (Accuracy check) · Artillery (charged Row hit) · Debuffer (Seal) · Line-breaker (Pull/Push) · Ambusher (hidden / Bleed-through specialist) · Summoner · Volatile (explodes) · Aerial (only Ranged reaches) · Sentinel (can't be snuck past).
Candidate tier: **Retinue** — a tyrant's own local troops, so each Volume has enemies from its source story.
Unit ideas kept: Censors — Bar Warden, Smudge, Redliner, Binder, Seeper, Spill, Quillshot, Copyist · Retinue — Gargoyle, Hound of the Baskervilles, Div Brute, Div Sorcerer, Forty Thieves, Old Man of the Sea, Roc.
