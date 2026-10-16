"""House pixel-art style shared by the generated and re-processed sprites.

The reference is the Pixel Office art (furniture, props, stations, crew):
about 4 source pixels per art pixel, soft rendered shading, warm key light
from the upper left, cream rim highlights, and thick near-black navy outlines.
Everything here samples those sheets, so generated art uses the same palette
and outline colour.
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
ART = os.path.join(ROOT, "Assets", "Resources", "PixelOffice")
REFERENCE = ["furniture", "props", "stations", "scam_crew", "scam_crew_vest", "scam_crew_beanie", "scam_crew_leather"]
ART_PIXEL = 4  # source pixels per art pixel in the reference sheets

_palette = None
_outline = None


def _opaque_pixels(name, step=5):
    rgba = np.asarray(Image.open(os.path.join(ART, name + ".png")).convert("RGBA"))
    return rgba[rgba[:, :, 3] > 200][::step, :3]


def palette(colors=128):
    """Median-cut palette of every opaque reference pixel."""
    global _palette
    if _palette is None:
        pixels = np.concatenate([_opaque_pixels(n) for n in REFERENCE]).astype(np.uint8)
        image = Image.fromarray(pixels.reshape(-1, 1, 3))
        quantized = image.quantize(colors=colors, method=Image.Quantize.MEDIANCUT)
        _palette = np.array(quantized.getpalette()[: colors * 3], dtype=np.float32).reshape(-1, 3)
    return _palette


def outline_color():
    """Median colour of reference pixels that touch transparency (the outline)."""
    global _outline
    if _outline is None:
        samples = []
        for name in ("furniture", "props"):
            rgba = np.asarray(Image.open(os.path.join(ART, name + ".png")).convert("RGBA")).astype(np.int32)
            solid = rgba[:, :, 3] > 200
            edge = solid.copy()
            edge[1:-1, 1:-1] &= ~(solid[:-2, 1:-1] & solid[2:, 1:-1] & solid[1:-1, :-2] & solid[1:-1, 2:])
            samples.append(rgba[edge][:, :3])
        _outline = np.median(np.concatenate(samples), axis=0).astype(np.uint8)
    return _outline


def quantize(rgb, pal=None):
    """Snap every pixel of an (h, w, 3) float/uint array to the nearest palette colour."""
    pal = palette() if pal is None else pal
    flat = rgb.reshape(-1, 3).astype(np.float32)
    weights = np.array([0.30, 0.59, 0.11], dtype=np.float32) * 3
    out = np.empty_like(flat)
    for start in range(0, len(flat), 65536):
        chunk = flat[start:start + 65536]
        distance = (((chunk[:, None, :] - pal[None, :, :]) ** 2) * weights).sum(axis=2)
        out[start:start + 65536] = pal[distance.argmin(axis=1)]
    return out.reshape(rgb.shape)


def add_outline(rgba, thickness=1, color=None):
    """Ring a sprite's silhouette with the house outline colour (outside the shape)."""
    color = outline_color() if color is None else color
    solid = rgba[:, :, 3] > 127
    grown = solid.copy()
    for _ in range(thickness):
        g = grown.copy()
        g[1:, :] |= grown[:-1, :]
        g[:-1, :] |= grown[1:, :]
        g[:, 1:] |= grown[:, :-1]
        g[:, :-1] |= grown[:, 1:]
        grown = g
    ring = grown & ~solid
    out = rgba.copy()
    out[ring, :3] = color
    out[ring, 3] = 255
    return out


def upscale(rgba, factor=ART_PIXEL):
    return np.repeat(np.repeat(rgba, factor, axis=0), factor, axis=1)
