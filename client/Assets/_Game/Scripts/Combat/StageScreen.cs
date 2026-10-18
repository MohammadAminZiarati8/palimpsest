#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Core;
using Game.Shared.Combat;
using Game.Shared.Content;
using Game.Shared.Enemies;
using Game.Shared.Stages;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Combat
{
    /// <summary>
    /// M2b-T7 placeholder stage flow (menu Game/Play Stages), driving the shared <see cref="StageRun"/>:
    /// stage list with stars (CMP-2, D-110) → one scene screen per scene: scene strip (stage map, CMB-4), preview
    /// (CMB-5: Reading/Watching, Margin Path, enemies, arrivals CMB-22, Herald, Notice CMB-23, Rewrite CMB-20) and
    /// deploy (party within capacity CMB-3/8, Margin flanker CMB-17, room option + % CMB-7) → the fight
    /// (<see cref="FightScreen"/> stage mode) → after-scene panel (HP carries CMB-33, Down CMB-34, next scene /
    /// cleared / failed, Retreat CMB-35, stars CMB-36).
    /// </summary>
    public sealed class StageScreen : MonoBehaviour
    {
        private static readonly RoomOption[] Options = (RoomOption[])Enum.GetValues(typeof(RoomOption));
        private static readonly RewriteUse[] Rewrites = (RewriteUse[])Enum.GetValues(typeof(RewriteUse));

        private ContentDb _db = null!;
        private FightScreen _fightScreen = null!;
        private StageRun _run = null!;
        private int _stageIndex;
        private ulong _seed;

        // Deploy choices for the current scene.
        private readonly List<string> _party = new List<string>();
        private string? _margin;
        private RoomOption _option;
        private int _strikeTarget = -1;

        // UI
        private RectTransform _root = null!, _stripRoot = null!;
        private GameObject _list = null!, _scene = null!, _after = null!;
        private readonly List<Button> _stageButtons = new List<Button>();
        private Button _unlockAll = null!;
        private Text _sceneHeader = null!, _details = null!, _extra = null!, _deployInfo = null!, _optionInfo = null!, _afterText = null!;
        private readonly List<(Image Box, Outline Border, Text Text)> _strip = new List<(Image, Outline, Text)>();
        private readonly List<Button> _enemyButtons = new List<Button>();
        private readonly List<Button> _heroButtons = new List<Button>();
        private readonly List<Button> _optionButtons = new List<Button>();
        private readonly List<Button> _rewriteButtons = new List<Button>();
        private Button _marginButton = null!, _enter = null!, _afterA = null!, _afterB = null!;
        private UnityAction? _onAfterA, _onAfterB;

        private StageDef Stage => _db.Stages[_stageIndex];

        private void Awake()
        {
            _db = ContentLoader.Load(new ResourcesContentSource());
            BuildUi();
            var fight = new GameObject("Fight");
            fight.transform.SetParent(transform, false);
            _fightScreen = fight.AddComponent<FightScreen>();
            _fightScreen.UseForStages();
            ShowList();
        }

        // ---------- flow ----------

        private void ShowList()
        {
            _root.gameObject.SetActive(true);
            _list.SetActive(true);
            _scene.SetActive(false);
            _after.SetActive(false);
            RefreshList();
        }

        private void StartStage(int index)
        {
            _stageIndex = index;
            _seed = (ulong)DateTime.Now.Ticks;
            _run = new StageRun(_db, Stage, DemoTeam.For(_db, Stage), _seed);
            BuildStrip();
            _list.SetActive(false);
            _scene.SetActive(true);
            _after.SetActive(false);
            NewScene();
        }

        private void NewScene()
        {
            _after.SetActive(false);
            _party.Clear();
            _margin = null;
            _option = RoomOption.Clash;
            _strikeTarget = -1;
            RefreshScene();
        }

        private void ToggleHero(string id)
        {
            if (_party.Remove(id)) { if (_margin == id) _margin = null; }
            else if (_party.Count < _run.Scene.Capacity) _party.Add(id);
            KeepOptionValid();
            RefreshScene();
        }

        private void CycleMargin()
        {
            var choices = new List<string?> { null };
            choices.AddRange(_party);
            _margin = choices[(choices.IndexOf(_margin) + 1) % choices.Count];
            RefreshScene();
        }

        private void PickOption(RoomOption option)
        {
            _option = option;
            if (option != RoomOption.StrikeOut) _strikeTarget = -1;
            RefreshScene();
        }

        private void PickEnemy(int index)
        {
            if (_option != RoomOption.StrikeOut || _db.Enemy(_run.Scene.Enemies[index]).Elite) return;
            _strikeTarget = index;
            RefreshScene();
        }

        private void DoRewrite(RewriteUse use)
        {
            if (!_run.CanRewrite(use)) return;
            _run.Rewrite(use);
            if (!_run.SceneMarginPath) _margin = null;
            KeepOptionValid();
            RefreshScene();
        }

        private void KeepOptionValid()
        {
            if (_party.Count == 0 || !_run.Allowed(_party).Contains(_option)) _option = RoomOption.Clash;
            if (_option != RoomOption.StrikeOut) _strikeTarget = -1;
        }

        private void EnterScene()
        {
            int sceneNo = _run.SceneIndex + 1;
            SceneStart start;
            try
            {
                start = _run.Enter(_party.ToList(), _option, _option == RoomOption.StrikeOut ? _strikeTarget : -1, _margin);
            }
            catch (Exception e)
            {
                _optionInfo.text = $"<color=#ff8080>{e.Message}</color>";
                return;
            }
            string how = OptionResult(start);
            if (start.Fight == null) { ShowAfter($"Scene {sceneNo}: {how}\nSlipped through without a fight."); return; }

            _root.gameObject.SetActive(false);
            _fightScreen.PlayStage(start.Fight, $"{Stage.Name} scene {sceneNo}/{Stage.Scenes.Count}: {how}",
                _run.Scene.Capacity, () => FightDone(sceneNo), Retreat);
        }

        private void FightDone(int sceneNo)
        {
            _fightScreen.Hide();
            bool won = _run.Fight!.Outcome == FightOutcome.Won;
            _run.EndScene();
            _root.gameObject.SetActive(true);
            ShowAfter($"Scene {sceneNo} {(won ? "won" : "lost")}");
        }

        /// <summary>CMB-35: free, any time (scene screen, fight, after a scene).</summary>
        private void Retreat()
        {
            _fightScreen.Hide();
            if (!_run.IsOver) _run.Retreat();
            _root.gameObject.SetActive(true);
            ShowAfter("Retreated");
        }

        private void ShowAfter(string headline)
        {
            var sb = new StringBuilder($"<b>{headline}</b>\n");
            switch (_run.State)
            {
                case StageState.ChooseParty:
                    sb.Append($"<size=26>Next: scene {_run.SceneIndex + 1}/{Stage.Scenes.Count} · Notice {_run.Notice}</size>\n");
                    SetAfter("Next scene", NewScene, "Retreat", Retreat);
                    break;
                case StageState.Cleared:
                    int stars = _run.Stars;
                    bool better = StageProgress.Record(Stage.Id, stars);
                    sb.Append($"<size=34>Stage cleared! Stars {stars}/3{(better ? " (new best)" : $" (best {StageProgress.Stars(Stage.Id)})")}</size>\n");
                    SetAfter("Stages", ShowList, "Replay", () => StartStage(_stageIndex));
                    break;
                case StageState.Failed:
                    sb.Append("<size=34>Stage failed</size>\n");
                    SetAfter("Stages", ShowList, "Retry", () => StartStage(_stageIndex));
                    break;
                default:
                    SetAfter("Stages", ShowList, "Retry", () => StartStage(_stageIndex));
                    break;
            }
            sb.Append("<size=26>HP carries to the next scene (CMB-33):</size>");
            foreach (var h in _run.Team)
                sb.Append($"\n<size=26>{_db.Champion(h.Id).Name}  {h.Hp}/{h.MaxHp}{(h.IsDown ? "  <color=#ff8080>DOWN</color>" : "")}</size>");
            _afterText.text = sb.ToString();
            _after.SetActive(true);
            RefreshScene();
        }

        private void SetAfter(string a, UnityAction onA, string b, UnityAction onB)
        {
            Ui.SetText(_afterA, a);
            Ui.SetText(_afterB, b);
            _onAfterA = onA;
            _onAfterB = onB;
        }

        // ---------- view ----------

        private void RefreshList()
        {
            for (int i = 0; i < _stageButtons.Count; i++)
            {
                var s = _db.Stages[i];
                bool open = StageProgress.Unlocked(_db.Stages, i);
                int stars = StageProgress.Stars(s.Id);
                var b = _stageButtons[i];
                b.interactable = open;
                Ui.SetText(b, $"<b>{s.Name}</b>\n{(open ? stars > 0 ? $"Stars {stars}/3" : "not cleared" : "Locked")}" +
                              $"\n<size=20>{s.Scenes.Count} scene(s) · enemies Lv{s.EnemyLevel}</size>");
                b.image.color = !open ? Ui.Dim : stars == 3 ? new Color(1f, 0.92f, 0.6f) : Color.white;
            }
            Ui.SetText(_unlockAll, $"Unlock all (test): {(StageProgress.UnlockAll ? "on" : "off")}");
        }

        private void RefreshScene()
        {
            if (_run == null) return;
            var scene = _run.Scene;
            int idx = _run.SceneIndex;
            _sceneHeader.text = $"{Stage.Name} · Scene {idx + 1}/{Stage.Scenes.Count} · enemies Lv{Stage.EnemyLevel} · " +
                                $"Notice {_run.Notice} · Rewrite {(_run.RewriteLeft ? "ready" : "used")}  <size=20>(seed {_seed})</size>";

            for (int j = 0; j < _strip.Count; j++)
            {
                var (box, border, text) = _strip[j];
                var s = Stage.Scenes[j];
                bool current = j == idx && !_run.IsOver;
                box.color = j < idx || _run.State == StageState.Cleared ? Ui.Dim : current ? Ui.Hero : new Color(0.25f, 0.25f, 0.28f);
                border.enabled = current;
                var alert = current ? _run.SceneAlert : s.Alert;
                bool margin = current ? _run.SceneMarginPath : s.MarginPath;
                text.text = $"<b>Scene {j + 1}</b> · {s.Capacity} hero{(s.Capacity > 1 ? "es" : "")}\n{alert}{(margin ? " · Margin Path" : "")}\n" +
                            string.Join(", ", s.Enemies.Select(e => _db.Enemy(e).Name)) +
                            (s.Arrivals.Count > 0 ? $"\n+{s.Arrivals.Count} nearby" : "");
            }

            bool reading = _run.SceneAlert == Alert.Reading;
            _details.text = $"<b>Scene {idx + 1}: {_run.SceneAlert}</b>  " +
                            (reading ? "enemies haven't seen you: stealth options open" : "enemies are ready: Clash or Spin a Tale only") +
                            $"\nMargin Path: {(_run.SceneMarginPath ? "open (one champion may enter behind the enemy Back)" : "none")}" +
                            "\nEnemies, Front first" + (_option == RoomOption.StrikeOut ? " <color=#80ff90>(tap one to Strike Out)</color>" : "") + ":";

            for (int i = 0; i < _enemyButtons.Count; i++)
            {
                var b = _enemyButtons[i];
                bool show = i < scene.Enemies.Count;
                b.gameObject.SetActive(show);
                if (!show) continue;
                var def = _db.Enemy(scene.Enemies[i]);
                var st = EnemyStatCalculator.At(_db, def, Stage.EnemyLevel);
                Ui.SetText(b, $"{i + 1}. <b>{def.Name}</b> ({def.Role}{(def.Elite ? ", Elite" : "")})  HP {st.Hp} Atk {st.Attack} Def {st.Defense} Spd {st.Speed}");
                b.image.color = i == _strikeTarget ? Ui.Picked : Color.white;
            }

            _extra.text = ExtraInfo(scene);
            for (int i = 0; i < _rewriteButtons.Count; i++)
            {
                var use = Rewrites[i];
                _rewriteButtons[i].interactable = _run.CanRewrite(use);
                _rewriteButtons[i].image.color = _run.SceneRewrite == use ? Ui.Picked : Color.white;
            }

            // Deploy
            _deployInfo.text = $"<b>Deploy</b>: up to {scene.Capacity}. Tap heroes in order, first = Front.";
            for (int i = 0; i < _heroButtons.Count; i++)
            {
                var b = _heroButtons[i];
                bool show = i < _run.Team.Count;
                b.gameObject.SetActive(show);
                if (!show) continue;
                var h = _run.Team[i];
                var def = _db.Champion(h.Id);
                int at = _party.IndexOf(h.Id);
                string tag = h.IsDown ? "  <color=#c00000>DOWN</color>"
                    : at < 0 ? "  waiting"
                    : $"  <b>#{at + 1}{(at == 0 ? " Front" : "")}{(_margin == h.Id ? " (Margin)" : "")}</b>";
                Ui.SetText(b, $"{def.Name} ({def.Class}) Lv{h.Level} R{h.Rank} · HP {h.Hp}/{h.MaxHp}{tag}");
                b.interactable = !h.IsDown;
                b.image.color = at >= 0 ? Ui.Picked : Color.white;
            }

            _marginButton.gameObject.SetActive(_run.SceneMarginPath && _party.Count > 0);
            Ui.SetText(_marginButton, $"Margin flanker: {(_margin == null ? "none" : _db.Champion(_margin).Name)}  (tap to change)");

            var allowed = _party.Count > 0 ? _run.Allowed(_party) : new List<RoomOption>();
            for (int i = 0; i < _optionButtons.Count; i++)
            {
                var o = Options[i];
                var b = _optionButtons[i];
                bool show = allowed.Contains(o);
                b.gameObject.SetActive(show);
                if (!show) continue;
                Ui.SetText(b, $"{OptionName(o)}\n{_run.Chance(o, _party)}%");
                b.image.color = _option == o ? Ui.Picked : Color.white;
            }

            var waiting = _run.Team.Where(h => !h.IsDown && !_party.Contains(h.Id)).Select(h => _db.Champion(h.Id).Name).ToList();
            _optionInfo.text = (_party.Count == 0 ? "Pick at least one hero." : $"<b>{OptionName(_option)}</b>: {OptionHelp(_option)}") +
                               (_option == RoomOption.StrikeOut && _strikeTarget < 0 ? "\n<color=#80ff90>Tap a non-Elite enemy on the left.</color>" : "") +
                               (waiting.Count > 0 ? $"\nWaiting at the door (heal {_db.StageRules.DoorHealPercent}%, may Join mid-fight): {string.Join(", ", waiting)}" : "");
            _enter.interactable = !_run.IsOver && _party.Count > 0 && allowed.Contains(_option) &&
                                  (_option != RoomOption.StrikeOut || _strikeTarget >= 0);
        }

        private string ExtraInfo(SceneDef scene)
        {
            var r = _db.StageRules;
            var sb = new StringBuilder();
            foreach (var a in scene.Arrivals)
                sb.AppendLine($"Nearby: {_db.Enemy(a.Enemy).Name} joins after round {a.InRounds}");
            if (_run.ScenePatrol)
                sb.AppendLine($"Patrol (Notice {r.NoticePatrolAt}+): {_db.Enemy(r.NoticePatrol.Enemy).Name} joins after round {r.NoticePatrol.InRounds}");
            var heralds = scene.Enemies.Concat(scene.Arrivals.Select(a => a.Enemy)).Select(_db.Enemy)
                .Where(d => d.Skills.Any(s => s.Special?.Type == SpecialType.Ring)).ToList();
            if (heralds.Count > 0)
            {
                int turns = heralds[0].Skills.First(s => s.Special?.Type == SpecialType.Ring).Special!.Rounds;
                sb.AppendLine(_run.SceneRewrite == RewriteUse.Silence
                    ? "Herald silenced by Rewrite: it won't ring"
                    : $"Herald: rings after {turns} of its turns, writes in {string.Join(", ", scene.Reinforcements.Select(e => _db.Enemy(e).Name))} (Notice +{r.NoticePerRing})");
            }
            sb.AppendLine($"Notice {_run.Notice}: +{r.NoticePerFight} per fight · at {r.NoticeWatchingAt}+ Reading scenes start Watching · at {r.NoticePatrolAt}+ a patrol joins");
            sb.AppendLine(_run.SceneRewrite is RewriteUse used ? $"Rewrite used here: {used}"
                : _run.RewriteLeft ? "Rewrite (once per stage): Calm = Watching to Reading · Silence = Herald won't ring · Reveal = open a Margin Path"
                : "Rewrite already spent this stage");
            sb.Append("<size=20>Enemy intents show each round in the fight (CMB-41).</size>");
            return sb.ToString();
        }

        private static string OptionName(RoomOption o) => o switch
        {
            RoomOption.MarginStrike => "Margin Strike",
            RoomOption.StrikeOut => "Strike Out",
            RoomOption.BetweenTheLines => "Between the Lines",
            RoomOption.SpinATale => "Spin a Tale",
            _ => "Clash",
        };

        private string OptionHelp(RoomOption o) => o switch
        {
            RoomOption.MarginStrike => $"you act first, +{_db.StageRules.MarginStrikeCritPercent}% crit on the opening",
            RoomOption.StrikeOut => "roll: cross one enemy out of the scene; fail = it strikes first",
            RoomOption.BetweenTheLines => "roll: slip past, no fight; fail = enemies act first",
            RoomOption.SpinATale => "roll: you act first; fail = enemies act first",
            _ => "a normal fight",
        };

        private static string OptionResult(SceneStart s) =>
            s.Chance >= 100 ? OptionName(s.Option) : $"{OptionName(s.Option)} {s.Chance}% {(s.Success ? "worked" : "failed")}";

        // ---------- UI built from code (no prefabs yet) ----------

        private void BuildUi()
        {
            var root = _root = Ui.Canvas(transform, "StageCanvas");
            var bg = Ui.Panel(root, "Background", new Color(0.1f, 0.1f, 0.12f));
            Ui.Stretch(bg.rectTransform, 0, 0, 1, 1);
            BuildList(root);
            BuildScene(root);
            BuildAfter(root);
        }

        private void BuildList(RectTransform root)
        {
            var list = Ui.Panel(root, "StageList", Color.clear);
            Ui.Stretch(list.rectTransform, 0, 0, 1, 1);
            _list = list.gameObject;
            var title = Ui.Label(list.rectTransform, "Title", 52, TextAnchor.MiddleCenter);
            title.text = "Campaign";
            Ui.Place(title.rectTransform, 0.5f, 0.9f, 800, 90);

            var chapters = _db.Stages.Select(s => s.Chapter).Distinct().ToList();
            for (int c = 0; c < chapters.Count; c++)
            {
                float y = 0.68f - c * 0.27f;
                var label = Ui.Label(list.rectTransform, chapters[c], 32, TextAnchor.MiddleCenter);
                label.text = chapters[c];
                Ui.Place(label.rectTransform, 0.5f, y + 0.11f, 600, 50);
                var inChapter = _db.Stages.Select((s, i) => (s, i)).Where(p => p.s.Chapter == chapters[c]).ToList();
                for (int k = 0; k < inChapter.Count; k++)
                {
                    int index = inChapter[k].i;
                    var b = Ui.Button(list.rectTransform, inChapter[k].s.Id, 26, () => StartStage(index));
                    Ui.Place(Ui.Rect(b), 0.5f + (k - (inChapter.Count - 1) / 2f) * 0.15f, y, 270, 140);
                    while (_stageButtons.Count <= index) _stageButtons.Add(null!);
                    _stageButtons[index] = b;
                }
            }
            _unlockAll = Ui.Button(list.rectTransform, "UnlockAll", 24, () => { StageProgress.UnlockAll = !StageProgress.UnlockAll; RefreshList(); });
            Ui.Place(Ui.Rect(_unlockAll), 0.35f, 0.08f, 340, 70);
            var reset = Ui.Button(list.rectTransform, "Reset", 24, () => { StageProgress.Reset(_db.Stages); RefreshList(); }, "Reset progress");
            Ui.Place(Ui.Rect(reset), 0.65f, 0.08f, 340, 70);
        }

        private void BuildScene(RectTransform root)
        {
            var scene = Ui.Panel(root, "Scene", Color.clear);
            Ui.Stretch(scene.rectTransform, 0, 0, 1, 1);
            _scene = scene.gameObject;
            var p = scene.rectTransform;

            _sceneHeader = Ui.Label(p, "Header", 32, TextAnchor.MiddleCenter);
            Ui.Place(_sceneHeader.rectTransform, 0.46f, 0.955f, 1600, 60);
            var retreat = Ui.Button(p, "Retreat", 24, Retreat, "Retreat");
            Ui.Place(Ui.Rect(retreat), 0.94f, 0.955f, 160, 60);

            var strip = Ui.Panel(p, "Strip", Color.clear);
            Ui.Stretch(strip.rectTransform, 0, 0.77f, 1, 0.92f);
            _stripRoot = strip.rectTransform;

            // Preview (left)
            var left = Ui.Panel(p, "Preview", new Color(1f, 1f, 1f, 0.04f));
            Ui.Stretch(left.rectTransform, 0.02f, 0.03f, 0.49f, 0.75f);
            var l = left.rectTransform;
            _details = Ui.Label(l, "Details", 24, TextAnchor.UpperLeft);
            Ui.Stretch(_details.rectTransform, 0.03f, 0.79f, 0.97f, 0.98f);
            for (int i = 0; i < Fight.MaxSlots; i++)
            {
                int index = i;
                var b = Ui.Button(l, $"Enemy{i}", 22, () => PickEnemy(index));
                b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                Ui.Place(Ui.Rect(b), 0.5f, 0.72f - i * 0.09f, 840, 54);
                _enemyButtons.Add(b);
            }
            _extra = Ui.Label(l, "Extra", 22, TextAnchor.UpperLeft);
            Ui.Stretch(_extra.rectTransform, 0.03f, 0.14f, 0.97f, 0.44f);
            for (int i = 0; i < Rewrites.Length; i++)
            {
                var use = Rewrites[i];
                var b = Ui.Button(l, $"Rewrite{use}", 24, () => DoRewrite(use), $"Rewrite: {use}");
                Ui.Place(Ui.Rect(b), 0.2f + i * 0.3f, 0.07f, 250, 60);
                _rewriteButtons.Add(b);
            }

            // Deploy (right)
            var right = Ui.Panel(p, "Deploy", new Color(1f, 1f, 1f, 0.04f));
            Ui.Stretch(right.rectTransform, 0.51f, 0.03f, 0.98f, 0.75f);
            var r = right.rectTransform;
            _deployInfo = Ui.Label(r, "Info", 26, TextAnchor.MiddleLeft);
            Ui.Stretch(_deployInfo.rectTransform, 0.03f, 0.9f, 0.97f, 0.98f);
            for (int i = 0; i < Fight.MaxSlots; i++)
            {
                int index = i;
                var b = Ui.Button(r, $"Hero{i}", 24, () => ToggleHero(_run.Team[index].Id));
                Ui.Place(Ui.Rect(b), 0.5f, 0.83f - i * 0.1f, 840, 60);
                _heroButtons.Add(b);
            }
            _marginButton = Ui.Button(r, "Margin", 24, CycleMargin);
            Ui.Place(Ui.Rect(_marginButton), 0.5f, 0.52f, 840, 56);
            for (int i = 0; i < Options.Length; i++)
            {
                var o = Options[i];
                var b = Ui.Button(r, $"Option{o}", 22, () => PickOption(o));
                Ui.Place(Ui.Rect(b), 0.11f + i * 0.195f, 0.4f, 165, 80);
                _optionButtons.Add(b);
            }
            _optionInfo = Ui.Label(r, "OptionInfo", 22, TextAnchor.UpperLeft);
            Ui.Stretch(_optionInfo.rectTransform, 0.03f, 0.14f, 0.97f, 0.33f);
            _enter = Ui.Button(r, "Enter", 32, EnterScene, "Enter scene");
            Ui.Place(Ui.Rect(_enter), 0.5f, 0.07f, 340, 70);
        }

        private void BuildAfter(RectTransform root)
        {
            var blocker = Ui.Panel(root, "AfterScene", new Color(0, 0, 0, 0.55f)); // blocks clicks on the scene behind
            Ui.Stretch(blocker.rectTransform, 0, 0, 1, 1);
            _after = blocker.gameObject;
            var after = Ui.Panel(blocker.rectTransform, "Box", new Color(0, 0, 0, 0.9f));
            Ui.Place(after.rectTransform, 0.5f, 0.5f, 900, 600);
            _afterText = Ui.Label(after.rectTransform, "Text", 44, TextAnchor.UpperCenter);
            Ui.Stretch(_afterText.rectTransform, 0.05f, 0.25f, 0.95f, 0.95f);
            _afterA = Ui.Button(after.rectTransform, "A", 28, () => _onAfterA?.Invoke());
            Ui.Place(Ui.Rect(_afterA), 0.3f, 0.12f, 260, 80);
            _afterB = Ui.Button(after.rectTransform, "B", 28, () => _onAfterB?.Invoke());
            Ui.Place(Ui.Rect(_afterB), 0.7f, 0.12f, 260, 80);
        }

        /// <summary>Stage map (CMB-4): one box per scene, joined by doors.</summary>
        private void BuildStrip()
        {
            foreach (Transform child in _stripRoot) Destroy(child.gameObject);
            _strip.Clear();
            int n = Stage.Scenes.Count;
            for (int j = 0; j < n; j++)
            {
                float x = 0.5f + (j - (n - 1) / 2f) * 0.26f;
                var box = Ui.Panel(_stripRoot, $"Scene{j + 1}", Ui.Empty);
                Ui.Place(box.rectTransform, x, 0.5f, 400, 150);
                var border = box.gameObject.AddComponent<Outline>();
                border.effectColor = Ui.Active;
                border.effectDistance = new Vector2(5, 5);
                var text = Ui.Label(box.rectTransform, "Text", 22, TextAnchor.MiddleCenter);
                Ui.Stretch(text.rectTransform, 0.03f, 0.03f, 0.97f, 0.97f);
                _strip.Add((box, border, text));
                if (j == n - 1) continue;
                var door = Ui.Label(_stripRoot, "Door", 40, TextAnchor.MiddleCenter);
                door.text = ">>";
                Ui.Place(door.rectTransform, x + 0.13f, 0.5f, 90, 60);
            }
        }
    }
}
