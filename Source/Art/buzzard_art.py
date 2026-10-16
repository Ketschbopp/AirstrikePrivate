"""Design concept for an early-game aircraft: the Buzzard, a single-engine propeller
attack plane / dive bomber (Il-2 / Stuka era) in the Falcon/Kraken sprite style.
Uses the Kraken renderer: white surfaces (tinted in-game by CutoutComplexPattern),
soft gradient shading, heavy black part outlines, light-grey panel lines and
unpainted (masked-out) metal, glass and propeller parts.

    python Source/Art/buzzard_art.py Textures/Things/Vehicles/Buzzard
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
OUT = Path(sys.argv[1] if len(sys.argv) > 1 else "Textures/Things/Vehicles/Buzzard")
sys.argv = [sys.argv[0], str(OUT)]  # kraken_art reads its output dir on import
from kraken_art import (CX, W, Canvas, body_shade, catmull, const, linear, m,  # noqa: E402
                        mirror_fn, radial, scaled, sym)

# Plan view, nose up, 1280 x 1280.
FUSELAGE_R = [
    (CX, 262, 'c'), (700, 266), (726, 290), (734, 340), (738, 420), (736, 520), (730, 640),
    (718, 760), (700, 880), (680, 990), (662, 1090), (652, 1150), (CX, 1166, 'c'),
]
COWL_R = [(CX, 150, 'c'), (690, 152), (726, 168), (742, 200), (744, 250), (736, 290, 'c'), (CX, 296, 'c')]
WING_R = [(736, 452, 'c'), (1000, 486), (1130, 506), (1168, 524), (1180, 552), (1166, 584),
          (1120, 598), (980, 610), (736, 650, 'c')]
HSTAB_R = [(664, 1002, 'c'), (800, 1024), (880, 1042), (900, 1064), (890, 1094), (840, 1106),
           (664, 1116, 'c')]
FIN = [(CX, 930, 'c'), (650, 980), (652, 1100), (646, 1180, 'c'), (CX, 1196, 'c'), (634, 1180, 'c'),
       (628, 1100), (630, 980)]
CANOPY = [(CX, 360, 'c'), (668, 368), (678, 400), (680, 470), (678, 560), (670, 600), (CX, 612, 'c'),
          (610, 600), (602, 560), (600, 470), (602, 400), (612, 368)]

STEEL = (120, 122, 124)


def P(pts):
    """Plain polygon (drop the corner markers)."""
    return [p[:2] for p in pts]


def draw():
    c = Canvas()

    # Tailplane and wings (behind the fuselage).
    for side in (False, True):
        mm = (lambda p: m(p)) if side else (lambda p: p)
        g = linear((664, 1020), (900, 1100), 250, 214)
        c.fill(catmull(mm(HSTAB_R)), mirror_fn(g) if side else g)
        c.line(mm([(668, 1080), (884, 1078)]))                     # elevator hinge
        c.line(mm([(780, 1082), (780, 1110)]), width=2)            # trim tab

        # Bomb under each wing: fins and nose peek out in front of/behind the wing.
        bx = 900 if not side else W - 900
        bomb = [(bx, 410, 'c'), (bx + 14, 428), (bx + 18, 470), (bx + 18, 640), (bx + 10, 670, 'c'),
                (bx - 10, 670, 'c'), (bx - 18, 640), (bx - 18, 470), (bx - 14, 428)]
        bomb_path = catmull(bomb)
        c.fill(bomb_path, lambda x, y, bx=bx: 95 - 40 * np.clip(((x - bx + 5) / 18) ** 2, 0, 1), outline=4)
        c.unmask(bomb_path)
        for fx in (-1, 1):
            fin = [(bx + fx * 8, 650, 'c'), (bx + fx * 30, 676, 'c'), (bx + fx * 30, 690, 'c'),
                   (bx + fx * 6, 684, 'c')]
            c.fill(P(fin), const(STEEL), outline=3)
            c.unmask(P(fin))
        c.line([(bx - 17, 444), (bx + 17, 444)], color=(200, 160, 40), width=5)  # yellow nose band

        g = linear((740, 470), (1170, 590), 252, 216)
        c.fill(catmull(mm(WING_R)), mirror_fn(g) if side else g)
        c.md.polygon(scaled(catmull(mm(WING_R))), fill=(255, 0, 0))  # wing over the bomb stays paintable
        # Aileron, flap and panel lines, main spar rivet line.
        c.line(mm([(960, 604), (1150, 590)]))
        c.line(mm([(960, 572), (960, 608)]), width=2)
        c.line(mm([(1040, 562), (1040, 600)]), width=2)
        c.line(mm([(742, 612), (958, 586)]))
        c.line(mm([(742, 612), (742, 646)]), width=2)
        c.line(mm([(850, 470), (850, 630)]), color=(190, 190, 190), width=1.8)
        c.line(mm([(760, 520), (1120, 534)]), color=(205, 205, 205), width=1.6)
        # Dark walkway on the wing root (unpainted).
        walk = [(744, 470, 'c'), (800, 476, 'c'), (800, 560, 'c'), (744, 566, 'c')]
        c.fill(P(mm(walk)), const((70, 72, 70)), outline=0)
        c.unmask(P(mm(walk)))
        # Wing cannon barrels poking out of the leading edge.
        gx = 1010 if not side else W - 1010
        barrel = [(gx - 5, 440, 'c'), (gx + 5, 440, 'c'), (gx + 6, 494, 'c'), (gx - 6, 494, 'c')]
        c.fill(P(barrel), const((50, 50, 52)), outline=3)
        c.unmask(P(barrel))
        # Wingtip nav light.
        lx, col = (1170, (60, 220, 90)) if not side else (W - 1170, (230, 50, 40))
        dot = [(lx + 6 * math.cos(t), 552 + 6 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 16)]
        c.fill(dot, const(col), outline=1.2)
        c.unmask(dot)

    # Fuselage.
    c.fill(catmull(sym(FUSELAGE_R)), body_shade(110.0))
    for side in (False, True):
        mm = (lambda p: m(p)) if side else (lambda p: p)
        c.line(catmull(mm([(700, 640), (690, 820), (672, 1000), (658, 1140)]), closed=False))
        for y in (680, 820, 960):
            w = 74 - (y - 680) * 0.05
            c.line([(CX - w, y), (CX + w, y)], color=(190, 190, 190), width=1.8)
        # Exhaust stubs along the cowling sides, with soot streaks.
        for k in range(3):
            ey = 214 + 26 * k
            ex = 744 if not side else W - 744
            d = 1 if not side else -1
            stub = [(ex - d * 4, ey, 'c'), (ex + d * 14, ey + 4, 'c'), (ex + d * 14, ey + 16, 'c'), (ex - d * 4, ey + 18, 'c')]
            c.fill(P(stub), const((45, 42, 40)), outline=2.5)
            c.unmask(P(stub))
        c.line(mm([(738, 320), (734, 420)]), color=(150, 150, 150), width=4)
    c.line([(CX, 620), (CX, 1150)], color=(190, 190, 190), width=2)

    # Fin (seen edge-on from above) with rudder hinge.
    c.fill(catmull(FIN), linear((626, 1000), (654, 1000), 245, 200), outline=5)
    c.line([(CX - 10, 1120), (CX + 10, 1120)], width=2)

    # Radial-engine cowling: polished metal ring (unpainted) around a painted front.
    cowl = catmull(sym(COWL_R))
    c.fill(cowl, body_shade(104.0, top=250, edge=200))
    for y in (194, 236):
        c.line(catmull([(548, y + 4), (CX, y), (732, y + 4)], closed=False), color=(150, 150, 150), width=2.4)
    for k in range(-4, 5):
        x = CX + k * 20
        c.line([(x, 278), (x, 294)], color=(130, 130, 130), width=2)    # cooling gills

    # Propeller: edge-on blur disc, three blades with yellow tips, spinner.
    disc = [(CX + 290 * math.cos(t), 138 + 14 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 64)]
    c.fill(disc, const((150, 150, 150)), outline=0)
    c.unmask(disc)
    for d in (-1, 1):
        blade = [(CX + d * 30, 128, 'c'), (CX + d * 160, 124), (CX + d * 262, 128), (CX + d * 278, 138, 'c'),
                 (CX + d * 262, 148), (CX + d * 160, 152), (CX + d * 30, 148, 'c')]
        bp = catmull(blade)
        c.fill(bp, const((40, 40, 42)), outline=3)
        c.unmask(bp)
        tip = [(CX + d * 242, 126, 'c'), (CX + d * 262, 128), (CX + d * 278, 138, 'c'), (CX + d * 262, 148),
               (CX + d * 242, 150, 'c')]
        c.fill(catmull(tip), const((236, 196, 30)), outline=0)
    spinner = [(CX, 76, 'c'), (650, 90), (668, 120), (672, 150, 'c'), (608, 150, 'c'), (612, 120), (630, 90)]
    sp = catmull(spinner)
    c.fill(sp, radial(628, 110, 70, 235, 120, tint=(1.0, 0.32, 0.26)), outline=5)
    c.unmask(sp)

    # Framed greenhouse canopy (unpainted), rear gunner's section at the back.
    canopy = catmull(CANOPY)
    c.fill(canopy, radial(626, 430, 210, 150, 30, tint=(0.62, 0.80, 0.92)), outline=5)
    c.unmask(canopy)
    for y in (410, 470, 530, 572):
        c.line([(602, y), (678, y)], color=(40, 44, 46), width=4)
    c.line([(CX, 362), (CX, 610)], color=(40, 44, 46), width=3.5)
    gloss = [(620 + 8 * math.cos(t), 440 + 26 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 30)]
    c.fill(gloss, const((235, 248, 255)), outline=0)
    # Rear gun barrel.
    gun = [(CX - 4, 600, 'c'), (CX + 4, 600, 'c'), (CX + 4, 690, 'c'), (CX - 4, 690, 'c')]
    c.fill(P(gun), const((50, 50, 52)), outline=2.5)
    c.unmask(P(gun))

    return c.image()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    north, north_m = draw()
    views = {
        "north": (north, north_m),
        "south": (north.transpose(Image.FLIP_TOP_BOTTOM), north_m.transpose(Image.FLIP_TOP_BOTTOM)),
        "east": (north.rotate(-90), north_m.rotate(-90)),
    }
    for diag in ("northEast", "southEast", "northWest", "southWest"):
        views[diag] = views["east"]
    for name, (img, mk) in views.items():
        img.save(OUT / f"Buzzard_{name}.png", optimize=True)
        mk.save(OUT / f"Buzzard_{name}m.png", optimize=True)
    print("wrote", len(views) * 2, "textures to", OUT)


if __name__ == "__main__":
    main()
