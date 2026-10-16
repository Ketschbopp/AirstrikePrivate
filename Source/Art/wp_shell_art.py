"""Draws the white phosphorus mortar shell item textures (Shell_a..d, 1-4 shells stacked)
in the original mod's art style (see rocket_style.py): stubby teardrop mortar round,
near-white body with the light-green WP band of the white phosphorus bomb, dark fuze
and tail-fin boom - tilted 45 degrees like the bomb icons.

    python Source/Art/wp_shell_art.py <repo root>
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from PIL import Image  # noqa: E402
from rocket_style import Sprite, add_outline, poly, rrect, stack  # noqa: E402

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
OUT = ROOT / "Textures/Things/Item/Resource/Shell/WhitePhosphorus"

BODY = (226, 228, 220)
BAND = (120, 215, 120)
DARK = (70, 70, 68)
FIN = (150, 154, 150)


def shell(size=128):
    """Nose-up mortar shell, without outline."""
    sp = Sprite(size)
    cx, hw = 64, 14
    # Tail fins behind the boom.
    for sgn in (-1, 1):
        sp.flat(poly([(cx + sgn * 4, 92), (cx + sgn * 13, 100), (cx + sgn * 13, 112), (cx + sgn * 4, 110)]),
                FIN, grad=(1.1, 0.7), axis=(sgn, 0), bounds=(sgn * cx, sgn * (cx + 15)))
    # Tail boom.
    sp.cylinder(rrect(cx - 5, 80, cx + 5, 112, 2), DARK, cx, 5)
    sp.flat(rrect(cx - 1.2, 92, cx + 1.2, 114, 1), FIN, grad=(1.0, 0.8))
    # Teardrop body: ogive nose, full belly, tapering boat-tail.
    sp.cylinder(poly([(cx - 6, 24), (cx - 11, 32), (cx - hw, 46), (cx - hw, 62), (cx - 11, 76), (cx - 6, 86),
                      (cx + 6, 86), (cx + 11, 76), (cx + hw, 62), (cx + hw, 46), (cx + 11, 32), (cx + 6, 24)]),
                BODY, cx, hw, 24, 86)
    # Fuze.
    sp.cylinder(poly([(cx - 6, 26), (cx - 4, 18), (cx, 14), (cx + 4, 18), (cx + 6, 26)]), DARK, cx, 6, gloss=0.3)
    # WP band and stencils.
    sp.cylinder(rrect(cx - hw + 0.4, 40, cx + hw - 0.4, 45, 0), BAND, cx, hw, gloss=0.1)
    sp.stencil(cx - 5, 52, [4, 3], w=1.3, h=1.7, gap=0.7, seed=11)
    sp.line([(cx - 11, 74), (cx + 11, 74)], col=(150, 152, 146), width=0.8)
    return sp


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    single = shell().image(outline=0)
    for count, letter in enumerate("abcd", start=1):
        scale = 0.95 if count == 1 else 0.8
        s = single.resize((int(128 * scale), int(128 * scale)), Image.LANCZOS)
        framed = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        framed.alpha_composite(s, ((128 - s.width) // 2, (128 - s.height) // 2))
        tilted = framed.rotate(-45, resample=Image.BICUBIC)
        add_outline(stack(tilted, count, step=16), 5.0).save(OUT / f"Shell_{letter}.png", optimize=True)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
