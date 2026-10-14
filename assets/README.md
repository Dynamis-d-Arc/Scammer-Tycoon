# Scam Den asset kit: Unity import

Sheets: `scamden_basement_sheet.png`, `scamden_neon_sheet.png` (1280×5120, transparent)

## Quick start
Copy this folder into your project's `Assets/` (e.g. `Assets/ScamDen/`). `Editor/ScamDenSheetImporter.cs` applies the import settings below and slices both sheets into named sprites: `basement_desk_0`, `neon_chair_270`, … Needs Unity 2022.3 or later with the 2D Sprite package (included in the 2D templates). If the sheets were imported before the script, right-click them → Reimport.

## Import settings
The importer script applies these; set them by hand only if you don't use it.
- Texture Type: Sprite (2D and UI) · Sprite Mode: Multiple
- Pixels Per Unit: 64 (1 floor tile = 64×32 px)
- Max Size: 8192 — the default 2048 shrinks the 5120 px sheets to 40%
- Compression: High Quality — lower settings band the faint neon glows · Filter Mode: Bilinear · Generate Mip Maps: off
- Sprite Editor → Slice → Grid By Cell Size 320×320, Pivot Custom (0.5, 0.35) — the footprint centre. Hand-sliced sprites are named `<sheet>_<row × 4 + column>`.

## Scene setup
- Tilemap: Isometric, Cell Size (1, 0.5, 1)
- Sorting: Transparency Sort Mode = Custom Axis (0, 1, 0). With URP (the default for Unity 6 2D projects) this is set on the Renderer 2D Data asset; Project Settings → Graphics only applies to the built-in renderer.
- Sprite Renderers: Sprite Sort Point = Pivot. The default, Center, sorts by the middle of the sprite instead of its footprint.
- Sorting layers, back to front: `Floor` (floor tilemap, rug at Order in Layer 1), `Walls` (wall tilemap), `Objects` (furniture and wall decorations). This relies on the room being open at the front, with walls on the north and west edges only; put any south or east walls on `Objects`.
- 2D lights: the glows are painted into the sprites, so URP 2D lights will darken them. Use the `Sprite-Unlit-Default` material on glowing pieces.

## Layout
Columns: 0°, 90°, 180°, 270° (clockwise). Rows, top to bottom:

| # | key | footprint (tiles) | layer |
|---|-----|------|-------|
| 0 | desk | 3×2 | floor, surface height 68.6 px (≈1.07 units) |
| 1 | monitor | 2×1 | floor or desk |
| 2 | laptop | 2×1 | floor or desk |
| 3 | chair | 2×2 | floor |
| 4 | phones | 1×1 | floor or desk |
| 5 | fridge | 1×1 | floor |
| 6 | stash (shoebox / safe) | 1×1 | floor |
| 7 | board | 3×1 | wall (use 0° on north wall, 270° on west wall) |
| 8 | bed | 2×4 | floor |
| 9 | pizza | 1×1 | floor |
| 10 | cans | 1×1 | floor or desk |
| 11 | trash | 1×1 | floor |
| 12 | light (tube / neon sign) | 3×1 | wall (0° north, 270° west) |
| 13 | rug | 4×4 | decal, under everything |
| 14 | floorTile | 1×1 | tilemap |
| 15 | wallSeg | 1×1 | tilemap |

Footprints are for 0°/180°; swap width and depth at 90°/270°.
At 0° the width runs down-right on screen and the depth down-left. On a Unity isometric grid that's along Y and X, so a 3×2 desk at 0° covers 3 cells along Y and 2 along X.

## Placing pieces
- Only `floorTile` and `wallSeg` are tiles. Add everything else as a GameObject with a Sprite Renderer, positioned at the centre of its footprint (for an even-sized footprint that's on a grid line, not a cell centre).
- `wallSeg` stands on one edge of its cell: 0° on the north (upper-right) edge, 270° on the west (upper-left) edge.
- Desk items: make the item a child of the desk and raise its sprite by the desk surface height, 68.6 px (≈1.07 units). Add a Sorting Group to the desk and put the items at Order in Layer 1, so the desk and everything on it sort as one piece.
