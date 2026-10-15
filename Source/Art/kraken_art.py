"""Procedurally draws a J-20-style top-down fighter sprite set (Kraken) matching the
Falcon's cartoon style: white body (tinted in-game by CutoutComplexPattern), heavy
black outline, light-grey panel lines, dark canopy and nozzles. Writes base + mask
(red = paintable, black = untinted) textures for every direction the Falcon ships."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageChops

OUT = Path(sys.argv[1])
S = 2                 # supersample factor
N = 1280 * S
CX = N // 2
OUTLINE = 9 * S
PANEL = (190, 190, 190, 255)


def P(pts, mirror=False):
    """Scale 1280-space points; optionally mirror across the centreline."""
    out = []
    for x, y in pts:
        if mirror:
            x = 1280 - x
        out.append((x * S, y * S))
    return out


def sym(pts):
    """Close a right-half outline (x >= 640) into a full symmetric polygon."""
    right = list(pts)
    left = [(1280 - x, y) for x, y in reversed(pts)]
    return right + left


# --- geometry (nose up, 1280 canvas) -------------------------------------
fuselage = sym([
    (640, 30), (654, 70), (672, 140), (690, 210), (702, 300), (714, 380),
    (722, 420),          # chine flares into intake
    (760, 440), (790, 470), (792, 560),   # caret intake lip / duct
    (786, 640), (790, 760), (790, 900), (784, 1040),
    (774, 1110), (750, 1135), (700, 1150),
    (660, 1160), (650, 1215), (640, 1235),
])

wing_r = [(790, 600), (1040, 960), (1040, 1005), (995, 1010), (800, 1030)]
canard_r = [(790, 470), (950, 590), (952, 625), (905, 628), (792, 560)]
lerx_r = [(790, 560), (835, 620), (800, 700)]
vtail_r = [(745, 900), (835, 1010), (840, 1105), (810, 1112), (745, 1080)]
vfin_r = [(770, 1040), (812, 1110), (800, 1150), (775, 1140)]
nozzle_r = (692, 1118, 768, 1178)   # ellipse bbox
canopy = [(640, 205), (662, 240), (672, 300), (668, 380), (652, 425),
          (640, 432), (628, 425), (612, 380), (608, 300), (618, 240)]
radome_line = [(598, 180), (640, 172), (682, 180)]


def poly(draw, pts, fill, outline=(0, 0, 0, 255), width=OUTLINE):
    draw.polygon(pts, fill=fill)
    draw.line(pts + [pts[0]], fill=outline, width=width, joint="curve")


def render():
    base = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    mask = Image.new("RGBA", (N, N), (255, 0, 0, 255))
    d = ImageDraw.Draw(base)
    md = ImageDraw.Draw(mask)
    white = (255, 255, 255, 255)
    light = (232, 232, 232, 255)

    # Back-to-front: ventral fins, wings, canards, LERX, fuselage, tails, details.
    for m in (False, True):
        poly(d, P(vfin_r, m), light)
    for m in (False, True):
        poly(d, P(wing_r, m), white)
        # flaperon / aileron hinge lines
        d.line(P([(805, 990), (1000, 975)], m), fill=PANEL, width=3 * S)
        d.line(P([(905, 985), (905, 1022)], m), fill=PANEL, width=3 * S)
        d.line(P([(830, 660), (1010, 930)], m), fill=PANEL, width=3 * S)
    for m in (False, True):
        poly(d, P(canard_r, m), white)
        d.line(P([(805, 500), (930, 600)], m), fill=PANEL, width=3 * S)
    for m in (False, True):
        poly(d, P(lerx_r, m), white)

    poly(d, P(fuselage), white)

    # Fuselage panel lines: intake ducts, spine, engine bays, radome seam.
    for m in (False, True):
        d.line(P([(722, 420), (742, 560), (760, 760), (762, 960)], m), fill=PANEL, width=3 * S)
        d.line(P([(760, 440), (760, 520)], m), fill=(150, 150, 150, 255), width=4 * S)
        for y in (720, 780, 840):
            d.rounded_rectangle(P([(690, y), (730, y + 40)], m) if not m else
                                [(1280 - 730) * S, y * S, (1280 - 690) * S, (y + 40) * S],
                                radius=6 * S, outline=PANEL, width=3 * S)
    d.line(P([(640, 440), (640, 1100)]), fill=PANEL, width=3 * S)
    d.line(P(radome_line), fill=(120, 120, 120, 255), width=4 * S, joint="curve")

    # Nozzles (untinted).
    for m in (False, True):
        x0, y0, x1, y1 = nozzle_r
        if m:
            x0, x1 = 1280 - x1, 1280 - x0
        box = [x0 * S, y0 * S, x1 * S, y1 * S]
        d.ellipse(box, fill=(45, 45, 45, 255), outline=(0, 0, 0, 255), width=OUTLINE)
        inner = [box[0] + 14 * S, box[1] + 12 * S, box[2] - 14 * S, box[3] - 12 * S]
        d.ellipse(inner, fill=(20, 20, 20, 255), outline=(90, 90, 90, 255), width=3 * S)
        md.ellipse(box, fill=(0, 0, 0, 255))

    # Canted all-moving vertical tails sit on top in plan view.
    for m in (False, True):
        poly(d, P(vtail_r, m), (240, 240, 240, 255))
        d.line(P([(760, 925), (825, 1040)], m), fill=PANEL, width=3 * S)

    # Canopy (untinted) with highlight and frame.
    poly(d, P(canopy), (35, 35, 35, 255))
    d.line(P([(612, 330), (668, 330)]), fill=(90, 90, 90, 255), width=4 * S)
    d.ellipse(P([(624, 240), (646, 300)])[0] + P([(624, 240), (646, 300)])[1],
              fill=(150, 150, 150, 255))
    md.polygon(P(canopy), fill=(0, 0, 0, 255))

    # Soft shading: darken toward the outer edges, light from upper-left.
    shade = Image.radial_gradient("L").resize((N, N))           # 0 centre -> 255 edge
    shade = shade.point(lambda v: 255 - int(v * 0.10))          # 255 .. ~230
    rgb = base.convert("RGB")
    rgb = ImageChops.multiply(rgb, Image.merge("RGB", (shade, shade, shade)))
    base = Image.merge("RGBA", (*rgb.split(), base.getchannel("A")))

    small = base.resize((1280, 1280), Image.LANCZOS)
    msmall = mask.resize((1280, 1280), Image.NEAREST)
    return small, msmall


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    north, north_m = render()
    views = {
        "north": (north, north_m),
        "south": (north.transpose(Image.FLIP_TOP_BOTTOM), north_m.transpose(Image.FLIP_TOP_BOTTOM)),
        "east": (north.rotate(-90), north_m.rotate(-90)),
    }
    # Falcon convention: every diagonal is the east-facing side view (VF flips/rotates).
    for diag in ("northEast", "southEast", "northWest", "southWest"):
        views[diag] = views["east"]
    for name, (img, m) in views.items():
        img.save(OUT / f"Kraken_{name}.png", optimize=True)
        m.save(OUT / f"Kraken_{name}m.png", optimize=True)
    print("wrote", sorted(p.name for p in OUT.iterdir()))


if __name__ == "__main__":
    main()
