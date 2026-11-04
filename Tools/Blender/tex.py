import numpy as np, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont
OUT = "/Users/nmoff/chillhop-crate/Assets/Textures/Chillhop/"

def gn(h, w, sx, sy, seed):
    """periodic gaussian-filtered noise in 0..1; sx/sy are blur sigmas in pixels along x / y"""
    r = np.random.RandomState(seed)
    n = r.randn(h, w)
    fy = np.fft.fftfreq(h)[:, None]; fx = np.fft.fftfreq(w)[None, :]
    filt = np.exp(-2 * (math.pi ** 2) * ((fx * sx) ** 2 + (fy * sy) ** 2))
    o = np.real(np.fft.ifft2(np.fft.fft2(n) * filt))
    o = (o - o.min()) / (o.max() - o.min() + 1e-9)
    return o

def save(a, name, mode="RGB"):
    a = np.clip(a, 0, 1)
    arr = (a * 255 + 0.5).astype(np.uint8)
    Image.fromarray(arr, mode).save(OUT + name)
    print("wrote", name, arr.shape)

# ---------------------------------------------------------------- wood floor (board 0.2 x 1.2 m)
palette = [(0.30, 0.17, 0.09), (0.25, 0.14, 0.075), (0.34, 0.20, 0.11), (0.28, 0.16, 0.085)]
for i, base in enumerate(palette):
    h, w = 512, 128
    g = gn(h, w, 1.4, 40, 10 + i)
    g2 = gn(h, w, 5, 120, 20 + i)
    lines = 0.5 + 0.5 * np.sin(2 * math.pi * (np.arange(w)[None, :] / w * (6 + i) + 2.5 * g2))
    lum = 0.62 + 0.22 * g + 0.16 * lines * g2 + 0.1 * (g2 - 0.5)
    img = np.zeros((h, w, 3))
    for c in range(3):
        img[:, :, c] = base[c] * lum * 1.55
    edge = np.minimum(np.arange(w), w - 1 - np.arange(w))[None, :]
    img *= (0.55 + 0.45 * np.clip(edge / 3.0, 0, 1))[:, :, None]
    img[:, :4, :] *= 0.9
    save(img, "floor%d.png" % i)

# ---------------------------------------------------------------- generic tileables
g = gn(256, 256, 1.0, 36, 3); g2 = gn(256, 256, 6, 90, 4)
rings = 0.5 + 0.5 * np.sin(2 * math.pi * (np.arange(256)[None, :] / 256 * 9 + 2.2 * g2))
lum = 0.62 + 0.2 * g + 0.18 * rings * g2
save(np.stack([lum * 1.1, lum * 1.0, lum * 0.92], -1), "wood.png")

p = gn(256, 256, 1.2, 1.2, 5) * 0.6 + gn(256, 256, 7, 7, 6) * 0.4
save(np.stack([0.80 + 0.2 * p] * 3, -1), "plaster.png")

# fabric weave
yy, xx = np.mgrid[0:128, 0:128]
weave = 0.5 + 0.25 * np.sin(xx * 2 * math.pi / 4) * np.sin(yy * 2 * math.pi / 4) + 0.12 * (gn(128, 128, 1, 1, 7) - 0.5)
save(np.stack([0.78 + 0.22 * weave] * 3, -1), "fabric.png")

# felt/deskmat grain
f = gn(128, 128, 0.8, 0.8, 8)
save(np.stack([0.85 + 0.15 * f] * 3, -1), "felt.png")

# ---------------------------------------------------------------- rug (2.9 x 2.1 m)
W, H = 1024, 740
rug = Image.new("RGB", (W, H), (214, 190, 150))
d = ImageDraw.Draw(rug)
cols = {"terra": (176, 82, 52), "cream": (226, 205, 168), "teal": (38, 96, 104), "mustard": (214, 156, 48), "navy": (36, 44, 78), "rose": (190, 110, 110)}
d.rectangle([0, 0, W, H], fill=cols["cream"])
d.rectangle([18, 18, W - 18, H - 18], fill=cols["navy"])
d.rectangle([40, 40, W - 40, H - 40], fill=cols["cream"])
d.rectangle([58, 58, W - 58, H - 58], fill=cols["terra"])
# diamond lattice
cx, cy = W // 2, H // 2
for k in range(6):
    s = 300 - k * 48
    col = [cols["cream"], cols["teal"], cols["mustard"], cols["navy"], cols["rose"], cols["cream"]][k]
    d.polygon([(cx, cy - s * 0.78), (cx + s, cy), (cx, cy + s * 0.78), (cx - s, cy)], fill=col)
