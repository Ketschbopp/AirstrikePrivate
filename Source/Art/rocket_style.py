"""Shared renderer that imitates the original mod art (Rocket's bombs): heavy soft black
outline, rounded cylinder shading lit from the upper left with a dark lower-right rim,
shaded grey fins and small stencilled markings. Parts are drawn nose-up and the
finished sprite can be tilted 45 degrees like the bomb item icons.
"""
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 4  # supersample factor


class Sprite:
    def __init__(self, size):
        self.size = size
        self.n = size * S
        self.rgb = np.zeros((self.n, self.n, 3), np.float32)
        self.a = np.zeros((self.n, self.n), np.float32)
        ys, xs = np.mgrid[0:self.n, 0:self.n].astype(np.float32)
        self.xs, self.ys = xs / S, ys / S  # output-pixel units

    # -- masks ---------------------------------------------------------------
    def _mask(self, draw_fn):
        m = Image.new("L", (self.n, self.n), 0)
        draw_fn(ImageDraw.Draw(m), S)
        return np.asarray(m, np.float32) / 255

    def _put(self, cov, col):
        self.rgb = self.rgb * (1 - cov[..., None]) + col * cov[..., None]
        self.a = np.maximum(self.a, cov)

    # -- shaded parts --------------------------------------------------------
    def cylinder(self, draw_fn, base, cx, half_w, y0=None, y1=None, gloss=0.18):
        """Fill a part with round-body shading across x (lit from the left)."""
        cov = self._mask(draw_fn)
        u = np.clip((self.xs - cx) / max(half_w, 1e-3), -1, 1)
        # Lambert-ish across the cylinder, light from upper left.
        nx = u
        nz = np.sqrt(np.clip(1 - u * u, 0, 1))
        light = np.clip(-0.55 * nx + 0.83 * nz, 0, 1)
        shade = 0.42 + 0.68 * light
        # Specular streak left of centre.
        shade += gloss * np.exp(-((u + 0.42) / 0.16) ** 2)
        # Slight darkening toward the ends of the part.
        if y0 is not None and y1 is not None:
            t = np.clip((self.ys - y0) / max(y1 - y0, 1e-3), 0, 1)
            shade *= 1 - 0.10 * (np.abs(t - 0.45) * 2) ** 2
        col = np.clip(np.array(base, np.float32)[None, None, :] * shade[..., None], 0, 255)
        self._put(cov, col)
        return cov

    def flat(self, draw_fn, base, grad=(1.0, 0.75), axis=(0.0, 1.0), bounds=(0.0, 1.0)):
        """Fill with a linear light-to-dark gradient (fins, kits)."""
        cov = self._mask(draw_fn)
        ax, ay = axis
        p = (self.xs * ax + self.ys * ay - bounds[0]) / max(bounds[1] - bounds[0], 1e-3)
        f = grad[0] + (grad[1] - grad[0]) * np.clip(p, 0, 1)
        col = np.clip(np.array(base, np.float32)[None, None, :] * f[..., None], 0, 255)
        self._put(cov, col)
        return cov

    def solid(self, draw_fn, col):
        cov = self._mask(draw_fn)
        self._put(cov, np.array(col, np.float32)[None, None, :])
        return cov

    def stencil(self, x, y, rows, w=1.0, h=1.3, gap=0.5, col=(30, 30, 28), alpha=0.85, seed=1):
        """Little blocks of pseudo text, like the stencilled markings on Rocket's bombs."""
        rnd = np.random.RandomState(seed)
        def draw(d, s):
            yy = y
            for r in rows:
                xx = x
                for _ in range(r):
                    if rnd.rand() < 0.85:
                        d.rectangle([xx * s, yy * s, (xx + w) * s, (yy + h) * s], fill=255)
                    xx += w + gap * 0.6
                yy += h + gap
        cov = self._mask(draw) * alpha
        self._put(cov, np.array(col, np.float32)[None, None, :])

    def line(self, pts, col=(25, 25, 25), width=1.0, alpha=1.0):
        def draw(d, s):
            d.line([(px * s, py * s) for px, py in pts], fill=255, width=max(1, int(width * s)))
        self._put(self._mask(draw) * alpha, np.array(col, np.float32)[None, None, :])

    # -- finish --------------------------------------------------------------
    def image(self, outline=4.5, tilt=0.0):
        """Downsample, add the heavy soft black outline, optionally tilt."""
        rgb = np.clip(self.rgb, 0, 255).astype(np.uint8)
        a = np.clip(self.a * 255, 0, 255).astype(np.uint8)
        body = Image.fromarray(np.dstack([rgb, a]), "RGBA")
        if tilt:
            body = body.rotate(tilt, resample=Image.BICUBIC)
        # Soft dilation of the silhouette -> black outline behind the art.
        alpha = body.getchannel("A")
        grown = alpha.filter(ImageFilter.GaussianBlur(outline * S * 0.55))
        grown = grown.point(lambda v: 255 if v > 18 else int(v * 14))
        outline_img = Image.new("RGBA", body.size, (8, 8, 8, 0))
        outline_img.putalpha(grown)
        out = Image.alpha_composite(outline_img, body)
        return out.resize((self.size, self.size), Image.LANCZOS)


def add_outline(img, px=4.5):
    """Heavy soft black outline around an already-composed RGBA image."""
    up = img.resize((img.width * S, img.height * S), Image.LANCZOS)
    alpha = up.getchannel("A")
    grown = alpha.filter(ImageFilter.GaussianBlur(px * S * 0.55))
    grown = grown.point(lambda v: 255 if v > 18 else int(v * 14))
    ol = Image.new("RGBA", up.size, (8, 8, 8, 0))
    ol.putalpha(grown)
    return Image.alpha_composite(ol, up).resize(img.size, Image.LANCZOS)


def stack(single, count, step=14, size=128):
    """1-4 copies offset perpendicular to a 45-degree axis (item stack textures)."""
    big = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
    for i in range(count):
        k = (i - (count - 1) / 2) * step
        big.alpha_composite(single, (int(size / 2 + k), int(size / 2 + k)))
    return big.crop((size // 2, size // 2, size // 2 + size, size // 2 + size))


def ellipse(cx, cy, rx, ry):
    return lambda d, s: d.ellipse([(cx - rx) * s, (cy - ry) * s, (cx + rx) * s, (cy + ry) * s], fill=255)


def poly(pts):
    return lambda d, s: d.polygon([(x * s, y * s) for x, y in pts], fill=255)


def rrect(x0, y0, x1, y1, r):
    return lambda d, s: d.rounded_rectangle([x0 * s, y0 * s, x1 * s, y1 * s], radius=r * s, fill=255)


def trefoil(sp, cx, cy, r):
    """Radiation trefoil: yellow disc, three black blades, black hub."""
    sp.solid(ellipse(cx, cy, r, r), (245, 205, 25))
    def blades(d, s):
        for k in range(3):
            a0 = -90 + k * 120 - 30
            d.pieslice([(cx - r * 0.86) * s, (cy - r * 0.86) * s, (cx + r * 0.86) * s, (cy + r * 0.86) * s],
                       a0, a0 + 60, fill=255)
    sp.solid(blades, (15, 15, 15))
    sp.solid(ellipse(cx, cy, r * 0.22, r * 0.22), (15, 15, 15))
    sp.solid(ellipse(cx, cy, r * 0.13, r * 0.13), (245, 205, 25))
