"""Draws the URDAM bomb item textures (Bomb_a..d, 1-4 bombs stacked diagonally): a
JDAM-style guided bomb - slim olive body, side strakes, grey guidance tail kit with
fins - with a yellow nuclear band and trefoil.

    python Source/Art/urdam_art.py <repo root>
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
OUT = ROOT / "Textures/Things/Item/Resource/Bomb/URDAM"
S = 4
SIZE = 128
BLACK = (0, 0, 0, 255)


def single(length=110):
    """One bomb pointing up, on a square canvas."""
    n = SIZE * S
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    L = length * S
    cx, top = n / 2, (n - L) / 2
    bot = top + L
    w = L * 0.085
    ol = int(2.2 * S)
    body = (118, 128, 92, 255)
    kit = (150, 152, 150, 255)
    # tail fins (X-tail seen from above: two wide, slightly swept)
    for sgn in (-1, 1):
        d.polygon([(cx + sgn * w * 0.8, bot - L * 0.20), (cx + sgn * w * 2.3, bot - L * 0.06),
                   (cx + sgn * w * 2.3, bot), (cx + sgn * w * 0.8, bot - L * 0.03)],
                  fill=kit, outline=BLACK, width=ol)
    # side strakes along the mid body
    for sgn in (-1, 1):
        d.polygon([(cx + sgn * w, top + L * 0.38), (cx + sgn * w * 1.55, top + L * 0.46),
                   (cx + sgn * w * 1.55, top + L * 0.62), (cx + sgn * w, top + L * 0.66)],
                  fill=kit, outline=BLACK, width=ol)
    # body + ogive nose
    d.rounded_rectangle([cx - w, top + L * 0.16, cx + w, bot - L * 0.22], radius=int(w * 0.3),
                        fill=body, outline=BLACK, width=ol)
    d.chord([cx - w, top, cx + w, top + L * 0.34], 180, 360, fill=body, outline=BLACK, width=ol)
    d.rectangle([cx - w + ol, top + L * 0.165, cx + w - ol, top + L * 0.19], fill=body)
    # guidance tail kit (tapered cone)
    d.polygon([(cx - w, bot - L * 0.23), (cx + w, bot - L * 0.23),
               (cx + w * 0.75, bot - L * 0.02), (cx - w * 0.75, bot - L * 0.02)],
              fill=kit, outline=BLACK, width=ol)
    d.line([(cx - w * 0.9, bot - L * 0.15), (cx + w * 0.9, bot - L * 0.15)], fill=(95, 95, 95, 255), width=ol)
    # yellow nuclear band + trefoil
    d.rectangle([cx - w + ol, top + L * 0.24, cx + w - ol, top + L * 0.29], fill=(245, 205, 20, 255))
    ty, r = top + L * 0.70, w * 0.62
    d.ellipse([cx - r, ty - r, cx + r, ty + r], fill=(245, 205, 20, 255), outline=BLACK, width=max(1, ol // 2))
    for k in range(3):
        a0 = -90 + k * 120 - 30
        d.pieslice([cx - r * 0.9, ty - r * 0.9, cx + r * 0.9, ty + r * 0.9], a0, a0 + 60, fill=BLACK)
    d.ellipse([cx - r * 0.22, ty - r * 0.22, cx + r * 0.22, ty + r * 0.22], fill=BLACK)
    # highlight
    d.line([(cx - w * 0.45, top + L * 0.32), (cx - w * 0.45, top + L * 0.66)],
           fill=(170, 180, 140, 255), width=ol)
    # match the other bomb icons: tilted 45 degrees, nose to the upper right
    return im.rotate(-45, resample=Image.BICUBIC).resize((SIZE, SIZE), Image.LANCZOS)


def stack(count):
    """Bombs side by side, offset perpendicular to their 45-degree axis."""
    one = single(100 if count > 1 else 110)
    big = Image.new("RGBA", (SIZE * 2, SIZE * 2), (0, 0, 0, 0))
    step = 14
    for i in range(count):
        k = (i - (count - 1) / 2) * step
        big.alpha_composite(one, (int(SIZE / 2 + k), int(SIZE / 2 + k)))
    return big.crop((SIZE // 2, SIZE // 2, SIZE // 2 + SIZE, SIZE // 2 + SIZE))


OUT.mkdir(parents=True, exist_ok=True)
for i, letter in enumerate("abcd", start=1):
    stack(i).save(OUT / f"Bomb_{letter}.png", optimize=True)
print("wrote", OUT)
