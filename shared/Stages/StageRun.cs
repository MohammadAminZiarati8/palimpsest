using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Champions;
using Game.Shared.Combat;
using Game.Shared.Common;
using Game.Shared.Content;

namespace Game.Shared.Stages
{
    public enum StageState { ChooseParty, Fighting, Cleared, Failed, Retreated }

    /// <summary>Room options (02-combat.md room table).</summary>
    public enum RoomOption { Clash, MarginStrike, StrikeOut, BetweenTheLines, SpinATale }

    /// <summary>How a scene began: the option, its roll, and the fight (null = scene passed without one).</summary>
    public sealed class SceneStart
    {
        public RoomOption Option { get; internal set; }
        /// <summary>Shown % (100 for options without a roll).</summary>
        public int Chance { get; internal set; } = 100;
        public bool Success { get; internal set; } = true;
        public Fight? Fight { get; internal set; }
    }

    /// <summary>One champion's state across the stage's scenes (CMB-33/34).</summary>
    public sealed class StageHero
    {
        public string Id { get; }
        public int Level { get; }
        public int Rank { get; }
        public int MaxHp { get; }
        public int Hp { get; internal set; }
        /// <summary>CMB-34: Down for the rest of the stage.</summary>
        public bool IsDown => Hp <= 0;

        internal StageHero(string id, int level, int rank, int maxHp)
        {
            Id = id;
            Level = level;
            Rank = rank;
            MaxHp = maxHp;
            Hp = maxHp;
        }
    }

    /// <summary>
    /// M2b-T2 stage flow: scenes in order (CMB-3), party pick per scene within capacity (CMB-8),
    /// waiting heroes heal at the door (CMB-9, BAL-21), HP carries (CMB-33), Down for the stage (CMB-34),
    /// Retreat any time (CMB-35), 3 stars = nobody Down (CMB-36). Join / Fall back / arrivals / Herald live in <see cref="Combat.Fight"/>; room options: T4.
    /// Every scene's fight seed comes from the stage seed, so a replay with the same inputs matches (A3).
    /// </summary>
    public sealed class StageRun
    {
        private readonly ContentDb _db;
        private readonly SeededRandom _seeds;
        private readonly List<StageHero> _team = new List<StageHero>();
        private List<StageHero> _party = new List<StageHero>();

        public StageDef Stage { get; }
        public StageState State { get; private set; }
        /// <summary>0-based index of the current scene.</summary>
        public int SceneIndex { get; private set; }
        public SceneDef Scene => Stage.Scenes[SceneIndex];
        public IReadOnlyList<StageHero> Team => _team;
        /// <summary>The fight of the current scene while <see cref="StageState.Fighting"/>.</summary>
        public Fight? Fight { get; private set; }
        public bool IsOver => State == StageState.Cleared || State == StageState.Failed || State == StageState.Retreated;
        /// <summary>CMB-36: cleared with no hero Down at any point.</summary>
        public bool ThreeStars => State == StageState.Cleared && _team.All(h => !h.IsDown);

        /// <param name="team">CMB-2: 1–3 champions from the stage's allowed heroes, with level/rank.</param>
        public StageRun(ContentDb db, StageDef stage, IReadOnlyList<HeroEntry> team, ulong seed)
        {
            _db = db;
            Stage = stage;
            _seeds = new SeededRandom(seed);
            if (team.Count < 1 || team.Count > Fight.MaxSlots)
                throw new ArgumentException($"A stage team needs 1..{Fight.MaxSlots} champions (CMB-2)");
            if (team.Select(t => t.ChampionId).Distinct().Count() != team.Count)
                throw new ArgumentException("A champion can only be in the team once");
            foreach (var t in team)
            {
                if (!stage.Heroes.Contains(t.ChampionId))
                    throw new ArgumentException($"{t.ChampionId} is not allowed in {stage.Id}");
                var stats = StatCalculator.At(db, db.Champion(t.ChampionId), t.Level, t.Rank);
                _team.Add(new StageHero(t.ChampionId, t.Level, t.Rank, stats.Hp));
            }
        }

        public StageHero Hero(string id) => _team.First(h => h.Id == id);

        /// <summary>
        /// Starts the current scene with these champions (Front first). The rest wait at the door
        /// and heal BAL-21 % of max HP (CMB-9). Enemies use the stage level (BAL-19).
        /// </summary>
        public Fight Enter(IReadOnlyList<string> partyFrontFirst) => Enter(partyFrontFirst, RoomOption.Clash).Fight!;

