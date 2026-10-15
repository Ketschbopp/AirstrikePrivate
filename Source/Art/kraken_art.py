"""Draws the Kraken (J-20-style canard-delta stealth fighter) sprite set in the
Falcon's style: white surfaces (tinted in-game by CutoutComplexPattern) with soft
gradient shading, heavy black part outlines, light-grey panel lines, a tinted glossy
canopy and detailed engine nozzles. Writes base + mask textures (red = paintable,
black = keep original colour) for every direction the Falcon ships.

    python Source/Art/kraken_art.py Textures/Things/Vehicles/Kraken
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

OUT = Path(sys.argv[1])
S = 3                     # supersample factor
W = 1280                  # output size
N = W * S
CX = W / 2
OUTLINE = 7               # part outline half-width (output px)
PANEL = (168, 168, 168)
PANEL_W = 2.6


# ----------------------------------------------------------------- geometry utils

def mirror(pts):
    return [(W - x, y) + tuple(p[2:]) for p, (x, y) in zip(pts, [(p[0], p[1]) for p in pts])]


def _segment(p0, p1, p2, p3, steps):
    out = []
    for t in np.linspace(0, 1, steps, endpoint=False):
        t2, t3 = t * t, t * t * t
        out.append(tuple(
            0.5 * ((2 * p1[k]) + (-p0[k] + p2[k]) * t
                   + (2 * p0[k] - 5 * p1[k] + 4 * p2[k] - p3[k]) * t2
                   + (-p0[k] + 3 * p1[k] - 3 * p2[k] + p3[k]) * t3) for k in (0, 1)))
    return out


def catmull(pts, closed=True, steps=10):
    """Smooth Catmull-Rom path through points. A point given as (x, y, 'c') is a sharp
    corner; open paths always start and end sharp."""
    pts = [tuple(p) for p in pts]
    if not closed:
        pts[0] = pts[0][:2] + ('c',)
        pts[-1] = pts[-1][:2] + ('c',)
    n = len(pts)
    corners = [i for i, p in enumerate(pts) if len(p) > 2]
    xy = [p[:2] for p in pts]
    out = []
    if closed and not corners:
        for i in range(n):
            out += _segment(xy[i - 1], xy[i], xy[(i + 1) % n], xy[(i + 2) % n], steps)
        return out
    # runs between consecutive corners
    pairs = list(zip(corners, corners[1:]))
    if closed:
        pairs.append((corners[-1], corners[0] + n))
    for a, b in pairs:
        run = [xy[i % n] for i in range(a, b + 1)]
        ext = [run[0]] + run + [run[-1]]
        for i in range(1, len(ext) - 2):
            out += _segment(ext[i - 1], ext[i], ext[i + 1], ext[i + 2], steps)
    if not closed:
        out.append(xy[-1])
    return out


def sym(right):
    """Right half outline from nose to tail (x >= CX) -> closed symmetric polygon."""
    left = [(W - p[0], p[1]) + tuple(p[2:]) for p in reversed(right)]
    return right + left[1:-1] if right[0][0] == CX and right[-1][0] == CX else right + left


def scaled(path):
    return [(x * S, y * S) for x, y in path]


# ------------------------------------------------------------------- canvas

class Canvas:
    def __init__(self):
        self.rgb = np.zeros((N, N, 3), np.float32)
        self.a = np.zeros((N, N), np.float32)
        self.mask = Image.new("RGB", (N, N), (255, 0, 0))
        self.md = ImageDraw.Draw(self.mask)

    def _blend(self, cov, color_fn, box):
        x0, y0, x1, y1 = box
        if x1 <= x0 or y1 <= y0:
            return
        c = cov[y0:y1, x0:x1]
        ys, xs = np.mgrid[y0:y1, x0:x1].astype(np.float32) / S
        col = color_fn(xs, ys)
        if col.ndim == 2:
            col = np.repeat(col[..., None], 3, axis=2)
        r = self.rgb[y0:y1, x0:x1]
        a = self.a[y0:y1, x0:x1]
        r[:] = r * (1 - c[..., None]) + col * c[..., None]
        a[:] = np.maximum(a, c)

    def fill(self, path, color_fn, outline=OUTLINE, outline_color=(0, 0, 0)):
        """Fill a closed path with a shaded colour and a black outline."""
        pts = scaled(path)
        if outline > 0:
            ol = Image.new("L", (N, N), 0)
            ImageDraw.Draw(ol).line(pts + [pts[0], pts[1]], fill=255, width=int(outline * 2 * S),
                                    joint="curve")
            cov = np.asarray(ol, np.float32) / 255
            box = ol.getbbox()
            if box:
                self._blend(cov, lambda x, y: np.broadcast_to(
                    np.array(outline_color, np.float32), x.shape + (3,)).copy(), box)
        m = Image.new("L", (N, N), 0)
        ImageDraw.Draw(m).polygon(pts, fill=255)
        box = m.getbbox()
        if box:
            self._blend(np.asarray(m, np.float32) / 255, color_fn, box)
        return m

    def line(self, path, color=PANEL, width=PANEL_W, closed=False):
        pts = scaled(path)
        if closed:
            pts = pts + [pts[0]]
        im = Image.new("L", (N, N), 0)
        ImageDraw.Draw(im).line(pts, fill=255, width=max(1, int(width * S)), joint="curve")
        box = im.getbbox()
        if box:
            col = np.array(color, np.float32)
            self._blend(np.asarray(im, np.float32) / 255,
                        lambda x, y: np.broadcast_to(col, x.shape + (3,)).copy(), box)

    def ellipse(self, box, color_fn, outline=0):
        x0, y0, x1, y1 = box
        pts = [(x0 + (x1 - x0) / 2 * (1 + math.cos(t)), y0 + (y1 - y0) / 2 * (1 + math.sin(t)))
               for t in np.linspace(0, 2 * math.pi, 64, endpoint=False)]
        return self.fill(pts, color_fn, outline=outline)

    def unmask(self, path):
        """Mark a region as not paintable (keeps its drawn colour in-game)."""
        self.md.polygon(scaled(path), fill=(0, 0, 0))

    def image(self):
        rgb = np.clip(self.rgb, 0, 255).astype(np.uint8)
        a = np.clip(self.a * 255, 0, 255).astype(np.uint8)
        img = Image.fromarray(np.dstack([rgb, a]), "RGBA").resize((W, W), Image.LANCZOS)
        return img, self.mask.resize((W, W), Image.NEAREST)


# ---------------------------------------------------------------- shading fns

def const(v):
    return lambda x, y: np.full(x.shape + (3,), v, np.float32) if np.isscalar(v) else \
        np.broadcast_to(np.array(v, np.float32), x.shape + (3,)).copy()


def body_shade(half_width, top=255, edge=212, hl_offset=-0.28):
    """Cylindrical fuselage shading: bright stripe left of centre, darker edges."""
    def f(x, y):
        u = (x - CX) / half_width
        v = 1 - np.clip((u - hl_offset) ** 2 / 1.6, 0, 1)
        g = edge + (top - edge) * v
        g -= np.clip((y - 1000) / 600, 0, 1) * 10        # slightly darker toward the tail
        return g
    return f


def linear(p0, p1, v0, v1):
    """Linear gradient from p0 (value v0) to p1 (value v1)."""
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    ll = dx * dx + dy * dy

    def f(x, y):
        t = np.clip(((x - p0[0]) * dx + (y - p0[1]) * dy) / ll, 0, 1)
        return v0 + (v1 - v0) * t
    return f


def mirror_fn(fn):
    return lambda x, y: fn(W - x, y)


def radial(cx, cy, r, v0, v1, tint=(1, 1, 1)):
    t = np.array(tint, np.float32)

    def f(x, y):
        d = np.clip(np.hypot(x - cx, y - cy) / r, 0, 1)
        g = v0 + (v1 - v0) * d
        return g[..., None] * t
    return f


# ----------------------------------------------------------------- the jet

def m(pts):
    return [(W - p[0], p[1]) + tuple(p[2:]) for p in pts]


FUSELAGE_R = [
    (CX, 24, 'c'), (652, 62), (668, 118), (684, 180), (697, 245), (707, 305), (713, 360),
    (716, 390, 'c'),                       # chine kink
    (770, 410, 'c'), (791, 440, 'c'),      # caret intake lip
    (795, 520), (797, 620), (795, 740), (790, 860), (784, 960), (776, 1040),
    (766, 1090), (752, 1120),
    (732, 1138, 'c'), (676, 1148, 'c'),    # aft deck between the nozzles
    (657, 1198), (CX, 1228, 'c'),          # tail stinger
]
WING_R = [(788, 556, 'c'), (1050, 966, 'c'), (1050, 1012, 'c'), (1004, 1018, 'c'), (774, 1044, 'c')]
CANARD_R = [(792, 452, 'c'), (964, 578, 'c'), (968, 616, 'c'), (930, 620, 'c'), (796, 566, 'c')]
LERX_R = [(788, 552, 'c'), (842, 640), (806, 740, 'c')]
VTAIL_R = [(742, 904, 'c'), (842, 1010, 'c'), (848, 1100, 'c'), (818, 1108, 'c'), (746, 1092, 'c')]
VFIN_R = [(764, 1046, 'c'), (812, 1108, 'c'), (804, 1150, 'c'), (774, 1142, 'c')]
INTAKE_R = [(714, 396, 'c'), (768, 413, 'c'), (787, 439, 'c'), (774, 437, 'c'), (726, 416, 'c')]
CANOPY = [(CX, 196, 'c'), (659, 228), (672, 280), (675, 335), (668, 390), (654, 424), (CX, 438, 'c'),
          (626, 424), (612, 390), (605, 335), (608, 280), (621, 228)]
NOZZLES = [(588, 1150), (692, 1150)]
NOZ_R = 33


def draw():
    c = Canvas()
    fus_half = 160.0

    # Back to front.
    for side in (False, True):
        g = linear((790, 1046), (812, 1150), 200, 170)
        c.fill(catmull(m(VFIN_R) if side else VFIN_R), mirror_fn(g) if side else g)

    for side in (False, True):
        g = linear((800, 580), (1050, 1010), 252, 214)
        c.fill(catmull(m(WING_R) if side else WING_R), mirror_fn(g) if side else g)
    for side in (False, True):
        mm = (lambda p: m(p)) if side else (lambda p: p)
        # leading-edge flap, flaperon, aileron
        c.line(mm([(804, 596), (1030, 958)]))
        c.line(mm([(812, 1006), (1030, 994)]))
        c.line(mm([(918, 1001), (921, 1022)]))
        c.line(mm([(1034, 964), (1034, 1012)]))
        # wing panels
        c.line(mm([(860, 780), (980, 960)]), color=(190, 190, 190), width=1.8)

    for side in (False, True):
        g = linear((800, 470), (966, 610), 252, 218)
        c.fill(catmull(m(CANARD_R) if side else CANARD_R), mirror_fn(g) if side else g)
        mm = (lambda p: m(p)) if side else (lambda p: p)
        c.line(mm([(808, 490), (950, 592)]))
        c.ellipse((W - 812, 504, W - 794, 522) if side else (794, 504, 812, 522), const(205), outline=1.6)


    c.fill(catmull(sym(FUSELAGE_R)), body_shade(fus_half))

    # Radome: slightly darker cap with a seam.
    radome = [(CX, 24, 'c'), (652, 62), (668, 118), (681, 166, 'c'), (CX, 158, 'c'),
              (599, 166, 'c'), (612, 118), (628, 62)]
    c.fill(catmull(radome), linear((CX, 24), (CX, 168), 205, 232), outline=0)
    c.line(catmull([(600, 168), (CX, 158), (680, 168)], closed=False), color=(110, 110, 110), width=3)

    # Dorsal spine with side lines and centreline.
    for side in (False, True):
        mm = (lambda p: m(p)) if side else (lambda p: p)
        c.line(catmull(mm([(660, 446), (668, 620), (670, 880), (664, 1110)]), closed=False))
        # intake duct / chine lines
        c.line(catmull(mm([(716, 396), (738, 520), (758, 760), (760, 980), (748, 1100)]), closed=False))
        # DSI bump ahead of the intake
        bx = 744 if not side else W - 744
        c.ellipse((bx - 16, 432, bx + 16, 492), radial(bx - 5, 452, 30, 255, 210), outline=0)
        c.line(catmull(mm([(728, 470), (736, 438), (752, 434), (760, 470)]), closed=False),
               color=(150, 150, 150), width=2)
        # intake opening (dark, unpainted)
        intake = catmull(mm(INTAKE_R))
        c.fill(intake, linear((720, 400), (780, 436), 25, 70), outline=2)
        c.unmask(intake)
        # stealth sawtooth panel edges
        for y0 in (585, 1046):
            zz = [(676 + 18 * k, y0 + (10 if k % 2 else 0)) for k in range(7)]
            c.line(mm(zz), width=2.2)
        # access panels
        for (x0, y0, x1, y1) in ((694, 640, 722, 690), (694, 712, 722, 762), (722, 820, 750, 900),
                                 (692, 930, 722, 990)):
            rect = [(x0, y0, 'c'), (x1, y0, 'c'), (x1, y1, 'c'), (x0, y1, 'c')]
            c.line(mm([p[:2] for p in rect]), closed=True, width=2)
        # small vents
        for k in range(4):
            c.line(mm([(744, 600 + 14 * k), (760, 600 + 14 * k)]), color=(140, 140, 140), width=2)
    c.line([(CX, 448), (CX, 1120)], color=(185, 185, 185), width=2)

    # Canted vertical tails (on top in plan view), outer side shaded darker.
    for side in (False, True):
        g = linear((744, 1000), (846, 1000), 246, 200)
        c.fill(catmull(m(VTAIL_R) if side else VTAIL_R), mirror_fn(g) if side else g)
        mm = (lambda p: m(p)) if side else (lambda p: p)
        c.line(mm([(760, 930), (834, 1030)]))
        c.line(mm([(752, 1060), (838, 1086)]), color=(185, 185, 185), width=1.8)

    # Nozzles seen from above: tapered tubes with ribbed petals and a dark exit lip
    # (unpainted), poking out behind the aft deck.
    for (nx, _) in NOZZLES:
        top, bot = 1108, 1182
        tube = [(nx - 33, top, 'c'), (nx + 33, top, 'c'), (nx + 28, bot, 'c'), (nx - 28, bot, 'c')]
        shade = lambda x, y, nx=nx: (lambda u: 150 - 85 * np.clip(u * u, 0, 1))((x - (nx - 6)) / 34)
        path = catmull(tube)
        c.fill(path, shade, outline=5)
        c.unmask(path)
        # engine/nozzle seam and ribbed petals on the convergent section
        c.line([(nx - 32, 1128), (nx + 32, 1128)], color=(35, 35, 35), width=2.5)
        for k in range(-4, 5):
            fx = k / 4.6
            c.line([(nx + fx * 31, 1131), (nx + fx * 27, bot - 6)], color=(48, 48, 48), width=1.6)
        # exit lip, foreshortened (dark interior visible at the very end)
        lip = [(nx + 27 * math.cos(t), bot - 2 + 5 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 40)]
        c.fill(lip, const(18), outline=1.5)
        c.unmask(lip)

    # Canopy: amber-tinted glass with frame and gloss (unpainted).
    canopy = catmull(CANOPY)
    c.fill(canopy, radial(632, 270, 190, 120, 22, tint=(0.95, 0.72, 0.30)), outline=5)
    c.unmask(canopy)
    c.line([(612, 352), (668, 352)], color=(70, 60, 40), width=3)
    gloss = [(626 + 9 * math.cos(t), 280 + 38 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 40)]
    c.fill(gloss, const((255, 240, 205)), outline=0)
    gloss2 = [(652 + 4 * math.cos(t), 236 + 12 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 30)]
    c.fill(gloss2, const((255, 245, 220)), outline=0)

    # Formation / nav lights (unpainted).
    for (lx, ly, col) in ((1040, 990, (60, 220, 90)), (W - 1040, 990, (230, 50, 40)),
                          (840, 1098, (240, 240, 240)), (W - 840, 1098, (240, 240, 240))):
        dot = [(lx + 5 * math.cos(t), ly + 5 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 16)]
        c.fill(dot, const(col), outline=1.2)
        c.unmask(dot)

    return c.image()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    north, north_m = draw()
    views = {
        "north": (north, north_m),
        "south": (north.transpose(Image.FLIP_TOP_BOTTOM), north_m.transpose(Image.FLIP_TOP_BOTTOM)),
        "east": (north.rotate(-90), north_m.rotate(-90)),
    }
    # Falcon convention: every diagonal is the east-facing view (VF flips/rotates).
    for diag in ("northEast", "southEast", "northWest", "southWest"):
        views[diag] = views["east"]
    for name, (img, mk) in views.items():
        img.save(OUT / f"Kraken_{name}.png", optimize=True)
        mk.save(OUT / f"Kraken_{name}m.png", optimize=True)
    print("wrote", len(views) * 2, "textures to", OUT)


if __name__ == "__main__":
    main()
