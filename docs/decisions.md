# Decision Log
> Read when: unsure whether something was already decided. Append one line per decision: `date | ID | decision | why`. Never rewrite old lines; supersede with a new line.

| Date | ID | Decision | Why |
|---|---|---|---|
| 2026-10-16 | D-01 | GDD rules imported from `game-design-decisions.md` into `docs/gdd/` | Single source, split per system |
| 2026-10-16 | D-02 | Docs + code live in one git repo (GitHub planned) | Git history = detailed progress log; docs keep only current state |
| 2026-10-16 | D-03 | Finish GDD before choosing tech stack (Q-03, Q-04) | Stack should fit the finished design |
| 2026-10-16 | D-04 | No Ultimate skills; all skills normal. ⭐5 awakens one more skill (CHM-15, AWK-6) | User decision |
| 2026-10-16 | D-05 | Gear rarities are Rare/Epic/Legendary only; substat count depends on rarity (GEAR-3, GEAR-10) | User decision |
| 2026-10-16 | D-06 | Team of 3 per stage; stage = rooms; player picks who enters each room (CMB-2, CMB-3). ACR model documented as Proposed CMB-4..18 | User decision + ACR research |
| 2026-10-16 | D-07 | Universe = original world + public-domain legends, organised into realms (frame "A+C", UNI-1..3) | We own the IP; legends give recognition; realms give variety |
| 2026-10-16 | D-08 | Claude writes lore, champions, skills, base stats; user approves | User decision |
| 2026-10-16 | D-09 | Universe draft approved (00-universe.md); weapon families + class names kept (CLS-7, CLS-8) | User approval |
| 2026-10-17 | D-10 | Combat v2: flexible party 1–3 + Noise, lines with nearest-enemy melee, Main Door/Margin/Gutter entries, Exposed, hazards/props/cover, gradual teaching (CMB-3..18, 32) | User agreed to recommendations |
| 2026-10-17 | D-11 | All remaining combat sections approved: room options, Rewrite, alert/Notice, turns, 3 skills, stats, health, RNG, no consumables, auto (02-combat.md) | User approval |
| 2026-10-17 | D-12 | F2P + IAP + ads + battle pass; first public build = beta without purchases. Android/Google Play first, landscape only, multi-language without Persian (OVR-6..8) | User decision |
| 2026-10-17 | D-13 | Launch scope, launch roster and currencies decided after a playable demo (Q-20, Q-13). Summoning modelled on RSL + WoR (MOD-3) | User decision |
| 2026-10-17 | D-14 | Bosses are never recruitable. Revised legends are captives: rescued (never fought), then join as champions (UNI, CMB-6, ENM-1) | User decision |
| 2026-10-17 | D-15 | Demo enemies kept simple: 4 roles, 5 Censors, tiers = Censor + Boss. Retinue and extra roles wait until after the demo (ENM-1, ENM-5) | User decision |
| 2026-10-17 | D-16 | Enemy roster approved (10-enemies.md, boss list still Q-25); status effects moved to CMB-40 | User approval |
| 2026-10-17 | D-17 | First chapter = **the Seedbed**, our own green realm at the Margin's edge; its rescued hero is a fully original Unwritten (UNI Volumes, 11-story.md) | User decision |
| 2026-10-17 | D-18 | Campaign goes light → dark: Seedbed → Thousand Nights → Book of Kings → Nocturne → Final Draft; darker inside each realm too (STY-1, STY-2). Characters per realm (Q-26) and bosses (Q-25) not decided | User decision |
| 2026-10-17 | D-19 | Story gives 2 free champions per realm (ch1 + ch4) + the Seedbed hero; other rescues via Recall. Config value, may change (STY-6) | User decision |
| 2026-10-17 | D-20 | 5 chapters per legend realm; each chapter = several stages with 1–3 stars each (STY-3, CMP-2). Free champions move to ch1 + ch5 | User decision |
| 2026-10-17 | D-21 | Kept for now (may revisit): Redactor's motive = lost love "Elegy" (placeholder); Folio was his catalogue, remembers at end of realm 2 (STY-8, STY-9) | User decision |
| 2026-10-17 | D-22 | Prologue: wake in the Margin, Censor raid with 2 Unwritten starters, free tutorial Recall gives the 3rd, then to the Seedbed (11-story.md) | User approval |
| 2026-10-17 | D-23 | Each captured legend is locked by a twist of their own tale (Holmes: no crimes, Sinbad: endless shipwreck…); rescue restores it (STY-5) | User approval |
| 2026-10-17 | D-24 | Add a 6th class: **Mage** (ranged magic damage + status effects). Details Open (Q-27) (CLS-1) | User decision |
| 2026-10-17 | D-25 | Seedbed chapter: sneak in → Herald + caged Unwritten → heart tree → Editor captain (no boss) → rescue original hero (11-story.md) | User approval |
| 2026-10-17 | D-26 | Mechanics taught once: Prologue basics, Seedbed party/room options/Herald, Thousand Nights ch1–4 Bleed-through, Margin, Gutter, hazards+props (STY-7, refines CMB-32) | User approval |
| 2026-10-17 | D-27 | Unwritten heroes get a short bio only, no extra story scenes (STY-10). GDD-2 story outline done (11-story.md) | User decision |
| 2026-10-17 | D-28 | Demo content = Prologue + Seedbed (~6 stages), ~30–45 min; no Thousand Nights (DEMO-2) | User approval |
| 2026-10-17 | D-29 | Demo roster = 4 champions: Guardian, Assassin, Support, Mage (DEMO-4) | User approval |
| 2026-10-17 | D-30 | Demo progression = stage stars + replay, level up, rank up. No gear, awakening or skill upgrades (DEMO-6, DEMO-7) | User approval |
| 2026-10-17 | D-31 | Demo is online: guest login, server save, server-checked stage results (DEMO-9) | User decision |
| 2026-10-17 | D-32 | Demo tester = the user only for now; APK installed directly (Google Play testing not available yet). English only (DEMO-10) | User decision |
| 2026-10-17 | D-33 | No auto-battle in the demo (CMB-39 stays for full game). GDD-3 demo definition done (plan/demo.md) | User decision |
| 2026-10-17 | D-34 | Ranger = ranged single-target, precise; Mage = ranged area magic + status effects, fragile (06-classes, Q-27 part 1) | User approval |
| 2026-10-17 | D-35 | Mage main stats = Attack + Speed (Q-27 part 2) | User approval |
| 2026-10-17 | D-36 | Mage weapon family = Arcana: wand, orb, rod, scroll (CLS-7, Q-27 part 3) | User approval |
| 2026-10-17 | D-37 | Mage has no room option for now; revisit after the demo. Q-27 resolved | User approval |
| 2026-10-17 | D-38 | Main hero is a Warrior. Demo roster = Warrior (main) + Assassin starters, Support from tutorial Recall, Mage rescued in Seedbed; Guardian out of demo (replaces D-29 classes) | User decision |
| 2026-10-17 | D-39 | Main hero concept "The Eager Blade": young, cheerful, reckless Unwritten Warrior with a pen-nib greatsword. May change after the demo (champions/c01-main-hero.md) | User decision |
| 2026-10-17 | D-40 | Main hero is a young woman | User decision |
| 2026-10-17 | D-41 | Main hero name: **Margo** (from the Margin) — champions/c01-margo.md | User decision |
| 2026-10-17 | D-42 | Margo look: parchment-coloured adventurer, short black hair with white streak, blank paper scarf that fills with words per awakening star, orange-red accent | User decision |
| 2026-10-17 | D-43 | Margo skills: S1 Nib Slash (single), S2 Bold Stroke (splash, CD3), S3 Exclamation Leap! (Dive, self Defense down, CD4) | User approval |
| 2026-10-17 | D-44 | Margo rarity: Epic | User approval |
| 2026-10-17 | D-45 | Margo stat shape: HP High, Attack High, Defense/Speed/Crit Dmg/Accuracy Medium, Crit Chance Low. Margo design complete | User approval |
| 2026-10-17 | D-46 | Assassin starter = **Pell** "The Half-Erased" (Rare, young man, twin letter-openers; Fine Line / Strikethrough / Fade) — user delegated all choices to Claude (champions/c02-pell.md) | User delegated |
| 2026-10-17 | D-47 | Seedbed Mage = **Libby** "The Sprout" (Rare, young girl, leafing scroll; Seed Scatter / Bramble Line / Rootbind) — user delegated all choices to Claude (champions/c03-libby.md) | User delegated |
| 2026-10-17 | D-48 | Tutorial Recall gives a famous legend (the demo's Support). May change later | User approval |
| 2026-10-17 | D-49 | Tutorial Recall legend = **Lady White Snake (Bai Suzhen)**, Chinese legend, healer Support. May change later | User decision |
| 2026-10-17 | D-50 | White Snake design (Epic; Jade Needle / Herbal Remedy / Coiled Guard; no monk antagonist) — user delegated to Claude. GDD-4 demo champions done (champions/c01..c04) | User delegated |
| 2026-10-17 | D-51 | Number scale Medium (Rare Lv1 avg HP 1,000 / Atk 100 / Def 50 / Spd 100). Speed = turn order only, range ~90–120; turn-order rule kept swappable for after the demo (BAL-1, BAL-3) | User approval |
| 2026-10-17 | D-52 | Defense = percentage: dmg = Atk × skill% × 100/(100+Def); crit × (1+Crit Dmg); ±5% spread (BAL-2) | User approval |
| 2026-10-17 | D-53 | Stat shape tiers → Lv1 values; rarity multiplies HP/Atk/Def only (C 0.85 / R 1.0 / E 1.15 / L 1.3) (BAL-5, BAL-6) | User approval |
| 2026-10-17 | D-54 | Per-hero stat tweak up to ±10% in content data; demo: White Snake HP +5% (BAL-7) | User approval |
| 2026-10-17 | D-55 | Q-18 placeholder: linear class growth per level, % of Lv1 (BAL-8) | User approval |
| 2026-10-17 | D-56 | Rank-up: HP/Atk/Def +10% + class bonus; R1→R2 5 badges + 1,000 gold; demo cap Rank 2 / Lv 20 (BAL-9, BAL-10) | User approval |
| 2026-10-17 | D-57 | XP to next level = 100 × level; stage XP to whole stage team; demo ends ~Lv 15 (BAL-11, BAL-13) | User approval |
| 2026-10-17 | D-58 | Story heroes join at level/rank of player's lowest hero; summoned heroes start Lv1 (BAL-12) | User approval |
| 2026-10-17 | D-59 | Stage gold/badges (2 of each class per Seedbed stage); replay 50% XP/gold, no badges; stars = score only in demo (BAL-13, BAL-14) | User approval |
| 2026-10-17 | D-60 | Status effect strengths (Bleed 30% Atk, ±25% Atk, ±30% Def, ±15 Spd); no stacking (BAL-15) | User approval |
| 2026-10-17 | D-61 | Demo hero skill % and Censor stats/kits/levels (enemy Lv = expected team Lv + 1). GDD-5 done | User approval |
| 2026-10-17 | D-62 | Q-03 stack: shared C# rules (netstandard2.1) + ASP.NET Core .NET 10 + PostgreSQL (EF Core/Npgsql) + Unity Authentication (server verifies token); HTTP+JSON now, SignalR for live PvP later; Docker; OpenAPI (built-in) + Scalar in dev only, no client codegen (DTOs in shared) | User approval |
| 2026-10-17 | D-63 | Packages: UniTask, PrimeTween, Newtonsoft (Unity pkg), Serilog, xUnit + Testcontainers, later Redis, Sentry, NuGetForUnity + SignalR client. Rule: free, actively maintained, community-recommended, Unity official / MIT / Apache only | User approval |
| 2026-10-17 | D-64 | No cloud CI: local scripts (`test`, `build-android`, optional pre-push hook). GitHub = code storage only. Client = Unity 6 LTS, 2D URP, Android IL2CPP ARM64 | User approval |
| 2026-10-17 | D-65 | Q-04 UI: uGUI (+ TextMeshPro, PrimeTween). Screen logic kept separate from views so a screen can move to UI Toolkit later | User approval |
| 2026-10-17 | D-66 | Architecture A1–A11 approved (`tech/architecture.md`): adds safe retries, ledger, content version check, client layers Core/Screens/Views, stateless server, server modules. Tech docs Decided (M0-7) | User approval |
| 2026-10-17 | D-67 | Repo: github.com/MohammadAminZiarati8/palimpsest (private), branch `main`; commits signed MohammadAminZiarati8 (repo-local git config) | User approval |
| 2026-10-17 | D-68 | Art style = stylized 3D game-model look like AC Rebellion (big head ~2.5–3 heads, mature sculpted faces, matte detailed costumes); rules ART-1..5 in `art/style.md`. Real art in demo, not placeholders | User approval |
| 2026-10-17 | D-69 | Characters = 2D cut-out sprites animated with bones (Unity 2D Animation + PSD Importer), not 3D models, not frame-by-frame. D-64 2D URP unchanged (ART-6) | User approval |
| 2026-10-17 | D-70 | Demo art pipeline: Claude + Gemini + ChatGPT Plus (hero picture → parts sheet → Claude crops, rigs, animates in Unity); no hired artist for the demo (ART-7, DEMO-12). Launch art/audio still Q-23 | User approval |
| 2026-10-17 | D-71 | Unity 6.3 LTS `6000.3.23f1` (installed, has Android SDK/NDK); UniTask/PrimeTween via OpenUPM; placeholder app id `com.ziarati.palimpsest` until Q-24. Implementation choice in M0-5 | Claude (user may change) |
| 2026-10-17 | D-72 | M1 tech: shared uses Newtonsoft 13.0.2 (same as Unity pkg); content copied to Unity `Resources/Content`; stat math in decimal, one round half-away-from-zero per stat (matches BAL table) | Claude (user may change) |
| 2026-10-17 | D-73 | M2 split: M2a = single fight sim + Unity prototype (tasks T0–T8 in STATUS); M2b = stage flow (scenes, doors, room options, Noise, Notice, Herald, Rewrite, Retreat). Small tasks, one session/commit each | User approval |
| 2026-10-17 | D-74 | Enemy AI (CMB-41): intent fixed at round start; special skill when ready (Corrector heals only an ally at ≤75% HP, Blotter Soak only once damaged), else S1; melee → nearest hero, ranged → lowest HP %, tie → front-most; invalid target at action time → retarget by same rule | User approval |
| 2026-10-17 | D-75 | Exposed (CMB-15) = +25% crit chance, additive, cap 100% (BAL-20) | User approval |
| 2026-10-17 | D-76 | Enemies: Crit Chance 5%, Crit Damage +50%, content values with per-enemy override (BAL-17) | User approval |
| 2026-10-17 | D-77 | Cooldowns (CMB-42): all skills ready at fight start, reset every scene; cd N = used turn 1 → ready turn 1+N; ticks at end of own turn, skipped (Stun) turns count | User approval |
| 2026-10-17 | D-78 | Splash (CMB-13) second hit = enemy behind the target (far side from attacker), else the other side; never allies; no side pick | User approval |
| 2026-10-17 | D-79 | Combat defaults (CMB-43): damage rounded half-away-from-zero, min 1, uniform ±5% spread; turn order rebuilt each round start, ties champion → Front-most; joiners act next round; Bleed ticks at start of affected unit's turn, Ward absorbs it; cleanse removes newest debuff; Dive targets any enemy; gaps close toward centre; win = all enemies on field Down (pending arrivals cancelled); 50-round cap = loss (config) | Claude default, user did not object |
| 2026-10-17 | D-80 | Q-02 removed from roadmap blockers (settled by combat v2, D-10/D-11) | Stale reference |
| 2026-10-17 | D-81 | Enemy growth (BAL-17) is linear like heroes (BAL-8): Lv1 × (1 + (L−1) × 9%), then Elite ×; growthPercent in `content/enemies.json` | User approval |
| 2026-10-17 | D-82 | Fight core (M2a-T2): 1–3 units per side (CMB-11); Down unit leaves its slot, units behind step toward the centre; round cap = loss if not won by the end of round 50; turn order behind `ITurnOrder` (BAL-3) | Claude default (reading of D-79) |
| 2026-10-17 | D-83 | Damage (M2a-T3): each unit hit rolls its own crit then spread (spread in 0.01% steps); Splash/Row targets fixed before anyone goes Down; Exposed check returns false until rear positions exist (Margin/Gutter/Bleed-through, M2b) | Claude default |
| 2026-10-17 | D-84 | Effects (M2a-T4): an effect applied during the affected unit's own turn skips that turn's tick (Margo's self Defense down 1 lasts until her next turn ends); re-apply replaces duration + size (Bleed size = applier Attack × 30% at apply time); Up and Down of one stat both count (cancel) | Claude default |
| 2026-10-17 | D-85 | Kits (M2a-T5): Faded unit is skipped by Single skills and doesn't block melee (next enemy becomes nearest); Splash/Row still hit it; Fade lasts until the end of Pell's next turn (D-84 rule); Ally/AllAllies include the caster; Shift = champions only, adjacent ally | Claude default |
| 2026-10-17 | D-86 | Enemy AI (M2a-T6): specials checked S3 then S2; no valid target = skip the action; heal picks lowest HP % ally under the threshold; healer self-heal behind `combat.json aiHealSelf` (false, Q-27) | Claude default |
| 2026-10-17 | D-87 | T7 fight screen built from code (no prefabs); placeholder text uses uGUI legacy `Text` + built-in font until TMP Essentials are imported (T8); opened via menu Game/Test Fight | Claude default (user approved scripts only) |
| 2026-10-17 | D-88 | T8 test lineups (Front first; heroes Margo/Pell/White Snake one level below BAL-19, rank 2 from S4): S1 Blotter+Inkslinger · S2 Blotter+2 Inkslinger · S3 Blotter+Corrector+Inkslinger · S4 2 Blotter+Corrector · S5 Blotter+Inkslinger+Corrector · S6 Editor+Inkslinger+Corrector. Test only; real stages in M2b | User approval |
| 2026-10-17 | D-89 | Q-27: enemy healers (Corrector) may heal themselves (CMB-41); `combat.json aiHealSelf` = true | User approval |
| 2026-10-17 | D-90 | M2b split into T0–T8 (STATUS): questions, stages.json, stage sim, Join/arrivals/Herald, room options + Noise, entry points, Notice + Rewrite, Unity map, Seedbed play-check | User approval |
| 2026-10-17 | D-91 | Door heal (CMB-9, BAL-21): 15% max HP per scene waited at the door (config) | User approval |
| 2026-10-17 | D-92 | Room option % (CMB-19, BAL-22): 50% ± 5%/level vs scene (party average for party options), Assassin +15% Strike Out / Between the Lines, Support +15% Spin a Tale, clamp 5–95%, no rank/stars in demo | User approval |
| 2026-10-17 | D-93 | Margin Strike (BAL-23): each hero in the scene gets one free pre-round action in Speed order, +50% crit chance (cap 100%), enemies don't act, cooldowns as normal | User approval |
| 2026-10-17 | D-94 | Strike Out (BAL-24): any one non-Elite target; success removes it (none left = scene cleared), failure = target acts first then fight; solo Assassin then fights | User approval |
| 2026-10-17 | D-95 | Stage lineups `content/stages.json` (Prologue 1–2 Margo+Pell; Seedbed 1–6 +White Snake, Libby after S6; Heralds in S2/S4/S5/S6 call an Inkslinger; S6 finale Editor+Inkslinger+Corrector). C3 Herald added: Low/Low/Low/Medium, Back, S1 Ring (countdown 2) | User approval |
| 2026-10-17 | D-96 | Stage sim (M2b-T2): door heal applied when a scene starts to non-Down heroes not entering; Down heroes never heal in-stage; each scene fight seed = next value from the stage seed (A3); a lost fight fails the stage until Join (T3); `doorHealPercent` in stages.json | Claude default |
| 2026-10-17 | D-97 | Herald (ENM-9) rings once per fight (countdown = its own turns, 2), then does nothing | User approval |
| 2026-10-17 | D-98 | Join/arrivals (M2b-T3): Join is free between turns, enters at our Back, acts next round, capped by scene capacity; Fall back keeps HP, can rejoin; fight not lost while someone waits at the door; arrivals and rung-in reinforcements enter at a round start when a slot is free (max 3) | Claude default |
| 2026-10-17 | D-99 | Room options (M2b-T4): Margin Strike, Strike Out, Between the Lines need a Reading scene; Spin a Tale/Clash any time; "acts first"/"skip round 1"/Margin Strike all = a one-side opening before round 1 (round 0, not counted for the cap); rolls use their own stream from the stage seed; stealth success with no enemies left passes the scene | Claude default |
