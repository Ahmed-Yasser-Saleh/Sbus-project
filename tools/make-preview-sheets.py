"""Verification contact sheet for the cropped logo. Read-only: builds images
for eyeballing, writes to tools/_preview/, does not touch any asset."""
import os
import numpy as np
from PIL import Image

SRC = r"F:\Downloads\WhatsApp Image 2026-10-08 at 11.21.27 PM.jpeg"
WWW = os.path.dirname(os.path.dirname(os.path.abspath(__file__))) + r"\src\SBus.Web\wwwroot"
OUT = os.path.dirname(os.path.abspath(__file__)) + r"\_preview"
os.makedirs(OUT, exist_ok=True)

orig = Image.open(SRC).convert("RGB")
full = Image.open(WWW + r"\img\logo-full.png").convert("RGB")


def on(colour, im=full):
    L = Image.new("RGB", im.size, colour)
    L.paste(im, (0, 0))
    return L


def tb(im, w):
    return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


W, BG = 430, (196, 205, 209)

# A: original photo vs crop
a = Image.new("RGB", (W + 12, tb(orig, W).height + tb(full, W).height + 18), (170, 170, 170))
t1 = tb(orig, W)
a.paste(t1, (6, 6))
a.paste(tb(full, W), (6, t1.height + 12))
a.save(OUT + r"\A-original-vs-crop.png")

# B: crop on the four surfaces it will sit on
rows = [tb(on(c), W) for c in (BG, (255, 255, 255), (26, 42, 85), (238, 243, 248))]
b = Image.new("RGB", (W + 12, sum(r.height for r in rows) + 24), (170, 170, 170))
y = 6
for r in rows:
    b.paste(r, (6, y))
    y += r.height + 6
b.save(OUT + r"\B-on-surfaces.png")

# C: favicon tiles at real size
c = Image.new("RGB", (5 * 54 + 6, 60), (170, 170, 170))
x = 6
for f in ("512", "128", "48", "32", "16"):
    im = Image.open(WWW + r"\icons\logo-icon-%sx%s.png" % (f, f)).convert("RGB")
    im = im.resize((48, 48), Image.LANCZOS)
    c.paste(im, (x, 6))
    x += 54
c.save(OUT + r"\C-tiles.png")
print("sheets:", sorted(os.listdir(OUT)))
