"""Draws the tactical nuclear bomb item textures (Bomb_a..d, 1-4 bombs stacked) in the
original mod's art style (see rocket_style.py): olive body like the high-explosive bomb,
side strakes, grey tail kit with X fins, yellow nuclear band and trefoil, stencilled
markings - tilted 45 degrees like the other bomb icons.

(Originally drawn for the URDAM; the two textures were swapped to fit better.)

    python Source/Art/tactical_nuke_art.py <repo root>
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from PIL import Image  # noqa: E402
from rocket_style import Sprite, add_outline, ellipse, poly, rrect, stack, trefoil  # noqa: E402

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
OUT = ROOT / "Textures/Things/Item/Resource/Bomb/TacticalNuke"

OLIVE = (176, 182, 140)
KIT = (150, 152, 150)


def nuke(size=128):
    """Nose-up tactical nuke, without outline."""
    sp = Sprite(size)
    cx, hw = 64, 10.5
    nose_top, body_top, body_bot, kit_bot = 12, 34, 92, 116

    # Tail fins (X tail seen side-on) and side strakes, behind the body.
    for sgn in (-1, 1):
        sp.flat(poly([(cx + sgn * 6, 96), (cx + sgn * 20, 108), (cx + sgn * 20, 117), (cx + sgn * 6, 113)]),
                KIT, grad=(1.1, 0.7), axis=(sgn, 0), bounds=(sgn * cx, sgn * (cx + 22)))
        sp.flat(poly([(cx + sgn * hw, 52), (cx + sgn * (hw + 4.5), 58), (cx + sgn * (hw + 4.5), 76),
                      (cx + sgn * hw, 80)]), KIT, grad=(1.05, 0.75), axis=(sgn, 0), bounds=(sgn * cx, sgn * (cx + 16)))
    # Ogive nose + body.
    sp.cylinder(poly([(cx - hw, body_top + 2), (cx - hw + 1.5, 24), (cx - 6, 16), (cx, nose_top),
                      (cx + 6, 16), (cx + hw - 1.5, 24), (cx + hw, body_top + 2)]), OLIVE, cx, hw)
    sp.cylinder(rrect(cx - hw, body_top, cx + hw, body_bot, 2), OLIVE, cx, hw, body_top, body_bot)
    # Guidance tail kit: tapered cone.
    sp.cylinder(poly([(cx - hw, body_bot - 1), (cx + hw, body_bot - 1), (cx + 7, kit_bot), (cx - 7, kit_bot)]),
                KIT, cx, hw)
    sp.line([(cx - hw + 1, 101), (cx + hw - 1, 101)], col=(95, 95, 95), width=0.8)
    sp.flat(rrect(cx - 1.2, 98, cx + 1.2, 118, 1), KIT, grad=(1.0, 0.8))
    # Yellow nuclear band + trefoil + stencils.
    sp.cylinder(rrect(cx - hw + 0.3, 38, cx + hw - 0.3, 43, 0), (238, 196, 26), cx, hw, gloss=0.1)
    trefoil(sp, cx, 70, 5.2)
    sp.stencil(cx - 5, 50, [4, 3], w=1.3, h=1.7, gap=0.7, seed=5)
    sp.stencil(cx - 4, 81, [3, 4], w=1.3, h=1.7, gap=0.7, seed=6)
    # Panel seam where the body meets the kit.
    sp.line([(cx - hw + 0.5, body_bot - 1.5), (cx + hw - 0.5, body_bot - 1.5)], col=(90, 92, 70), width=0.8)
    return sp


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    single = nuke().image(outline=0)
    for count, letter in enumerate("abcd", start=1):
        scale = 1.18 if count == 1 else 0.98
        s = single.resize((int(128 * scale), int(128 * scale)), Image.LANCZOS)
        framed = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        framed.alpha_composite(s, ((128 - s.width) // 2, (128 - s.height) // 2))
        tilted = framed.rotate(-45, resample=Image.BICUBIC)
        add_outline(stack(tilted, count, step=13), 5.0).save(OUT / f"Bomb_{letter}.png", optimize=True)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
