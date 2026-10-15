"""Derives the Blockbuster and Neum bomb item textures from the high-explosive bomb
art by recolouring its body and its yellow warning band.

    python Source/Art/bomb_variants.py <repo root>
"""
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
SRC = ROOT / "Textures/Things/Item/Resource/Bomb/HighExplosive"

# name -> (body multiply RGB, band RGB)
VARIANTS = {
    "Blockbuster": ((0.62, 0.62, 0.66), (205, 40, 30)),   # dark steel, red band
    "Neum": ((0.78, 0.98, 0.80), (40, 230, 240)),         # uranium-green body, EMP-cyan band
    "Smoke": ((1.30, 1.30, 1.30), (235, 235, 235)),       # pale grey body, white band
    "URDAM": ((1.25, 1.18, 0.80), (40, 40, 40)),          # warning-yellow body, black band
    "WhitePhosphorus": ((1.38, 1.38, 1.34), (120, 215, 120)),  # near-white body, light-green WP band
    "ToxGas": ((0.62, 0.78, 0.48), (150, 255, 40)),       # dark olive body, toxic-green band
}


def recolor(img, body_mul, band_rgb):
    img = img.convert("RGBA")
    hsv = img.convert("RGB").convert("HSV")
    px, hp = img.load(), hsv.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            h, s, v = hp[x, y]
            if 20 <= h <= 50 and s > 110 and v > 120:          # yellow band
                k = v / 255
                px[x, y] = (int(band_rgb[0] * k), int(band_rgb[1] * k), int(band_rgb[2] * k), a)
            elif v > 40:                                       # body / fins, keep outline black
                px[x, y] = (min(255, int(r * body_mul[0])), min(255, int(g * body_mul[1])),
                            min(255, int(b * body_mul[2])), a)
    return img


for name, (mul, band) in VARIANTS.items():
    out = ROOT / "Textures/Things/Item/Resource/Bomb" / name
    out.mkdir(parents=True, exist_ok=True)
    for f in sorted(SRC.glob("Bomb_*.png")):
        recolor(Image.open(f), mul, band).save(out / f.name, optimize=True)
    print("wrote", out)
