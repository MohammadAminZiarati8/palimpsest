# Unity Client — Decided (D-63..65)
> Read when: Unity work. Principles A1–A11: `architecture.md`.

## Setup
- Unity 6.3 LTS (`6000.3.23f1`, pinned in `ProjectVersion.txt`), template Universal 2D (URP 2D renderer). Android only, landscape (OVR-7). IL2CPP, ARM64.
- Player settings live in code: `Game.Editor.ProjectSetup.Apply` (menu *Game/Apply Project Setup*). App id `com.ziarati.palimpsest` is a placeholder until Q-24.
- UniTask + PrimeTween come from the OpenUPM scoped registry (`Packages/manifest.json`). Template extras removed: Collab, Visual Scripting, Multiplayer Center.
- UI: uGUI + TextMeshPro (D-65). Characters: 2D Animation + PSD Importer (Unity official) for cut-out sprites (D-69, ART-6). Animation: PrimeTween. Async: UniTask. JSON: Newtonsoft (Unity package).
- Login: Unity Authentication package. Net: HTTP+JSON via `UnityWebRequest` + UniTask; DTOs from `shared`.
- Later: NuGetForUnity + SignalR client (live PvP).
- Every `dotnet build`/`dotnet test` copies the `shared` DLL to `client/Assets/Plugins/Shared/` and `content/**/*.json` to `client/Assets/_Game/Resources/Content/` (read at runtime by `Game.Core.ResourcesContentSource`; check with menu *Game/Check Content*) (target `CopyToUnity` in `Game.Shared.csproj`, M0-6). Both are gitignored; never edit the copies. Load content by path, not by asset reference (metas differ per machine).

## Assemblies (A9)
| asmdef | Layer | Holds |
|---|---|---|
| `Game.Core` | Core | Player state cache, content loading, services |
| `Game.Net` | Core | API client, auth, request IDs (A6) |
| `Game.Screens` | Screens | Screen logic, plain C#; no `UnityEngine.UI` reference |
| `Game.Views` | Views | uGUI MonoBehaviours, prefabs bind here |
| `Game.Combat` | Views | Battle presentation; sim itself is in `shared` |
| `Game.Editor` | Editor | Editor tools |

## Folder layout
```
client/Assets/_Game/
  Scripts/{Core,Net,Screens,Views,Combat,Editor}/
  Resources/Content/ (generated)   Scenes/   Prefabs/   Art/   Audio/   UI/   link.xml (keeps Game.Shared for IL2CPP)
```

## Rules for Claude
- Create/modify scenes, prefabs and components via Unity MCP / Editor scripts, not by editing YAML.
- Keep MonoBehaviours thin; logic goes in `shared/` or `Game.Screens`.
- Placeholder art (colored shapes) until real art exists.

## Status
M0-5: `client/` created, packages resolve, asmdefs compile, Android setup applied (batch: `unity run client -- -executeMethod Game.Editor.ProjectSetup.Apply`). No scenes/code yet. M0-6: shared DLL + content copy verified (Unity compiles code using `Game.Shared`).
M2a-T7: menu *Game/Test Fight* creates/opens `Scenes/TestFight.unity` with `Game.Combat.FightScreen` (uGUI built from code, placeholder boxes; heroes/enemies/seed on the Fight object).
M2b-T7: menu *Game/Play Stages* creates/opens `Scenes/Stages.unity` with `Game.Combat.StageScreen` (stage list → scene preview + deploy → `FightScreen` stage mode → after-scene panel). Shared placeholder uGUI helpers: `Game.Combat.Ui`. Progress = PlayerPrefs (`StageProgress`, D-110).
M2a-T8: the screen opens on a Seedbed 1–6 test-fight picker (`TestFights.cs`), shows floating HP numbers (PrimeTween `Tween.Custom`) and a win/lose panel.
