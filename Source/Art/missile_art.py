"""Draws the strafing missile and the seeker missile in the original mod's art style
(see rocket_style.py): projectile sprites (nose up), the rocket-motor flame (root at
the top, tail pointing down) and the cargo item icons (two missiles tilted 45 degrees).

    python Source/Art/missile_art.py <repo root>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
from rocket_style import S, Sprite, add_outline, ellipse, poly, rrect  # noqa: E402

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")

BODY = (200, 202, 188)
FIN = (150, 154, 150)
DARK = (78, 78, 76)


def missile(seeker=False, size=128):
    """Nose-up missile on a size x size canvas, without outline."""
    sp = Sprite(size)
    cx, hw = 64, 9
    top_body, bot = 33, 111

    # Tail fins and mid wings (behind the body), shaded light->dark outward.
    for sgn in (-1, 1):
        sp.flat(poly([(cx + sgn * hw, 90), (cx + sgn * (hw + 14), 106), (cx + sgn * (hw + 14), 113),
                      (cx + sgn * hw, 111)]), FIN, grad=(1.1, 0.7), axis=(sgn, 0), bounds=(sgn * cx, sgn * (cx + 24)))
        sp.flat(poly([(cx + sgn * hw, 50), (cx + sgn * (hw + 7), 59), (cx + sgn * (hw + 7), 63),
                      (cx + sgn * hw, 64)]), FIN, grad=(1.1, 0.75), axis=(sgn, 0), bounds=(sgn * cx, sgn * (cx + 17)))
    # Body.
    sp.cylinder(rrect(cx - hw, top_body, cx + hw, bot, 3), BODY, cx, hw, top_body, bot)
    # Nozzle.
    sp.cylinder(rrect(cx - 6, bot - 2, cx + 6, bot + 6, 2), (60, 60, 58), cx, 6)
    sp.solid(ellipse(cx, bot + 5.5, 4.2, 1.2), (20, 20, 20))
    # Panel seams and centre fin.
    for y in (46, 78, 96):
        sp.line([(cx - hw + 0.5, y), (cx + hw - 0.5, y)], col=(120, 122, 116), width=0.7, alpha=0.8)
    sp.flat(rrect(cx - 1.2, 92, cx + 1.2, 114, 1), FIN, grad=(1.0, 0.8))

    if seeker:
        # Glass seeker dome.
        sp.cylinder(poly([(cx - hw, 35), (cx - hw + 1, 26), (cx - 5, 18), (cx, 15), (cx + 5, 18),
                          (cx + hw - 1, 26), (cx + hw, 35)]), (60, 104, 140), cx, hw, gloss=0.45)
        sp.solid(ellipse(cx - 3.2, 22, 1.6, 3.2), (225, 240, 250))
        band = (236, 196, 30)
    else:
        # Dark ogive nose.
        sp.cylinder(poly([(cx - hw, 35), (cx - hw + 1.5, 25), (cx - 4.5, 17), (cx, 11), (cx + 4.5, 17),
                          (cx + hw - 1.5, 25), (cx + hw, 35)]), DARK, cx, hw)
        band = (196, 42, 32)
    # Warhead band.
    sp.cylinder(rrect(cx - hw + 0.3, 37, cx + hw - 0.3, 41, 0), band, cx, hw, gloss=0.1)
    # Stencilled markings.
    sp.stencil(cx - 4.5, 58, [4, 3, 4], w=1.3, h=1.7, gap=0.7, seed=3 if seeker else 2)
    return sp


def flame(w=64, h=256):
    n_w, n_h = w * S, h * S
    im = Image.new("RGBA", (n_w, n_h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = n_w / 2
    # layered tapering flame: outer orange, inner yellow, white core at the root (top)
    for frac, col in ((1.0, (255, 110, 20, 150)), (0.65, (255, 190, 60, 210)), (0.3, (255, 250, 220, 255))):
        hw = n_w * 0.42 * frac
        d.polygon([(cx - hw, 0), (cx + hw, 0), (cx, n_h * (0.95 * frac + 0.05 * (1 - frac)))], fill=col)
    im = im.filter(ImageFilter.GaussianBlur(S * 3))
    return im.resize((w, h), Image.LANCZOS)


def icon(seeker=False, size=128):
    """Cargo item: two missiles side by side, tilted 45 degrees (nose to upper right)."""
    single = missile(seeker).image(outline=0)
    single = single.resize((int(size * 1.08), int(size * 1.08)), Image.LANCZOS)
    tilted = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    tilted.alpha_composite(single, ((size - single.width) // 2, (size - single.height) // 2))
    tilted = tilted.rotate(-45, resample=Image.BICUBIC)
    out = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
    for k in (-10, 10):
        out.alpha_composite(tilted, (size // 2 + k, size // 2 + k))
    out = out.crop((size // 2, size // 2, size // 2 + size, size // 2 + size))
    return add_outline(out, 5.0)


proj = ROOT / "Textures/Things/Projectile"
proj.mkdir(parents=True, exist_ok=True)
item = ROOT / "Textures/Things/Item/Resource/Missile"
item.mkdir(parents=True, exist_ok=True)

add_outline(missile().image(outline=0), 3.2).save(proj / "ROCKET_Missile.png", optimize=True)
add_outline(missile(seeker=True).image(outline=0), 3.2).save(proj / "ROCKET_SeekerMissile.png", optimize=True)
flame().save(proj / "ROCKET_MissileFlame.png", optimize=True)
icon().save(item / "StrafingMissile.png", optimize=True)
icon(seeker=True).save(item / "SeekerMissile.png", optimize=True)
print("wrote missile textures")