# corner triangles and dots
for (x0, y0, sx, sy) in ((58, 58, 1, 1), (W - 58, 58, -1, 1), (58, H - 58, 1, -1), (W - 58, H - 58, -1, -1)):
    d.polygon([(x0, y0), (x0 + sx * 150, y0), (x0, y0 + sy * 150)], fill=cols["mustard"])
    d.polygon([(x0, y0), (x0 + sx * 90, y0), (x0, y0 + sy * 90)], fill=cols["navy"])
for i in range(8):
    for (x, y) in ((110 + i * 115, 100), (110 + i * 115, H - 100)):
        d.ellipse([x - 9, y - 9, x + 9, y + 9], fill=cols["cream"])
a = np.asarray(rug).astype(np.float32) / 255
n = gn(H, W, 0.7, 0.7, 12)
a *= (0.86 + 0.18 * n)[:, :, None]
save(a, "rug.png")

# ---------------------------------------------------------------- posters
def grad(h, w, top, bot):
    t = np.linspace(0, 1, h)[:, None, None]
    return np.repeat(np.array(top)[None, None, :] * (1 - t) + np.array(bot)[None, None, :] * t, w, axis=1)

def finish(img, name, grain=0.04):
    a = np.asarray(img).astype(np.float32) / 255
    a += (np.random.RandomState(1).rand(*a.shape[:2]) - 0.5)[:, :, None] * grain
    save(a, name)

# 0: retro sun + grid (portrait)
w, h = 360, 512
a = (grad(h, w, (0.09, 0.05, 0.25), (0.95, 0.35, 0.5)) * 255).astype(np.uint8)
im = Image.fromarray(a); d = ImageDraw.Draw(im)
d.ellipse([w / 2 - 110, 130, w / 2 + 110, 350], fill=(255, 205, 90))
for k in range(7):
    y = 250 + k * 18
    d.rectangle([0, y, w, y + 2 + k * 2], fill=tuple(int(v) for v in a[y, 5]))
d.rectangle([0, 340, w, h], fill=(30, 12, 60))
for k in range(-8, 9):
    d.line([(w / 2 + k * 8, 340), (w / 2 + k * 70, h)], fill=(255, 90, 170), width=2)
for k in range(8):
    y = 340 + (k ** 1.8) * 4.6
    d.line([(0, y), (w, y)], fill=(255, 90, 170), width=2)
finish(im, "poster0.png")

# 1: layered mountains + moon (landscape-ish square-ish)
w, h = 512, 360
a = (grad(h, w, (0.05, 0.08, 0.2), (0.3, 0.28, 0.5)) * 255).astype(np.uint8)
im = Image.fromarray(a); d = ImageDraw.Draw(im)
d.ellipse([340, 50, 420, 130], fill=(250, 240, 210))
rs = random.Random(3)
for layer, (col, base, amp) in enumerate([((70, 60, 120), 230, 70), ((50, 44, 98), 270, 60), ((30, 28, 70), 310, 50), ((16, 16, 40), 345, 30)]):
    pts = [(0, h)]
    for x in range(0, w + 16, 16):
        pts.append((x, base - amp * abs(math.sin(x * 0.015 + layer * 1.7)) - rs.uniform(0, 14)))
    pts.append((w, h)); d.polygon(pts, fill=col)
finish(im, "poster1.png")

# 2: sound-wave bars (square)
w = h = 400
a = (grad(h, w, (0.98, 0.78, 0.35), (0.92, 0.4, 0.38)) * 255).astype(np.uint8)
im = Image.fromarray(a); d = ImageDraw.Draw(im)
rs = random.Random(5)
for k in range(21):
    amp = 20 + 130 * abs(math.sin(k * 0.45 + 0.6)) * rs.uniform(0.7, 1.0)
    x = 28 + k * 17
    d.rounded_rectangle([x, 200 - amp, x + 11, 200 + amp], radius=5, fill=(40, 18, 50))
d.ellipse([w / 2 - 14, 18, w / 2 + 14, 46], fill=(40, 18, 50))
finish(im, "poster2.png")

