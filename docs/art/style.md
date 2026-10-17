# Art style — Decided (D-68)
> Read when: making or briefing any art. Reference = Assassin's Creed Rebellion look; never copy its iconic items (white hoods, hidden blades, Assassin crest) or characters.

## Rules
- **ART-1** Stylized real-time 3D game-model look: head ≈ 1/3 of height (~2.5–3 heads), slim body, thin arms, short legs.
- **ART-2** Faces mature and sculpted: small sharp eyes, defined nose/cheekbones, strong brows. Not anime, not cute.
- **ART-3** Matte hand-painted textures, no plastic shine; costumes layered and detailed (stitching, belts, buckles, trims).
- **ART-4** Each hero keeps one accent colour (e.g. Margo orange-red) and one Palimpsest motif (paper, ink, pen).
- **ART-5** Hero select / collection: hero on glowing blue ring pedestal, dark blue background.
- **ART-6** Characters = 2D cut-out sprites (D-69): one hero picture split into layered parts (PSB), animated with bones (Unity 2D Animation). Fixed 3/4 view facing one side; no frame-by-frame sprites.
- **ART-7** Demo pipeline (D-70): Gemini + ChatGPT Plus (GPT image) make the hero picture, then a **parts sheet** of the same hero (each body part whole, separated, plain background). Claude crops parts (script), assembles and rigs them in Unity (Unity MCP), animates. Fallback if a parts sheet fails: user fixes in Krita.
- Open: launch art, audio, team, budget (Q-23).

## Gemini prompt (attach AC Rebellion screenshots as style reference)
```text
Use these images ONLY as an art style reference. Match their art style exactly: the same 3D character model look, the same head-to-body proportions, face sculpting, eye size, material and texture detail, costume detail level, lighting and render quality. Do not copy any of their characters, logos or text. Create a NEW original character in that exact style: <hero description>. Full body, front 3/4 view, idle stance, standing on a glowing blue circular pedestal against a dark blue background, like a hero selection screen. No text, no UI, no logo, no watermark. Landscape 4:3.
```
