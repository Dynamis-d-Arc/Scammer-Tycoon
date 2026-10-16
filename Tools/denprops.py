"""Generate the scam-den prop atlas (denprops.png) as chunky isometric pixel art.

Each prop is drawn at 1/4 resolution on an 80x96 cell and upscaled 4x with
nearest-neighbour sampling, matching the Pixel Office outline/amber/navy look.
Prints tight sprite rectangles (top-left origin) for FurnitureCatalog.
"""
import sys
from PIL import Image, ImageDraw

CW, CH, S = 80, 96, 4
OUT = (22, 18, 32, 255)
HI = (255, 214, 150, 255)

def pt(F, u, t, v):
    return (F[0] + 2 * u - 2 * t, F[1] - u - t - v)

def poly(d, pts, fill):
    d.polygon([(round(x), round(y)) for x, y in pts], fill=fill)

def line(d, pts, fill):
    d.line([(round(x), round(y)) for x, y in pts], fill=fill)

def box(d, F, a, b, h, top, left, right, edge=OUT, rim=None):
    p = lambda u, t, v: pt(F, u, t, v)
    poly(d, [p(0, 0, 0), p(a, 0, 0), p(a, 0, h), p(0, 0, h)], right)
    poly(d, [p(0, 0, 0), p(0, b, 0), p(0, b, h), p(0, 0, h)], left)
    poly(d, [p(0, 0, h), p(a, 0, h), p(a, b, h), p(0, b, h)], top)
    # Silhouette, then a warm rim on the near top edges.
    line(d, [p(0, 0, 0), p(a, 0, 0), p(a, 0, h), p(a, b, h), p(0, b, h), p(0, b, 0), p(0, 0, 0)], edge)
    line(d, [p(0, 0, 0), p(0, 0, h)], edge)
    if rim:
        line(d, [p(0, b, h), p(0, 0, h), p(a, 0, h)], rim)
    return p

def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + (255,)

def shredder(d):
    F = (40, 88)
    bin_top, bin_l, bin_r = (96, 100, 118, 255), (70, 72, 90, 255), (46, 48, 64, 255)
    p = box(d, F, 8, 7, 22, bin_top, bin_l, bin_r)
    # Clear window full of shreds on the front-right face.
    poly(d, [p(1.5, 0, 3), p(6.5, 0, 3), p(6.5, 0, 15), p(1.5, 0, 15)], (150, 170, 190, 255))
    for i, u in enumerate([2, 3, 4, 5, 6]):
        for v in range(4, 15, 3):
            x, y = p(u, 0, v + (i % 2))
            d.point((round(x), round(y)), fill=(244, 240, 230, 255))
            d.point((round(x) + 1, round(y) - 1), fill=(214, 210, 200, 255))
    # Shredder head with slot, LED and a sheet being eaten.
    head = pt(F, 0, 0, 22)
    q = box(d, (head[0], head[1] + 1), 8, 7, 5, (132, 136, 152, 255), (98, 100, 118, 255), (64, 66, 84, 255), rim=HI)
    line(d, [q(1, 3.5, 5), q(7, 3.5, 5)], OUT)
    d.point((round(q(0, 2, 3)[0]), round(q(0, 2, 3)[1])), fill=(90, 240, 130, 255))
    d.point((round(q(0, 4, 3)[0]), round(q(0, 4, 3)[1])), fill=(240, 80, 60, 255))
    sheet = [q(1.5, 3.5, 5), q(6.5, 3.5, 5), q(6.5, 3.5, 17), q(1.5, 3.5, 17)]
    poly(d, sheet, (242, 238, 226, 255))
    line(d, sheet + [sheet[0]], OUT)
    for v in (8, 10, 12):
        line(d, [q(2.5, 3.5, 5 + v), q(5.5, 3.5, 5 + v)], (150, 150, 160, 255))
    # Red "classified" stamp.
    for u in (2.5, 3.5):
        x, y = q(u, 3.5, 20)
        d.point((round(x), round(y)), fill=(214, 52, 52, 255))
    # Spilled shreds on the floor.
    for x, y in [(22, 86), (24, 87), (56, 86), (58, 85), (60, 87), (27, 89)]:
        d.point((x, y), fill=(240, 236, 226, 255))
        d.point((x + 1, y), fill=(200, 196, 186, 255))

def crate(d, F, a, b, h):
    wood = (182, 120, 64, 255), (146, 92, 48, 255), (110, 66, 36, 255)
    p = box(d, F, a, b, h, *wood, rim=HI)
    for v in range(3, h, 4):
        line(d, [p(0, 0, v), p(a, 0, v)], (84, 48, 28, 255))
        line(d, [p(0, 0, v), p(0, b, v)], (104, 62, 34, 255))
    return p

def bills(d, F, a, b, h):
    p = box(d, F, a, b, h, (132, 194, 108, 255), (98, 158, 82, 255), (70, 122, 62, 255))
    line(d, [p(a / 2, 0, 0), p(a / 2, 0, h)], (240, 230, 170, 255))
    line(d, [p(0, b / 2, 0), p(0, b / 2, h)], (240, 230, 170, 255))
    line(d, [p(a / 2, 0, h), p(a / 2, b, h)], (240, 230, 170, 255))
    return p

