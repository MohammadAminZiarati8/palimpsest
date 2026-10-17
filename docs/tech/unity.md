# Unity Client — Decided (D-63..65)
> Read when: Unity work. Principles A1–A11: `architecture.md`.

## Setup
- Unity 6 LTS, 2D URP. Android only, landscape (OVR-7). IL2CPP, ARM64.
- UI: uGUI + TextMeshPro (D-65). Characters: 2D Animation + PSD Importer (Unity official) for cut-out sprites (D-69, ART-6). Animation: PrimeTween. Async: UniTask. JSON: Newtonsoft (Unity package).
- Login: Unity Authentication package. Net: HTTP+JSON via `UnityWebRequest` + UniTask; DTOs from `shared`.
- Later: NuGetForUnity + SignalR client (live PvP).
- `scripts/` copies the built `shared` DLL to `client/Assets/Plugins/Shared/` and `content/` to `client/Assets/_Game/Content/` (M0-6). Never edit those copies.

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
  Content/   Scenes/   Prefabs/   Art/   Audio/   UI/
```

## Rules for Claude
- Create/modify scenes, prefabs and components via Unity MCP / Editor scripts, not by editing YAML.
- Keep MonoBehaviours thin; logic goes in `shared/` or `Game.Screens`.
- Placeholder art (colored shapes) until real art exists.

## Status
Unity project not created yet (M0-5).
