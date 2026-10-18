#!/bin/sh
# usage: shot.sh name -> TestResults/playtest/name.png. Renders overlay canvases through the camera
# into a texture, so it works while the Editor is unfocused (Game view does not repaint then).
root=$(cd "$(dirname "$0")/../.." && pwd -W); mkdir -p "$root/TestResults/playtest"; out="$root/TestResults/playtest/$1.png"
code='var cam = UnityEngine.Camera.main ?? UnityEngine.Object.FindAnyObjectByType<UnityEngine.Camera>();
var rt = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var all = UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None);
var canvases = new System.Collections.Generic.List<UnityEngine.Canvas>();
foreach (var c in all) if (c.isRootCanvas && c.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay) canvases.Add(c);
foreach (var c in canvases) { c.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 1f + (100 - c.sortingOrder) * 0.01f; }
var old = cam.targetTexture; cam.targetTexture = rt;
UnityEngine.Canvas.ForceUpdateCanvases(); cam.Render();
UnityEngine.RenderTexture.active = rt;
var tex = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
UnityEngine.RenderTexture.active = null; cam.targetTexture = old;
foreach (var c in canvases) { c.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
System.IO.File.WriteAllBytes("OUT", tex.EncodeToPNG());
UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
return "saved OUT (" + canvases.Count + " canvases)";'
code=$(printf '%s' "$code" | sed "s#OUT#$out#g")
unity command eval --code "$code" --no-banner --caller plugin --skill unity-cli --result-only 2>&1 | grep -E '"result"|error|message' | head -5
