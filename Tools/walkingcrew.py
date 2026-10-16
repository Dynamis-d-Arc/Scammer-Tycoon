"""Re-pixelate crew_walking.png to the seated crew's on-screen pixel density.

The walking frames were drawn about four times finer than every other sprite
once scaled into the room (540 source pixels tall at 1.15 tiles, versus about
142 source pixels per tile for workstations). This snaps each frame to a coarse
art-pixel grid (BLOCK source pixels per art pixel), uses the reference palette,
darkens the silhouette rim to the house outline colour, and scales back up so
the texture size and in-game proportions stay the same.

Usage: python Tools/walkingcrew.py [source.png] [output.png]
The original is kept as Tools/source/crew_walking_original.png.
"""
import os
import shutil
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixelstyle as ps  # noqa: E402

BLOCK = 10
COLUMNS, ROWS = 4, 2


def pixelate_cell(cell, block=BLOCK):
    """Each art pixel takes its block's dominant palette colour, so features stay
    crisp; blocks with a solid share of outline-dark pixels stay dark."""
    h, w = cell.shape[:2]
    gh, gw = h // block, w // block
    # Anchor the grid at the feet (bottom) and centre horizontally.
    oy, ox = h - gh * block, (w - gw * block) // 2
    grid = cell[oy:oy + gh * block, ox:ox + gw * block]
    pal = ps.palette()
    opaque = grid[:, :, 3] > 127
    snapped = ps.quantize(grid[:, :, :3].astype(np.float32))
    index = np.zeros(opaque.shape, dtype=np.int64)
    flat = snapped.reshape(-1, 3)
    lookup = {tuple(c): i for i, c in enumerate(pal.astype(np.float32))}
    index.reshape(-1)[:] = [lookup[tuple(c)] for c in flat]
    block_id = (np.arange(gh * block)[:, None] // block) * gw + (np.arange(gw * block)[None, :] // block)
    counts = np.bincount((block_id[opaque] * len(pal) + index[opaque]), minlength=gh * gw * len(pal)).reshape(gh * gw, len(pal))
    coverage = counts.sum(axis=1) / (block * block)
    dark = (pal @ np.array([0.3, 0.59, 0.11])) < 40
    dark_share = counts[:, dark].sum(axis=1) / np.maximum(counts.sum(axis=1), 1)
    choice = np.where(dark_share > 0.28, np.argmax(np.where(dark, counts, -1), axis=1), counts.argmax(axis=1))
    solid = (coverage >= 0.42).reshape(gh, gw)
    small = np.zeros((gh, gw, 4), dtype=np.uint8)
    small[:, :, :3] = pal[choice].reshape(gh, gw, 3)
    small[:, :, 3] = np.where(solid, 255, 0)
    rim = solid & ~(np.roll(solid, 1, 0) & np.roll(solid, -1, 0) & np.roll(solid, 1, 1) & np.roll(solid, -1, 1))
    small[rim, :3] = (small[rim, :3] * 0.3 + ps.outline_color() * 0.7).astype(np.uint8)
    big = np.repeat(np.repeat(small, block, axis=0), block, axis=1)
    out = np.zeros_like(cell)
    out[oy:oy + gh * block, ox:ox + gw * block] = big
    return out


def main(source, output):
    sheet = np.asarray(Image.open(source).convert("RGBA"))
    h, w = sheet.shape[:2]
    out = np.zeros_like(sheet)
    for r in range(ROWS):
        for c in range(COLUMNS):
            y0, y1 = round(r * h / ROWS), round((r + 1) * h / ROWS)
            x0, x1 = round(c * w / COLUMNS), round((c + 1) * w / COLUMNS)
            out[y0:y1, x0:x1] = pixelate_cell(sheet[y0:y1, x0:x1])
    Image.fromarray(out, "RGBA").save(output)
    print("wrote", output)


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    original = os.path.join(here, "source", "crew_walking_original.png")
    target = os.path.join(ps.ART, "crew_walking.png")
    if not os.path.exists(original):
        os.makedirs(os.path.dirname(original), exist_ok=True)
        shutil.copyfile(target, original)
    main(sys.argv[1] if len(sys.argv) > 1 else original, sys.argv[2] if len(sys.argv) > 2 else target)