def cash_counter(d):
    F = (40, 90)
    c = crate(d, F, 9, 8, 13)
    top = c(0, 0, 13)
    m = box(d, pt(top, 1, 1.5, 0), 6, 5, 7, (230, 218, 188, 255), (198, 184, 152, 255), (158, 144, 116, 255), rim=(255, 248, 225, 255))
    # LCD with green digits.
    poly(d, [m(1, 0, 2), m(5, 0, 2), m(5, 0, 5), m(1, 0, 5)], (24, 40, 30, 255))
    for u in (1.8, 2.8, 3.8):
        x, y = m(u, 0, 3.5)
        d.point((round(x), round(y)), fill=(110, 255, 140, 255))
    # Hopper of fanned bills at the back.
    h = box(d, m(1, 3, 7), 4, 2, 3, (70, 72, 88, 255), (52, 54, 70, 255), (38, 40, 54, 255))
    bills(d, h(0.5, 0.5, 3), 3, 1, 2)
    # Bundles beside the machine.
    bills(d, pt(top, 7, 1, 0), 2, 2, 3)
    bills(d, pt(top, 7, 1, 3), 2, 2, 2)
    bills(d, pt(top, 1, 6.2, 0), 2, 1.5, 2)

def scanner(d):
    F = (40, 90)
    c = crate(d, F, 8, 8, 12)
    top = c(0, 0, 12)
    # Notepad with scribbles.
    pad = [pt(top, 0.5, 4.8, 0), pt(top, 3.5, 4.8, 0), pt(top, 3.5, 7.5, 0), pt(top, 0.5, 7.5, 0)]
    poly(d, pad, (238, 230, 200, 255))
    line(d, pad + [pad[0]], OUT)
    line(d, [pt(top, 1, 5.8, 0), pt(top, 3, 5.8, 0)], (60, 70, 140, 255))
    line(d, [pt(top, 1, 6.8, 0), pt(top, 2.5, 6.8, 0)], (200, 50, 50, 255))
    r = box(d, pt(top, 1, 1, 0), 6, 3, 6, (64, 66, 80, 255), (44, 46, 58, 255), (30, 32, 42, 255), rim=(120, 124, 140, 255))
    # Amber frequency display and speaker grille.
    poly(d, [r(0.6, 0, 3), r(3, 0, 3), r(3, 0, 5), r(0.6, 0, 5)], (255, 176, 70, 255))
    for u in (3.8, 4.6, 5.4):
        for v in (1.5, 3, 4.5):
            x, y = r(u, 0, v)
            d.point((round(x), round(y)), fill=(14, 14, 20, 255))
    for t, col in ((1, (240, 70, 60, 255)), (2, (70, 120, 255, 255))):
        x, y = r(0, t, 4)
        d.point((round(x), round(y)), fill=col)
    # Antenna with a red tip.
    base = r(5, 2, 6)
    tip = (base[0] + 3, base[1] - 16)
    line(d, [base, tip], OUT)
    d.point((round(tip[0]), round(tip[1]) - 1), fill=(240, 70, 60, 255))
    # Mug.
    box(d, pt(top, 6.2, 0.4, 0), 1.5, 1.5, 3, (236, 232, 222, 255), (210, 204, 192, 255), (170, 164, 152, 255))

def cash_pallet(d):
    F = (40, 90)
    slat = (170, 128, 78, 255), (136, 98, 58, 255), (102, 70, 40, 255)
    # Pallet: three runners under a slatted deck.
    for t in (0, 3.5, 7):
        box(d, pt(F, 0, t, 0), 11, 1.2, 2, *slat)
    deck = pt(F, 0, 0, 2)
    p = box(d, deck, 11, 8.2, 1.4, *slat, rim=HI)
    for u in (2.5, 5.5, 8.5):
        line(d, [p(u, 0, 1.4), p(u, 8.2, 1.4)], (96, 64, 36, 255))
    top = p(0, 0, 1.4)
    # Bricks of cash, stacked back to front so nearer bricks overlap.
    for layer, (rows, cols) in enumerate(((3, 3), (2, 2), (1, 1))):
        for t in reversed(range(rows)):
            for u in reversed(range(cols)):
                bills(d, pt(top, 0.3 + u * 3.6 + layer * 1.8, 0.3 + t * 2.6 + layer * 1.3, layer * 3.5), 3.4, 2.4, 3.5)
    # Paper label.
    lbl = pt(top, 0.6, 0.4, 2.2)
    d.point((round(lbl[0]) + 2, round(lbl[1])), fill=(240, 230, 170, 255))

def main(out):
    props = [shredder, cash_counter, scanner, cash_pallet]
    atlas = Image.new("RGBA", (CW * S * len(props), CH * S), (0, 0, 0, 0))
    rects = []
    for i, draw in enumerate(props):
        cell = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
        draw(ImageDraw.Draw(cell))
        big = cell.resize((CW * S, CH * S), Image.NEAREST)
        atlas.paste(big, (i * CW * S, 0))
        x0, y0, x1, y1 = big.getbbox()
        rects.append((draw.__name__, i * CW * S + x0, y0, x1 - x0, y1 - y0))
    atlas.save(out)
    for name, x, y, w, h in rects:
        print(f"{name}: new Rect({x},{y},{w},{h})")

if __name__ == "__main__":
    main(sys.argv[1])
