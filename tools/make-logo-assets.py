"""Crop the SBus logo and export the raster assets the app consumes.

Source: the clean white-background logo PNG at D:\sbus-logo.png.

Rules enforced here:
  * source pixels are used as-is - nothing is redrawn, traced or vectorised
  * the crop comes from the *measured* ink box, never a hard-coded rectangle
  * margins are normalised to be symmetric (the source file's are uneven)
  * every export is a Lanczos *downscale*; scale is capped at 1.0 so nothing is
    ever upscaled
  * the aspect ratio is never changed - icons are padded, never stretched

Run from the repo root:
    python tools/make-logo-assets.py

Outputs (written to src/SBus.Web/wwwroot):
    img/logo-full.png         the wordmark, white field, 1x
    img/logo-full@2x.png      the wordmark, 2x for hi-dpi screens
    icons/logo-icon-NxN.png   square tiles, N in SIZES below
    favicon.ico               16 / 32 / 48 from the 48px tile
"""
import os

import numpy as np
from PIL import Image, ImageFilter

SRC = r"D:\sbus-logo.png"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WWW = os.path.join(ROOT, "src", "SBus.Web", "wwwroot")
IMG = os.path.join(WWW, "img")
ICONS = os.path.join(WWW, "icons")
CROP_PAD_FRAC = 0.04    # breathing room restored around the ink
TILE_PAD_FRAC = 0.16    # icons and favicons get more air than the wordmark
SIZES = (512, 256, 192, 180, 167, 152, 144, 128, 96, 80, 76, 72, 60, 48, 40, 32, 16)
for d in (IMG, ICONS):
    os.makedirs(d, exist_ok=True)

# ----------------------------------------------------------------- source ---
src = Image.open(SRC)
rgb = src.convert("RGB")
arr = np.asarray(rgb).astype(int)
print("source     %s" % SRC)
print("mode       %s (alpha: %s)" % (src.mode, "A" in src.getbands()))
print("pixels     %s  aspect %.3f" % (src.size, src.width / src.height))

# Background colour, measured from the outer frame rather than assumed.
frame = np.concatenate([arr[0], arr[-1], arr[:, 0], arr[:, -1]], 0)
BG = tuple(int(round(v)) for v in frame.mean(0))
print("background %s" % BG)

# --------------------------------------------------------------- ink box ----
lum, mx, mn = arr.mean(2), arr.max(2), arr.min(2)
ink = ((np.abs(arr - np.array(BG)).max(2) > 24) | (lum < 160)).astype(np.uint8)

# Keep only connected ink, so a speck of file noise cannot widen the box.
probe = Image.fromarray((ink * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))
kept = np.asarray(probe.filter(ImageFilter.MinFilter(5)), int) > 128

ys, xs = np.where(kept)
x0, x1, y0, y1 = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
tight = rgb.crop((x0, y0, x1 + 1, y1 + 1))
print("ink box    x %d..%d  y %d..%d" % (x0, x1, y0, y1))
print("ink size   %s  aspect %.3f" % (tight.size, tight.width / tight.height))
print("source margins  L%d R%d T%d B%d" % (x0, src.width - 1 - x1, y0, src.height - 1 - y1))

# Letter colours, read off the pixels themselves.
sky, dark = (mx - mn) > 60, lum < 110
print("letter S   #%02x%02x%02x  over %d px" % (*arr[sky].mean(0).astype(int), int(sky.sum())))
print("letter Bus #%02x%02x%02x  over %d px" % (*arr[dark].mean(0).astype(int), int(dark.sum())))


def pad_symmetric(im, frac):
    """Re-pad onto a larger canvas so the margins are equal on all sides."""
    px, py = int(im.width * frac), int(im.height * frac)
    canvas = Image.new("RGB", (im.width + 2 * px, im.height + 2 * py), BG)
    canvas.paste(im, (px, py))
    return canvas


logo = pad_symmetric(tight, CROP_PAD_FRAC)
print("crop       %s  aspect %.3f  (ink = %.1f%% of the width)"
      % (logo.size, logo.width / logo.height, 100.0 * tight.width / logo.width))

lg = np.asarray(logo.convert("RGB")).astype(int)
edge = np.concatenate([lg[0], lg[-1], lg[:, 0], lg[:, -1]], 0)
print("check      ink px on the crop border = %d  (0 means nothing is cut)"
      % int((np.abs(edge - np.array(BG)).max(1) > 24).sum()))

# ---------------------------------------------------------------- export ----
logo.save(os.path.join(IMG, "logo-full.png"))
logo.resize((logo.width * 2, logo.height * 2), Image.LANCZOS).save(
    os.path.join(IMG, "logo-full@2x.png"))


def tile(size):
    """Logo centred on its own field. Downscale only, aspect preserved."""
    canvas = Image.new("RGB", (size, size), BG)
    limit = int(size * (1 - 2 * TILE_PAD_FRAC))
    scale = min(1.0, limit / max(logo.width, logo.height))   # 1.0 = no upscale
    mark = logo.resize((max(1, int(logo.width * scale)),
                        max(1, int(logo.height * scale))), Image.LANCZOS)
    canvas.paste(mark, ((size - mark.width) // 2, (size - mark.height) // 2))
    return canvas


print("tiles")
for sz in SIZES:
    tile(sz).save(os.path.join(ICONS, "logo-icon-%dx%d.png" % (sz, sz)))
print("favicon    %s" % os.path.join(WWW, "favicon.ico"))
Image.open(os.path.join(ICONS, "logo-icon-48x48.png")).save(
    os.path.join(WWW, "favicon.ico"), sizes=[(16, 16), (32, 32), (48, 48)])

# Verify the files written to disk, not just the in-memory images.
blank = [sz for sz in SIZES
         if not (np.abs(np.asarray(Image.open(os.path.join(ICONS, "logo-icon-%dx%d.png"
                                                           % (sz, sz))).astype(int))
                        - np.array(BG)).max(2) > 24).any()]
print("check      %d/%d tiles contain logo ink" % (len(SIZES) - len(blank), len(SIZES)))

print("\nCSS geometry (from the measured aspect %.3f):" % (logo.width / logo.height))
for label, height in (("topbar h34", 34), ("topbar h40", 40),
                      ("login h120", 120), ("login h140", 140)):
    print("  %-12s -> w %3dpx" % (label, round(height * logo.width / logo.height)))
