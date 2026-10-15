"""Draws the burning white-phosphorus fragment: a soft white-hot glow with a yellow rim.

    python Source/Art/wp_fragment_art.py <repo root>
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
n = 64
y, x = np.mgrid[0:n, 0:n].astype(np.float32)
d = np.hypot(x - (n - 1) / 2, y - (n - 1) / 2) / (n / 2)
a = np.clip(1 - d, 0, 1) ** 1.6
core = np.clip(1 - d * 2.2, 0, 1)
r = 255 * np.ones_like(d)
g = 235 + 20 * core
b = 170 + 85 * core
img = np.dstack([r, g, b, a * 255]).astype(np.uint8)
out = ROOT / "Textures/Things/Projectile"
out.mkdir(parents=True, exist_ok=True)
Image.fromarray(img, "RGBA").save(out / "ROCKET_WPFragment.png", optimize=True)
print("wrote", out / "ROCKET_WPFragment.png")