        /// <summary>
        /// CMB-7 Noise + room rules: which options this party may pick here. Stealth options need the
        /// scene Reading (D-99); Strike Out needs a solo Assassin, Spin a Tale a Support in the party.
        /// </summary>
        public IReadOnlyList<RoomOption> Allowed(IReadOnlyList<string> party)
        {
            var list = new List<RoomOption> { RoomOption.Clash };
            bool reading = Scene.Alert == Alert.Reading;
            if (reading && party.Count <= 2) list.Add(RoomOption.MarginStrike);
            if (reading && party.Count == 1 && ClassOf(party[0]) == HeroClass.Assassin) list.Add(RoomOption.StrikeOut);
            if (reading && party.Count == 1) list.Add(RoomOption.BetweenTheLines);
            if (party.Any(id => ClassOf(id) == HeroClass.Support)) list.Add(RoomOption.SpinATale);
            return list;
        }

        /// <summary>
        /// BAL-22: base + per-level × (party average level − scene level), Assassin bonus on
        /// Strike Out / Between the Lines, Support bonus on Spin a Tale, clamped. 100 if no roll.
        /// </summary>
        public int Chance(RoomOption option, IReadOnlyList<string> party)
        {
            if (option == RoomOption.Clash || option == RoomOption.MarginStrike) return 100;
            var r = _db.StageRules;
            decimal avg = party.Select(id => (decimal)Hero(id).Level).Average();
            decimal chance = r.RoomBasePercent + r.RoomPerLevelPercent * (avg - Stage.EnemyLevel);
            bool assassin = party.Any(id => ClassOf(id) == HeroClass.Assassin);
            if ((option == RoomOption.StrikeOut || option == RoomOption.BetweenTheLines) && assassin)
                chance += r.RoomClassBonusPercent;
            if (option == RoomOption.SpinATale) chance += r.RoomClassBonusPercent; // Support required
            int pct = (int)Math.Round(chance, MidpointRounding.AwayFromZero);
            return Math.Max(r.RoomMinPercent, Math.Min(r.RoomMaxPercent, pct));
        }

