"""Draws the strafing missile: projectile sprite (nose up), its rocket-motor flame
(root at the top, tail pointing down) and the cargo item icon.

    python Source/Art/missile_art.py <repo root>
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
S = 4  # supersample


def missile(size=128, band=(200, 40, 30, 255), seeker=False):
    n = size * S
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = n / 2
    w = n * 0.085           # body half-width
    top, bot = n * 0.08, n * 0.90
    ol = int(n * 0.02)
    black = (0, 0, 0, 255)
    # tail fins
    for sgn in (-1, 1):
        d.polygon([(cx + sgn * w, bot - n * 0.20), (cx + sgn * (w + n * 0.13), bot - n * 0.02),
                   (cx + sgn * (w + n * 0.13), bot + n * 0.03), (cx + sgn * w, bot)],
                  fill=(120, 124, 120, 255), outline=black, width=ol)
        # mid canards
        d.polygon([(cx + sgn * w, top + n * 0.30), (cx + sgn * (w + n * 0.07), top + n * 0.40),
                   (cx + sgn * (w + n * 0.07), top + n * 0.43), (cx + sgn * w, top + n * 0.43)],
                  fill=(120, 124, 120, 255), outline=black, width=ol)
    # body
    d.rounded_rectangle([cx - w, top + n * 0.12, cx + w, bot], radius=int(w * 0.4),
                        fill=(178, 182, 170, 255), outline=black, width=ol)
    # nose cone
    d.polygon([(cx - w, top + n * 0.14), (cx, top), (cx + w, top + n * 0.14)],
              fill=(70, 70, 70, 255), outline=black)
    d.line([(cx - w, top + n * 0.14), (cx, top), (cx + w, top + n * 0.14)], fill=black, width=ol)
    # warhead band + highlight
    d.rectangle([cx - w + ol, top + n * 0.18, cx + w - ol, top + n * 0.22], fill=band)
    if seeker:
        # glass seeker dome over the nose
        d.ellipse([cx - w * 0.85, top + n * 0.015, cx + w * 0.85, top + n * 0.16],
                  fill=(70, 110, 140, 255), outline=black, width=ol // 2)
        d.ellipse([cx - w * 0.45, top + n * 0.04, cx - w * 0.05, top + n * 0.09], fill=(190, 225, 245, 255))
    d.line([(cx - w * 0.35, top + n * 0.26), (cx - w * 0.35, bot - n * 0.06)],
           fill=(225, 228, 220, 255), width=int(ol * 0.8))
    # nozzle
    d.rectangle([cx - w * 0.7, bot - ol, cx + w * 0.7, bot + n * 0.03], fill=(40, 40, 40, 255),
                outline=black, width=ol // 2)
    return im.resize((size, size), Image.LANCZOS)


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


def icon(size=128, **kw):
    """Cargo item: two missiles lying diagonally."""
    m = missile(size, **kw)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    for dx in (-14, 12):
        r = m.rotate(-45, resample=Image.BICUBIC, expand=False)
        out.alpha_composite(r, (dx, -dx // 2))
    return out


proj = ROOT / "Textures/Things/Projectile"
proj.mkdir(parents=True, exist_ok=True)
missile().save(proj / "ROCKET_Missile.png", optimize=True)
flame().save(proj / "ROCKET_MissileFlame.png", optimize=True)
item = ROOT / "Textures/Things/Item/Resource/Missile"
item.mkdir(parents=True, exist_ok=True)
icon().save(item / "StrafingMissile.png", optimize=True)
# Seeker missile: yellow band and a glass seeker-head dome.
seek = dict(band=(240, 200, 30, 255), seeker=True)
missile(**seek).save(proj / "ROCKET_SeekerMissile.png", optimize=True)
icon(**seek).save(item / "SeekerMissile.png", optimize=True)
print("wrote missile textures")
