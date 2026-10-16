"""Generate Assets/Resources/PixelOffice/denprops.png in the house pixel-art style.

Each prop is built from bevelled boxes and cylinders, ray-cast in the game's 2:1
isometric projection with a warm key light from the upper left, then
supersampled down to art pixels, snapped to the reference palette, outlined in
the reference outline colour and upscaled 4x (see pixelstyle.py).

Usage: python Tools/denprops.py [output.png]
Prints each sprite rectangle (top-left origin) and a FurnitureCatalog width.
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixelstyle as ps  # noqa: E402

SS = 4                      # samples per art pixel along each axis
CELL_W, CELL_H = 80, 96     # art pixels per atlas cell
S = 33.0                    # art pixels per world unit (a 1x1 tile spans 2S wide)
HS = 1.0                    # one world unit up = HS * S art pixels
ORIGIN = (CELL_W / 2, CELL_H - 20)


def unit(v):
    v = np.asarray(v, dtype=np.float64)
    return v / np.linalg.norm(v, axis=-1, keepdims=True)


LIGHT = unit([0.38, 0.80, 1.0])
VIEW = unit([1.0, 1.0, 1.0 / HS])
HALF = unit(LIGHT + VIEW)
AMBIENT = np.array([0.30, 0.31, 0.46])
KEY = np.array([1.0, 0.85, 0.66]) * 0.95
SPECULAR = np.array([1.0, 0.90, 0.74])


def cellnoise(p, frequency, seed=0.0):
    q = np.floor(p * frequency)
    h = np.sin(q @ np.array([12.9898, 78.233, 37.719]) + seed) * 43758.5453
    return h - np.floor(h)


def mottle(albedo, p, amount=0.14, frequency=16):
    return albedo * (1 - amount / 2 + amount * cellnoise(p, frequency))[:, None]


# ---------- materials: (points, normals) -> (albedo, emissive) ----------
def flat(color, amount=0.06):
    color = np.array(color)
    return lambda p, n: (mottle(np.tile(color, (len(p), 1)), p, amount, 9), False)


def glow(color):
    color = np.array(color)
    return lambda p, n: (np.tile(color, (len(p), 1)), True)


def wood(plank=0.125, along=0):
    base = np.array([0.56, 0.34, 0.18])

    def paint(p, n):
        grain = np.sin(p[:, along] * 38 + cellnoise(p, 6) * 5)
        a = np.tile(base, (len(p), 1)) * (0.92 + 0.06 * grain)[:, None]
        side = np.abs(n[:, 2]) < 0.5
        groove = side & ((p[:, 2] % plank) < 0.018)
        top_groove = ~side & ((p[:, 1 - along] % 0.15) < 0.018)
        a[groove | top_groove] *= 0.45
        return mottle(a, p, 0.05, 9), False
    return paint


def cash(axis=0, center=0.0):
    green = np.array([0.33, 0.58, 0.29])
    band = np.array([0.93, 0.88, 0.68])

    def paint(p, n):
        a = np.tile(green, (len(p), 1))
        side = np.abs(n[:, 2]) < 0.5
        a[side & ((p[:, 2] * 70) % 1 < 0.35)] *= 1.12          # stacked bill edges
        a[np.abs(p[:, axis] - center) < 0.028] = band             # paper band
        return mottle(a, p, 0.08, 20), False
    return paint


def paper(lines_axis=2, face_axis=1):
    base = np.array([0.94, 0.92, 0.86])

    def paint(p, n):
        a = np.tile(base, (len(p), 1))
        facing = np.abs(n[:, face_axis]) > 0.5
        rows = (p[:, lines_axis] * 16) % 1 < 0.3
        a[facing & rows] = [0.70, 0.71, 0.76]
        return a, False
    return paint


def shred_window(y0, y1, z0, z1, body):
    def paint(p, n):
        a, _ = body(p, n)
        inside = (np.abs(n[:, 0]) > 0.5) & (p[:, 1] > y0) & (p[:, 1] < y1) & (p[:, 2] > z0) & (p[:, 2] < z1)
        strips = cellnoise(p * [1, 9, 2.5], 7) > 0.45
        a[inside] = np.where(strips[inside, None], [0.92, 0.91, 0.86], [0.32, 0.38, 0.47])
        return a, False
    return paint


def panel(face_axis, lo, hi, color, base, emissive_color=None, dots=False):
    """Paint a rectangle (in the two non-face axes) onto a box face."""
    axes = [a for a in range(3) if a != face_axis]
    color = np.array(color)

    def paint(p, n):
        a, _ = base(p, n)
        on = np.abs(n[:, face_axis]) > 0.5
        for k, axis in enumerate(axes):
            on &= (p[:, axis] > lo[k]) & (p[:, axis] < hi[k])
        if dots:
            pattern = ((p[:, axes[0]] * 40) % 1 < 0.5) & ((p[:, axes[1]] * 40) % 1 < 0.5)
            a[on] = np.where(pattern[on, None], color * 0.35, color)
        else:
            a[on] = color
        if emissive_color is not None:
            digits = on & (cellnoise(p, 30) > 0.55)
            a[digits] = emissive_color
        return a, False
    return paint


# ---------- primitives ----------
class Box:
    def __init__(self, lo, hi, material, bevel=0.035):
        self.lo, self.hi, self.material, self.bevel = np.array(lo, float), np.array(hi, float), material, bevel

    def hit(self, o, d):
        inv = 1.0 / d
        t1, t2 = (self.lo - o) * inv, (self.hi - o) * inv
        near, far = np.minimum(t1, t2), np.maximum(t1, t2)
        tnear, tfar = near.max(axis=1), far.min(axis=1)
        ok = (tnear <= tfar) & (tfar > 0)
        axis = near.argmax(axis=1)
        normal = np.zeros_like(o)
        normal[np.arange(len(o)), axis] = -np.sign(d[np.arange(len(o)), axis])
        return np.where(ok, tnear, np.inf), normal

    def shade_normal(self, p, n):
        """Bevelled normal, bevel weight (0-1) and a top-lit gradient for side faces."""
        side = np.abs(n[:, 2]) < 0.5
        height = np.clip((p[:, 2] - self.lo[2]) / max(self.hi[2] - self.lo[2], 1e-6), 0, 1)
        gradient = np.where(side, 0.86 + 0.22 * height, 1.04)
        r = self.bevel
        if r <= 0:
            return n, np.zeros(len(p)), gradient
        out, weight = n.copy(), np.zeros(len(p))
        for axis in range(3):
            off = np.abs(n[:, axis]) < 0.5
            dhi, dlo = self.hi[axis] - p[:, axis], p[:, axis] - self.lo[axis]
            whi = np.where(off & (dhi < r), np.clip(1 - dhi / r, 0, 1), 0)
            wlo = np.where(off & (dlo < r), np.clip(1 - dlo / r, 0, 1), 0)
            out[:, axis] += whi * 0.95 - wlo * 0.95
            weight = np.maximum(weight, whi)
        return unit(out), weight, gradient


class Cylinder:
    def __init__(self, cx, cy, z0, z1, radius, material, cap=None):
        self.c, self.z0, self.z1, self.r, self.material, self.cap = np.array([cx, cy]), z0, z1, radius, material, cap

    def hit(self, o, d):
        oc = o[:, :2] - self.c
        a = (d[:, :2] ** 2).sum(axis=1)
        b = 2 * (d[:, :2] * oc).sum(axis=1)
        c = (oc ** 2).sum(axis=1) - self.r ** 2
        disc = b * b - 4 * a * c
        s_side = np.where(disc >= 0, (-b - np.sqrt(np.maximum(disc, 0))) / (2 * a), np.inf)
        z = o[:, 2] + s_side * d[:, 2]
        s_side = np.where((z >= self.z0) & (z <= self.z1), s_side, np.inf)
        s_cap = (self.z1 - o[:, 2]) / d[:, 2]
        pc = o[:, :2] + s_cap[:, None] * d[:, :2] - self.c
        s_cap = np.where((pc ** 2).sum(axis=1) <= self.r ** 2, s_cap, np.inf)
        use_cap = s_cap < s_side
        s = np.minimum(s_side, s_cap)
        p = o + s[:, None] * d
        normal = np.zeros_like(o)
        side_n = np.zeros_like(o)
        side_n[:, :2] = (p[:, :2] - self.c) / self.r
        normal[:] = np.where(use_cap[:, None], [0, 0, 1], side_n)
        return s, normal

    def shade_normal(self, p, n):
        return n, np.zeros(len(p)), np.ones(len(p))


def render(prims):
    """Ray-cast one atlas cell and return an RGBA art-pixel image."""
    h, w = CELL_H * SS, CELL_W * SS
    ys, xs = np.mgrid[0:h, 0:w]
    sx, sy = (xs + 0.5) / SS, (ys + 0.5) / SS
    a = (sx - ORIGIN[0]) / S
    b = 2 * (sy - ORIGIN[1]) / S
    p0 = np.stack([(a + b) / 2, (b - a) / 2, np.zeros_like(a)], axis=-1).reshape(-1, 3)
    n_view = np.array([1.0, 1.0, 1.0 / HS])
    o = p0 + 50 * n_view
    d = np.tile(-n_view, (len(o), 1))
    best = np.full(len(o), np.inf)
    ident = np.full(len(o), -1)
    normal = np.zeros_like(o)
    for k, prim in enumerate(prims):
        s, n = prim.hit(o, d)
        closer = s < best
        best[closer], ident[closer], normal[closer] = s[closer], k, n[closer]
    color = np.zeros_like(o)
    hit = ident >= 0
    points = o + np.where(hit, best, 0)[:, None] * d
    for k, prim in enumerate(prims):
        sel = ident == k
        if not sel.any():
            continue
        p = points[sel]
        n, bevel, gradient = prim.shade_normal(p, normal[sel])
        albedo, emissive = prim.material(p, normal[sel])
        if emissive:
            color[sel] = albedo
            continue
        diffuse = np.clip(n @ LIGHT, 0, 1)[:, None]
        spec = (np.clip(n @ HALF, 0, 1) ** 28)[:, None] * 0.55
        # Cream rim light on bevels facing the key light, as in the reference art.
        rim = (bevel * np.clip(n @ LIGHT, 0, 1))[:, None] * 0.55
        color[sel] = albedo * gradient[:, None] * (AMBIENT + KEY * diffuse) + SPECULAR * (spec + rim)
    color = np.clip(color, 0, 1).reshape(h, w, 3)
    depth = np.where(hit, best, np.inf).reshape(h, w)
    hit = hit.reshape(h, w)
    ident = ident.reshape(h, w)
    # Supersample down to art pixels.
    cov = hit.reshape(CELL_H, SS, CELL_W, SS).mean(axis=(1, 3))
    summed = (color * hit[:, :, None]).reshape(CELL_H, SS, CELL_W, SS, 3).sum(axis=(1, 3))
    rgb = summed / np.maximum(hit.reshape(CELL_H, SS, CELL_W, SS).sum(axis=(1, 3)), 1)[:, :, None]
    centre = SS // 2
    art_id = ident[centre::SS, centre::SS]
    art_depth = depth[centre::SS, centre::SS]
    solid = cov >= 0.5
    rgb = ps.quantize(rgb * 255)
    # Inner outlines: where a part sits in front of another, ring the nearer part.
    line = np.zeros_like(solid)
    for dy, dx in ((0, 1), (1, 0), (0, -1), (-1, 0)):
        nid = np.roll(art_id, (dy, dx), axis=(0, 1))
        ndepth = np.roll(art_depth, (dy, dx), axis=(0, 1))
        line |= solid & (nid >= 0) & (nid != art_id) & (ndepth + 0.05 < art_depth)
    rgba = np.zeros((CELL_H, CELL_W, 4), dtype=np.uint8)
    rgba[solid, :3] = rgb[solid]
    rgba[solid, 3] = 255
    rgba[line, :3] = ps.outline_color()
    # Outer outline, two art pixels heavy like the reference: ring + darkened rim.
    rim = solid & ~(np.roll(solid, 1, 0) & np.roll(solid, -1, 0) & np.roll(solid, 1, 1) & np.roll(solid, -1, 1))
    rgba[rim, :3] = (rgba[rim, :3] * 0.35 + ps.outline_color() * 0.65).astype(np.uint8)
    return ps.add_outline(rgba, 1)


# ---------- the props (world units; footprint centred on the origin) ----------
DARK_METAL = flat([0.22, 0.22, 0.33])
MID_METAL = flat([0.42, 0.43, 0.53])


def shredder():
    body = shred_window(-0.18, 0.18, 0.14, 0.62, DARK_METAL)
    prims = [
        Box([-0.30, -0.27, 0.0], [0.30, 0.27, 0.80], body, 0.04),
        Box([-0.33, -0.30, 0.80], [0.33, 0.30, 1.00], panel(1, [-0.25, 0.86], [-0.15, 0.93], [0.35, 1.0, 0.45], MID_METAL), 0.05),
        Box([-0.22, -0.016, 1.00], [0.20, 0.016, 1.42], paper(2, 1), 0.0),
        Box([-0.05, 0.016, 1.30], [0.05, 0.019, 1.36], glow([0.80, 0.16, 0.16]), 0.0),
        Box([-0.28, -0.06, 1.00], [0.28, 0.06, 1.012], flat([0.05, 0.05, 0.07], 0), 0.0),
    ]
    return prims


def crate(z_top):
    return Box([-0.40, -0.36, 0.0], [0.40, 0.36, z_top], wood(), 0.03)


def cash_counter():
    beige = flat([0.86, 0.79, 0.64], 0.08)
    machine = panel(0, [-0.12, 0.60], [0.10, 0.70], [0.06, 0.16, 0.09], beige, emissive_color=[0.45, 1.0, 0.55])
    return [
        crate(0.50),
        Box([-0.24, -0.20, 0.50], [0.16, 0.18, 0.78], machine, 0.05),
        Box([-0.24, -0.20, 0.78], [-0.02, 0.18, 0.92], flat([0.26, 0.27, 0.34]), 0.04),
        Box([-0.20, -0.15, 0.92], [-0.08, 0.13, 0.99], cash(1, -0.01), 0.02),
        Box([0.21, -0.32, 0.50], [0.38, -0.06, 0.62], cash(1, -0.19), 0.02),
        Box([0.21, -0.32, 0.62], [0.38, -0.06, 0.72], cash(1, -0.19), 0.02),
        Box([0.21, 0.06, 0.50], [0.38, 0.32, 0.60], cash(1, 0.19), 0.02),
    ]


def scanner():
    radio_body = panel(1, [0.02, 0.55], [0.19, 0.69], [0.12, 0.12, 0.16], flat([0.20, 0.20, 0.27]), dots=True)
    radio = panel(1, [-0.22, 0.58], [-0.06, 0.66], [1.0, 0.68, 0.25], radio_body)
    return [
        crate(0.50),
        Box([-0.26, -0.11, 0.50], [0.22, 0.11, 0.72], radio, 0.04),
        Box([-0.04, 0.111, 0.61], [-0.01, 0.115, 0.64], glow([0.95, 0.22, 0.18]), 0.0),
        Cylinder(0.14, -0.05, 0.72, 1.40, 0.022, flat([0.10, 0.10, 0.13], 0)),
        Box([0.12, -0.07, 1.40], [0.16, -0.03, 1.45], glow([0.95, 0.22, 0.18]), 0.0),
        Box([-0.36, 0.14, 0.50], [-0.08, 0.33, 0.512], paper(0, 2), 0.0),
        Cylinder(0.28, 0.22, 0.50, 0.63, 0.065, flat([0.92, 0.90, 0.86], 0.05)),
        Cylinder(0.28, 0.22, 0.50, 0.631, 0.048, flat([0.28, 0.16, 0.09], 0.05)),
    ]


def cash_pallet():
    slat = wood(plank=10, along=0)
    prims = [Box([-0.44, y - 0.06, 0.0], [0.44, y + 0.06, 0.07], slat, 0.02) for y in (-0.32, 0.0, 0.32)]
    prims.append(Box([-0.44, -0.40, 0.07], [0.44, 0.40, 0.12], slat, 0.02))
    z = 0.12
    for layer, (nx, ny) in enumerate(((3, 3), (2, 2), (1, 1))):
        w, d = 0.26, 0.22
        x0, y0 = -nx * w / 2, -ny * d / 2
        for i in range(nx):
            for j in range(ny):
                lo = [x0 + i * w + 0.01, y0 + j * d + 0.01, z]
                hi = [x0 + (i + 1) * w - 0.01, y0 + (j + 1) * d - 0.01, z + 0.11]
                prims.append(Box(lo, hi, cash(0, (lo[0] + hi[0]) / 2), 0.02))
        z += 0.11
    return prims


PROPS = [("shredder", shredder), ("cashcounter", cash_counter), ("scanner", scanner), ("cashpallet", cash_pallet)]
# Reference furniture is drawn at about 265 source pixels per tile (width * 0.78).
SOURCE_PER_TILE = 265.0


def main(out):
    cells = []
    for name, build in PROPS:
        cells.append(ps.upscale(render(build())))
    atlas = np.concatenate(cells, axis=1)
    Image.fromarray(atlas, "RGBA").save(out)
    cw = CELL_W * ps.ART_PIXEL
    for i, (name, _) in enumerate(PROPS):
        alpha = atlas[:, i * cw:(i + 1) * cw, 3] > 0
        ys, xs = np.nonzero(alpha)
        x, y, w, h = i * cw + xs.min(), ys.min(), xs.max() - xs.min() + 1, ys.max() - ys.min() + 1
        print(f"{name}: new Rect({x},{y},{w},{h}) width {w / (SOURCE_PER_TILE * 0.78):.2f}")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(ps.ART, "denprops.png"))
