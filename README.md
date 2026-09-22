# LITTLE EMBER — Project Root & Agent Handoff

> **Read this first.** Written 17 Sep 2026 for the agent who picks this project up in a new session.

## What this is

Unity **6000.6.1f1** project (URP 2D config) for **LITTLE EMBER** — a top-down action-adventure RPG (Zelda-like).
Brief from the owner: *simple, easy-to-digest story — fun and exciting, not complicated — 20–30 h game, maximize the art with beautiful, deliberate design (no random asset dumping).*

**The design bible is `Docs/LITTLE_EMBER_GRAND_DESIGN.md`** — full story (5 Everflames to relight, 3 twists), cast (Pip, Cinder the fox, Kael, the Hollow Frost), Kindling leveling system, 12 Flame Arts, items, 5 Warden bosses, content budget, asset map, and the "Two-State World" art direction. Build to that document.

## State on arrival — verified working

| Item | Status |
|---|---|
| Project skeleton | Cloned from sandbox `C:\Pixel Game Master\Pixel Game` (Packages, ProjectSettings, Assets\Settings, Assets\Scenes) |
| First Unity import | **DONE** — `Exiting batchmode successfully now!`, **0 `error CS`**, 23,571 imports logged (`Logs\import.log`) |
| Script assemblies | `Assembly-CSharp.dll`, `FangAutoTile.dll`, `FangAutoTile.Editor.dll` compiled |
| `productName` | `Little Ember` |
| `activeInputHandler` | **`2` (Both)** — the known input trap is already fixed; sample scenes can be Played |
| `Assets\Gif` | **Junction** → `C:\Pixel Game Master\PixelAssetLibrary\Gif` (18,348 assets, canonical source) |
| Game code | **None yet** — next step is M0 (below) |

## Hard rules — do not cross

1. **`C:\Pixel Game Master` is READ-ONLY.** Assets only. Never write docs/notes/exports there, never rename/restructure it.
2. **Never import the `.unitypackage`** into this project — the junction already counts as imported; double GUIDs = conflict.
3. **Never touch `.meta` files**, never rename/move assets outside the Unity Editor (references ride on GUIDs).
4. **Delete junctions with `cmd /c rmdir` or `Remove-Item` only** — never recursive delete through the link, it can erase the real library.
5. Sprites ship correctly configured: **PPU 16, Point filter, uncompressed, pre-sliced.** Do not "optimize".
6. The pack has **zero audio and zero UI** — budget both as real workstreams.
7. License: assets may ship in the finished game; **redistribution of asset files is prohibited** — never commit `Assets/Gif/` or copy the library for anyone.

## Resuming work — first commands

```powershell
# sanity: project opens clean (also re-imports anything stale after the folder move)
& 'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath 'C:\LittleEmber' -logFile 'C:\LittleEmber\Logs\open.log'
(Select-String 'C:\LittleEmber\Logs\open.log' -Pattern 'error CS').Count   # expect 0
```

Then **M0 Foundation** (design doc Part VIII):
1. Script-generate Animator controllers/clips from the pre-sliced sheets (Hero: 16 groups × 4 dirs × 5 colors; Monsters: 5 × 8 frames; ARPG: 12 sets × 6 frames) — do not hand-click these.
2. Emberholt greybox scene (layout reference: `Assets/Gif/Super_Retro_Collection/Samples/village_sample`).
3. Core loop: movement (4-dir), lantern point-light (URP 2D Light — Renderer2D already configured), sword combo, one pushable block + one chest (`Prefabs_with_behavior`).

## Open question to settle with the owner

Story canon. This project was commissioned as **LITTLE EMBER** (simple, fun, 3 twists). A separate, much darker/complex alternative bible — **`C:\Pixel Game Factory Deepseek41\LANTERN_AND_ASH_STORY_BIBLE.md`** ("Lantern & Ash", memory-as-fuel, 7 twists, 4 endings, marked "the chosen game" by that workspace's README) — also exists. The owner's latest direct brief favors LITTLE EMBER; if told otherwise, adapt.

## Reference docs (old workspace — read-only reference, do not move)

| File | What it gives you |
|---|---|
| `C:\Pixel Game Factory Deepseek41\AGENT_GUIDE_PIXEL_ASSETS.md` | Junction workflow, 6 proven traps, measured timings, troubleshooting |
| `C:\Pixel Game Factory Deepseek41\SUPER_RETRO_COLLECTION_INVENTORY.md` | Exact asset counts, sizes, frame maps |
| `C:\Pixel Game Factory Deepseek41\GAME_RECOMMENDATIONS.md` | Genre fit analysis |
| `C:\Pixel Game Factory Deepseek41\README.md` | Original folder rules |

## Key facts about the asset pack (verified)

- Hero: 5 colors × 16 anim groups × 4 dirs @ 32×32 · ARPG: 32 chars × 12 attack sets × 6 frames @ 96×128 (skill VFX)
- Bosses: `Monsters_01–05` (4-dir walkers, 8 frames) + 6 front-view bonus monsters · Battlers: 57 front-view enemies · Backgrounds: 11 painterly 320×240 plates
- Environments: 14,505 tile assets + 435 FangAutoTile rules, 5 color themes (green/pink/autumn/crystal/ash) + beach + gigantic-tree pack
- Props: 571 prefabs · behavior prefabs (chest, push-block, reactive grass) · 22 staged crops · animals incl. pigs baby→adult · farm tool anims
- 8 playable sample scenes under `Assets/Gif/Super_Retro_Collection/Samples/`
