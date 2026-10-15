"""Draws the bunker buster item textures (Bomb_a..d, 1-4 bombs stacked) in the original
mod's art style (see rocket_style.py): long slim GBU-28-like penetrator with a gunmetal
casing, hardened dark steel nose, laser-seeker ring, strakes and a tail kit with X fins,
orange-brown "hardened target" band and stencilled markings - tilted 45 degrees like the
other bomb icons.

    python Source/Art/bunker_buster_art.py <repo root>
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from PIL import Image  # noqa: E402
from rocket_style import Sprite, add_outline, ellipse, poly, rrect, stack  # noqa: E402

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
OUT = ROOT / "Textures/Things/Item/Resource/Bomb/BunkerBuster"

BODY = (150, 156, 150)
NOSE = (84, 86, 88)
KIT = (128, 130, 128)
BAND = (196, 112, 40)


def bunker_buster(size=128):
    """Nose-up bunker buster, without outline."""
    sp = Sprite(size)
    cx, hw = 64, 7
    nose_top, body_top, body_bot, kit_bot = 4, 22, 100, 122

    # Tail fins (X tail seen side-on) and long mid strakes, behind the body.
    for sgn in (-1, 1):
        sp.flat(poly([(cx + sgn * 5, 104), (cx + sgn * 17, 114), (cx + sgn * 17, 122), (cx + sgn * 5, 119)]),
                KIT, grad=(1.1, 0.7), axis=(sgn, 0), bounds=(sgn * cx, sgn * (cx + 19)))
        sp.flat(poly([(cx + sgn * hw, 50), (cx + sgn * (hw + 3.5), 56), (cx + sgn * (hw + 3.5), 82),
                      (cx + sgn * hw, 86)]), KIT, grad=(1.05, 0.75), axis=(sgn, 0), bounds=(sgn * cx, sgn * (cx + 12)))
    # Canards behind the seeker.
    for sgn in (-1, 1):
        sp.flat(poly([(cx + sgn * hw, 26), (cx + sgn * (hw + 5), 30), (cx + sgn * (hw + 5), 33),
                      (cx + sgn * hw, 34)]), KIT, grad=(1.1, 0.75), axis=(sgn, 0), bounds=(sgn * cx, sgn * (cx + 13)))
    # Body: long slim cylinder.
    sp.cylinder(rrect(cx - hw, body_top, cx + hw, body_bot, 2), BODY, cx, hw, body_top, body_bot)
    # Hardened steel nose: sharp ogive.
    sp.cylinder(poly([(cx - hw, body_top + 2), (cx - hw + 1, 15), (cx - 3.5, 8), (cx, nose_top),
                      (cx + 3.5, 8), (cx + hw - 1, 15), (cx + hw, body_top + 2)]), NOSE, cx, hw, gloss=0.3)
    # Laser-seeker ring where the nose meets the body.
    sp.cylinder(rrect(cx - hw - 0.6, body_top - 1, cx + hw + 0.6, body_top + 3, 1), (60, 62, 64), cx, hw + 0.6)
    # Tail kit: tapered cone, seam and centre fin.
    sp.cylinder(poly([(cx - hw, body_bot - 1), (cx + hw, body_bot - 1), (cx + 5, kit_bot), (cx - 5, kit_bot)]),
                KIT, cx, hw)
    sp.line([(cx - hw + 1, 108), (cx + hw - 1, 108)], col=(90, 90, 90), width=0.8)
    sp.flat(rrect(cx - 1, 104, cx + 1, 123, 1), KIT, grad=(1.0, 0.8))
    # Bands, panel seams and stencils.
    for y in (36, 42):
        sp.cylinder(rrect(cx - hw + 0.3, y, cx + hw - 0.3, y + 2.6, 0), BAND, cx, hw, gloss=0.1)
    for y in (60, 88):
        sp.line([(cx - hw + 0.5, y), (cx + hw - 0.5, y)], col=(100, 104, 100), width=0.7, alpha=0.8)
    sp.stencil(cx - 3.5, 66, [3, 2, 3], w=1.2, h=1.6, gap=0.7, seed=8)
    sp.solid(ellipse(cx - 2.6, 12, 0.8, 2.6), (170, 172, 176))  # nose glint
    return sp


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    single = bunker_buster().image(outline=0)
    for count, letter in enumerate("abcd", start=1):
        scale = 1.12 if count == 1 else 0.98
        s = single.resize((int(128 * scale), int(128 * scale)), Image.LANCZOS)
        framed = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        framed.alpha_composite(s, ((128 - s.width) // 2, (128 - s.height) // 2))
        tilted = framed.rotate(-45, resample=Image.BICUBIC)
        add_outline(stack(tilted, count, step=12), 5.0).save(OUT / f"Bomb_{letter}.png", optimize=True)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