# 3: boho arches (portrait)
w, h = 360, 512
im = Image.new("RGB", (w, h), (232, 214, 184)); d = ImageDraw.Draw(im)
for (x0, x1, col) in ((30, 330, (176, 82, 52)), (70, 290, (214, 156, 48)), (110, 250, (38, 96, 104)), (150, 210, (36, 44, 78))):
    d.rounded_rectangle([x0, 80 + (x0 - 30) * 0.5, x1, 470], radius=(x1 - x0) // 2, fill=col)
    d.rectangle([x0, 300, x1, 470], fill=col)
d.ellipse([245, 40, 300, 95], fill=(250, 240, 210))
finish(im, "poster3.png")

# 4: dusk skyline (wide 1.2 x 0.45)
w, h = 768, 288
a = (grad(h, w, (0.2, 0.12, 0.38), (0.98, 0.55, 0.4)) * 255).astype(np.uint8)
im = Image.fromarray(a); d = ImageDraw.Draw(im)
rs = random.Random(9); x = 0
while x < w:
    bw = rs.randint(28, 70); bh = rs.randint(60, 190)
    d.rectangle([x, h - bh, x + bw, h], fill=(22, 14, 44))
    for wy in range(h - bh + 8, h - 6, 14):
        for wx in range(x + 5, x + bw - 6, 10):
            if rs.random() < 0.45: d.rectangle([wx, wy, wx + 4, wy + 6], fill=(255, 214, 120))
    x += bw + rs.randint(0, 6)
finish(im, "poster4.png")

# ---------------------------------------------------------------- night skyline (window view)
W, H = 2048, 1024
sky = grad(H, W, (0.02, 0.03, 0.10), (0.22, 0.17, 0.38)).copy()
rs = np.random.RandomState(2)
for _ in range(320):
    x, y = rs.randint(0, W), rs.randint(0, int(H * 0.6))
    sky[y, x] = np.array([1, 1, 0.9]) * rs.uniform(0.5, 1)
im = Image.fromarray((np.clip(sky, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6))
d = ImageDraw.Draw(im, "RGBA")
d.ellipse([1500, 200, 1590, 290], fill=(250, 244, 220, 255))      # moon
glow = Image.new("RGBA", (W, H), (0, 0, 0, 0)); gd = ImageDraw.Draw(glow)
gd.ellipse([1420, 120, 1670, 370], fill=(200, 210, 255, 70))
im = Image.alpha_composite(im.convert("RGBA"), glow.filter(ImageFilter.GaussianBlur(28)))
d = ImageDraw.Draw(im, "RGBA")
rr = random.Random(4); x = 0                                     # far buildings
while x < W:
    bw = rr.randint(40, 110); bh = rr.randint(150, 420)
    d.rectangle([x, H - bh, x + bw, H], fill=(34, 30, 64, 255))
    x += bw + rr.randint(0, 10)
bok = Image.new("RGBA", (W, H), (0, 0, 0, 0)); bd = ImageDraw.Draw(bok)
for _ in range(70):                                              # bokeh city lights
    x, y, r = rr.randint(0, W), rr.randint(int(H * 0.6), H - 10), rr.randint(5, 14)
    c = rr.choice([(255, 190, 90, 150), (255, 120, 170, 130), (120, 220, 255, 120), (255, 230, 150, 140)])
    bd.ellipse([x - r, y - r, x + r, y + r], fill=c)
im = Image.alpha_composite(im, bok.filter(ImageFilter.GaussianBlur(3)))
im.convert("RGB").save(OUT + "skyline_far.png"); print("wrote skyline_far.png")

near = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(near)
rr = random.Random(7); x = -20
while x < W:
    bw = rr.randint(120, 260); bh = rr.randint(220, 560)
    d.rectangle([x, H - bh, x + bw, H], fill=(12, 10, 26, 255))
    if rr.random() < 0.4: d.rectangle([x + bw // 2 - 3, H - bh - 34, x + bw // 2 + 3, H - bh], fill=(12, 10, 26, 255))
    for wy in range(H - bh + 16, H - 14, 30):
        for wx in range(x + 12, x + bw - 14, 22):
            if rr.random() < 0.38:
                c = rr.choice([(255, 206, 120, 255), (255, 170, 90, 255), (140, 210, 255, 255), (255, 230, 170, 255)])
                d.rectangle([wx, wy, wx + 10, wy + 14], fill=c)
    x += bw + rr.randint(-10, 20)
near.save(OUT + "skyline_near.png"); print("wrote skyline_near.png")

# ---------------------------------------------------------------- rain streaks (tileable, additive)
h, w = 512, 256
rs = np.random.RandomState(21)
img = np.zeros((h, w))
for _ in range(60):
    x = rs.randint(0, w); y = rs.randint(0, h); ln = rs.randint(30, 110); a0 = rs.uniform(0.25, 0.9)
    for t in range(ln):
        yy = (y + t) % h
        img[yy, x % w] = max(img[yy, x % w], a0 * (1 - t / ln) ** 1.4)
        img[yy, (x + 1) % w] = max(img[yy, (x + 1) % w], a0 * 0.4 * (1 - t / ln))
img = np.asarray(Image.fromarray((img * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))).astype(np.float32) / 255
rgba = np.zeros((h, w, 4)); rgba[:, :, 0] = 0.75; rgba[:, :, 1] = 0.85; rgba[:, :, 2] = 1.0; rgba[:, :, 3] = np.clip(img * 1.4, 0, 1)
save(rgba, "rain.png", "RGBA")

# ---------------------------------------------------------------- vinyl grooves + labels
S = 1024
yy, xx = np.mgrid[0:S, 0:S].astype(np.float32); c = S / 2
r = np.sqrt((xx - c) ** 2 + (yy - c) ** 2) / c            # 0..1 maps to 0..0.15 m
ang = np.arctan2(yy - c, xx - c)
groove = 0.5 + 0.5 * np.sin(r * 2 * math.pi * 210) * (1 + 0.2 * np.sin(r * 31))
sheen = np.clip(np.cos(2 * ang) , 0, 1) ** 6 * 0.5 + np.clip(np.cos(2 * (ang - 0.9)), 0, 1) ** 10 * 0.25
lum = 0.04 + 0.03 * groove + sheen * 0.13 * (0.5 + 0.5 * groove)
lum[(r < 0.36) | (r > 0.985)] = 0.03
bands = (np.abs(r - 0.52) < 0.004) | (np.abs(r - 0.7) < 0.003) | (np.abs(r - 0.86) < 0.003)
lum[bands] = 0.015
img = np.stack([lum * 0.96, lum * 1.0, lum * 1.12], -1)
save(img, "vinyl.png")
S = 256; yy, xx = np.mgrid[0:S, 0:S].astype(np.float32); c = S / 2
r = np.sqrt((xx - c) ** 2 + (yy - c) ** 2) / c; ang = np.arctan2(yy - c, xx - c)
lab = np.zeros((S, S, 3)); lab[:] = (0.86, 0.38, 0.28)
lab[(r > 0.78)] = (0.12, 0.09, 0.14)
lab[(r > 0.6) & (r < 0.66)] = (0.95, 0.85, 0.6)
star = (r < 0.42) & (np.abs(np.sin(ang * 5 + 0.3)) * 0.42 + 0.2 > r)
lab[star] = (0.98, 0.86, 0.5)
lab[r < 0.07] = (0.1, 0.08, 0.1)
save(lab, "vinyl_label.png")

# ---------------------------------------------------------------- sprites
S = 128; yy, xx = np.mgrid[0:S, 0:S].astype(np.float32); r = np.sqrt((xx - S / 2) ** 2 + (yy - S / 2) ** 2) / (S / 2)
a = np.clip(1 - r, 0, 1) ** 2.2
rgba = np.zeros((S, S, 4)); rgba[:, :, :3] = 1; rgba[:, :, 3] = a
save(rgba, "halo.png", "RGBA")
S = 64; yy, xx = np.mgrid[0:S, 0:S].astype(np.float32); r = np.sqrt((xx - S / 2) ** 2 + (yy - S / 2) ** 2) / (S / 2)
n = gn(S, S, 3, 3, 33)
a = np.clip(1 - r, 0, 1) ** 1.5 * (0.5 + 0.5 * n)
rgba = np.zeros((S, S, 4)); rgba[:, :, :3] = 1; rgba[:, :, 3] = a
save(rgba, "puff.png", "RGBA")
# light shaft: soft vertical gradient (alpha)
h, w = 256, 64
t = np.linspace(0, 1, h)[:, None]; u = np.abs(np.linspace(-1, 1, w))[None, :]
a = (1 - t) ** 1.2 * np.clip(1 - u, 0, 1) ** 1.5
rgba = np.zeros((h, w, 4)); rgba[:, :, :3] = 1; rgba[:, :, 3] = a
save(rgba, "shaft.png", "RGBA")