        /// <summary>
        /// Starts the current scene with a room option. Strike Out needs <paramref name="strikeTarget"/>
        /// (enemy index, non-Elite, BAL-24). Rolls use their own stream from the stage seed (A3).
        /// <paramref name="marginHero"/>: one party champion entering by the Margin, behind the enemy Back
        /// (CMB-17, D-101); needs the scene's Margin Path. The rest use the Main Door in the given order.
        /// </summary>
        public SceneStart Enter(IReadOnlyList<string> partyFrontFirst, RoomOption option, int strikeTarget = -1,
            string? marginHero = null)
        {
            if (State != StageState.ChooseParty) throw new InvalidOperationException($"Can't enter while {State}");
            if (partyFrontFirst.Count < 1 || partyFrontFirst.Count > Scene.Capacity)
                throw new ArgumentException($"Scene {SceneIndex + 1} takes 1..{Scene.Capacity} champions (CMB-8)");
            if (partyFrontFirst.Distinct().Count() != partyFrontFirst.Count)
                throw new ArgumentException("A champion can only enter once");
            var party = partyFrontFirst.Select(id => _team.FirstOrDefault(h => h.Id == id)
                ?? throw new ArgumentException($"{id} is not in the team")).ToList();
            if (party.Any(h => h.IsDown)) throw new ArgumentException("Down champions can't enter (CMB-34)");
            if (marginHero != null && (!Scene.MarginPath || !partyFrontFirst.Contains(marginHero)))
                throw new ArgumentException($"{marginHero} can't take the Margin: needs a Margin Path and a party member (CMB-17)");
            if (!Allowed(partyFrontFirst).Contains(option))
                throw new ArgumentException($"{option} isn't allowed here with {party.Count} champion(s) (CMB-7)");
            var enemies = Scene.Enemies.ToList();
            if (option == RoomOption.StrikeOut &&
                (strikeTarget < 0 || strikeTarget >= enemies.Count || _db.Enemy(enemies[strikeTarget]).Elite))
                throw new ArgumentException("Strike Out needs a non-Elite target (BAL-24)");

            foreach (var h in _team.Where(h => !h.IsDown && !party.Contains(h)))
                h.Hp = Math.Min(h.MaxHp, h.Hp + Pct(h.MaxHp, _db.StageRules.DoorHealPercent));

            _party = party;
            var start = new SceneStart { Option = option, Chance = Chance(option, partyFrontFirst) };
            var roll = new SeededRandom(_seeds.NextULong());
            if (start.Chance < 100) start.Success = roll.RollPercent(start.Chance);

            Side? opening = null;
            int openingIndex = -1, crit = 0;
            switch (option)
            {
                case RoomOption.MarginStrike:
                    opening = Side.Heroes; crit = _db.StageRules.MarginStrikeCritPercent; break;
                case RoomOption.SpinATale:
                    opening = start.Success ? Side.Heroes : Side.Enemies; break; // Enthral: enemies skip
                case RoomOption.BetweenTheLines:
                    if (start.Success) { PassScene(); return start; }
                    opening = Side.Enemies; break; // spotted: enemies act first
                case RoomOption.StrikeOut:
                    if (start.Success)
                    {
                        enemies.RemoveAt(strikeTarget); // crossed out of the story
                        if (enemies.Count == 0) { PassScene(); return start; }
                    }
                    else { opening = Side.Enemies; openingIndex = strikeTarget; } // target strikes first
                    break;
            }

            Fight = new Fight(_db, new FightSetup
            {
                Opening = opening,
                OpeningEnemyIndex = openingIndex,
                OpeningCritBonus = crit,
                Heroes = party.Select(h => new HeroEntry
                {
                    ChampionId = h.Id, Level = h.Level, Rank = h.Rank, Hp = h.Hp,
                    Spot = h.Id == marginHero ? Spot.Behind : Spot.Main,
                }).ToList(),
                Enemies = enemies.Select(id => new EnemyEntry { EnemyId = id, Level = Stage.EnemyLevel }).ToList(),
                Bench = _team.Where(h => !h.IsDown && !party.Contains(h))
                    .Select(h => new HeroEntry { ChampionId = h.Id, Level = h.Level, Rank = h.Rank, Hp = h.Hp }).ToList(),
                HeroCapacity = Scene.Capacity,
                Arrivals = Scene.Arrivals.Select(a => new ArrivalEntry
                    { EnemyId = a.Enemy, Level = Stage.EnemyLevel, InRounds = a.InRounds }).ToList(),
                Reinforcements = Scene.Reinforcements.Select(id => new EnemyEntry { EnemyId = id, Level = Stage.EnemyLevel }).ToList(),
                Seed = _seeds.NextULong(),
            });
            State = StageState.Fighting;
            start.Fight = Fight;
            return start;
        }

        private HeroClass ClassOf(string id) => _db.Champion(id).Class;

        /// <summary>Scene passed without a fight (stealth success).</summary>
        private void PassScene()
        {
            if (SceneIndex + 1 >= Stage.Scenes.Count) State = StageState.Cleared;
            else SceneIndex++;
        }

        /// <summary>
        /// Call once the scene's fight is over: HP and Down carry back (CMB-33/34). Win → next scene or
        /// Cleared; loss → Failed (all champions Down or round cap; waiting ones could Join, CMB-10).
        /// </summary>
        public void EndScene()
        {
            if (State != StageState.Fighting || Fight == null) throw new InvalidOperationException("No fight running");
            if (!Fight.IsOver) throw new InvalidOperationException("The fight isn't over");
            // CMB-33: HP carries. Joined units and fall-backs both count; a unit's last state wins.
            foreach (var u in Fight.Units.Where(u => u.Side == Side.Heroes))
                Hero(u.DefId).Hp = u.Hp;

            if (Fight.Outcome != FightOutcome.Won || _team.All(h => h.IsDown)) State = StageState.Failed;
            else if (SceneIndex + 1 >= Stage.Scenes.Count) State = StageState.Cleared;
            else { SceneIndex++; State = StageState.ChooseParty; }
            Fight = null;
        }

        /// <summary>CMB-35: leave any time, free.</summary>
        public void Retreat()
        {
            if (IsOver) throw new InvalidOperationException("Stage is over");
            Fight = null;
            State = StageState.Retreated;
        }

        private static int Pct(int value, int percent) =>
            (int)Math.Round(value * percent / 100m, MidpointRounding.AwayFromZero);
    }
}
