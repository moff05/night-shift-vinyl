import sys, math, random
sys.path.insert(0, "/private/tmp/claude-501/-Users-nmoff/5c2b5fe1-51bf-4bb6-bec5-edba5506ae6a/scratchpad/bl")
from lib2 import *
for n, c in {"M_Ceramic": (.9,.85,.75), "M_Coffee": (.15,.08,.04), "M_Black": (.04,.04,.05), "M_Steel": (.75,.75,.8), "M_Mustard": (.75,.5,.15),
             "M_Leaf": (.12,.4,.16), "M_LeafDark": (.06,.25,.12), "M_Snake": (.1,.3,.15), "M_Pot": (.7,.35,.2), "M_PotWhite": (.85,.85,.8),
             "M_Soil": (.08,.05,.03), "M_Wood": (.36,.2,.1), "M_Paper": (.9,.88,.8), "M_Notebook": (.65,.3,.2), "M_Brass": (.8,.6,.2),
             "M_NeonPink": (1,.2,.6), "M_NeonCyan": (.2,.9,1), "M_NeonYellow": (1,.85,.3), "M_Wire": (.03,.03,.03),
             "M_Bulb0": (1,.8,.45), "M_Bulb1": (1,.55,.7), "M_Glow": (1,.75,.35), "M_ClockFace": (.9,.88,.8), "M_WoodLight": (.6,.42,.25),
             "M_Pad": (.1,.1,.12), "M_Cactus": (.2,.45,.3), "M_Pink": (.9,.5,.55), "M_Teal": (.1,.4,.42), "M_FurWhite": (.9,.9,.85)}.items():
    M(n, c)
for i, c in enumerate([(.5,.15,.15),(.15,.3,.45),(.7,.55,.15),(.2,.4,.25),(.45,.25,.4),(.8,.4,.2),(.25,.25,.3),(.6,.6,.5)]):
    M("M_Book%d" % i, c)
for i, c in enumerate([(.8,.3,.3),(.3,.5,.8),(.9,.75,.2)]):
    M("M_Cassette%d" % i, c)

def build_mug():
    A = Asset("Mug")
    A.cyl(0.04, 0.09, (0, 0.045, 0), "M_Ceramic", r2=0.037, seg=24, bevel=0.003)
    A.cyl(0.034, 0.002, (0, 0.082, 0), "M_Coffee", seg=24)
    A.torus(0.022, 0.006, (0.043, 0.048, 0), "M_Ceramic", rot=(90, 0, 0), arc=360, seg=20, rseg=6)
    A.cyl(0.04, 0.007, (0, 0.0035, 0), "M_Mustard", seg=24)
    return A

def build_headphones():
    A = Asset("Headphones")
    A.cyl(0.07, 0.012, (0, 0.006, 0), "M_Black", seg=24, bevel=0.003)
    A.cyl(0.011, 0.22, (0, 0.12, 0), "M_Steel", seg=10)
    A.sphere(0.02, (0, 0.235, 0), "M_Steel", seg=10, rings=7)
    # headband draped over the stand top
    A.torus(0.088, 0.011, (0, 0.2, 0), "M_Black", rot=(0, 0, 90), arc=190, seg=28, rseg=8)
    for sx in (-1, 1):
        A.cyl(0.05, 0.034, (sx * 0.088, 0.115, 0), "M_Black", rot=(0, 0, 90), seg=20, bevel=0.004)
        A.cyl(0.043, 0.02, (sx * 0.074, 0.115, 0), "M_Mustard", rot=(0, 0, 90), seg=20)
    return A

def build_cassettes():
    A = Asset("Cassettes")
    for i, (m, rot, off) in enumerate((("M_Cassette0", 0, (0, 0)), ("M_Cassette1", 11, (0.01, 0)), ("M_Cassette2", -8, (-0.005, 0)))):
        y = 0.009 + i * 0.0185
        A.box((0.108, 0.0165, 0.07), (off[0], y, off[1]), m, bevel=0.002, rot=(0, rot, 0), uv=0.2)
        A.box((0.08, 0.0005, 0.03), (off[0], y + 0.0085, off[1] + 0.005), "M_Paper", rot=(0, rot, 0))
    return A

def build_notebook():
    A = Asset("Notebook")
    A.box((0.15, 0.012, 0.21), (0, 0.006, 0), "M_Notebook", bevel=0.002, uv=0.4)
    A.box((0.14, 0.008, 0.2), (0.003, 0.006, 0), "M_Paper", bevel=0.001)
    A.box((0.15, 0.003, 0.21), (0, 0.0135, 0), "M_Notebook", bevel=0.001)
    A.cyl(0.004, 0.14, (0.04, 0.019, -0.01), "M_Mustard", rot=(0, 0, 90), seg=8)
    return A

def build_succulent():
    A = Asset("Succulent")
    A.cyl(0.05, 0.06, (0, 0.03, 0), "M_PotWhite", r2=0.04, seg=18, bevel=0.003)
    A.cyl(0.045, 0.004, (0, 0.058, 0), "M_Soil", seg=14)
    for ring, (n, tilt, ln) in enumerate(((10, 62, 0.055), (7, 38, 0.05), (5, 14, 0.04))):
        for k in range(n):
            a = 360 * k / n + ring * 18
            A.leaf(ln, 0.028, (0, 0.062 + ring * 0.008, 0), (tilt - 90 + 90, a, 0), "M_Cactus", fold=0.4, droop=-0.1)
    return A

def build_cactus():
    A = Asset("Cactus")
    A.cyl(0.04, 0.05, (0, 0.025, 0), "M_Pot", r2=0.033, seg=16, bevel=0.002)
    A.cyl(0.03, 0.12, (0, 0.11, 0), "M_Cactus", seg=14, smooth=True)
    A.sphere(0.03, (0, 0.17, 0), "M_Cactus", seg=14, rings=8)
    A.tube([(0.025, 0.1, 0), (0.07, 0.1, 0), (0.075, 0.16, 0)], 0.011, "M_Cactus", seg=8)
    A.sphere(0.011, (0.075, 0.162, 0), "M_Cactus", seg=8, rings=6)
    A.sphere(0.008, (0.0, 0.17, -0.027), "M_NeonPink", seg=6, rings=4)
    return A

def _stem(A, base, tip, r, mat):
    mid = ((base[0] + tip[0]) / 2, max(base[1], tip[1]) + 0.08, (base[2] + tip[2]) / 2)
    pts = []
    for i in range(7):
        t = i / 6
        pts.append(tuple((1 - t) ** 2 * b + 2 * (1 - t) * t * m + t * t * q for b, m, q in zip(base, mid, tip)))
    A.tube(pts, r, mat, seg=6, rscale=lambda t: 1.0 - 0.3 * t)

def build_monstera():
    A = Asset("Monstera")
    rnd = random.Random(3)
    A.cyl(0.19, 0.34, (0, 0.17, 0), "M_Pot", r2=0.14, seg=24, bevel=0.006, uv=0.5)
    A.cyl(0.2, 0.025, (0, 0.34, 0), "M_Pot", seg=24, bevel=0.004)
    A.cyl(0.17, 0.01, (0, 0.335, 0), "M_Soil", seg=20)
    for k in range(11):
        yaw = 360 * k / 11 + rnd.uniform(-12, 12)
        r = rnd.uniform(0.18, 0.5)
        h = rnd.uniform(0.55, 1.15)
        ay = math.radians(yaw)
        tip = (math.sin(ay) * r, 0.34 + h, math.cos(ay) * r)
        _stem(A, (math.sin(ay) * 0.03, 0.34, math.cos(ay) * 0.03), tip, 0.007, "M_LeafDark")
        L = rnd.uniform(0.3, 0.42)
        A.leaf(L, L * 0.85, tip, (rnd.uniform(15, 55), yaw + rnd.uniform(-25, 25), 0), "M_Leaf", fold=0.3, droop=0.5)
    return A

def build_snake():
    A = Asset("SnakePlant")
    rnd = random.Random(8)
    A.cyl(0.14, 0.26, (0, 0.13, 0), "M_PotWhite", r2=0.11, seg=20, bevel=0.005)
    A.cyl(0.125, 0.008, (0, 0.255, 0), "M_Soil", seg=18)
    for k in range(9):
        a = 360 * k / 9 + rnd.uniform(-10, 10)
        tilt = rnd.uniform(0, 12)
        h = rnd.uniform(0.4, 0.8)
        w = rnd.uniform(0.035, 0.05)
        ay = math.radians(a)
        px, pz = math.sin(ay) * rnd.uniform(0.02, 0.07), math.cos(ay) * rnd.uniform(0.02, 0.07)
        A.poly([(-w, 0), (w, 0), (w * 1.05, h * 0.55), (0, h), (-w * 1.05, h * 0.55)], 0.012, (px, 0.25, pz), "M_Snake", rot=(-tilt, a, 0), bevel=0.002)
    return A

def build_pothos():
    A = Asset("Pothos")
    rnd = random.Random(4)
    A.cyl(0.03, 0.02, (0, -0.01, 0), "M_Black", seg=10)
    for k in range(3):
        a = 120 * k
        ay = math.radians(a)
        A.tube([(0, -0.02, 0), (math.sin(ay) * 0.06, -0.4, math.cos(ay) * 0.06), (math.sin(ay) * 0.1, -0.78, math.cos(ay) * 0.1)], 0.003, "M_Wire", seg=5)
    A.cyl(0.12, 0.1, (0, -0.92, 0), "M_PotWhite", r2=0.08, seg=22, bevel=0.004)
    A.cyl(0.11, 0.008, (0, -0.87, 0), "M_Soil", seg=18)
    for v in range(7):
        a = 360 * v / 7 + rnd.uniform(-20, 20)
        ay = math.radians(a)
        ln = rnd.uniform(0.5, 1.0)
        pts = []
        for i in range(8):
            t = i / 7
            pts.append((math.sin(ay) * (0.1 + 0.06 * t + 0.03 * math.sin(t * 5)), -0.87 - ln * t, math.cos(ay) * (0.1 + 0.06 * t + 0.03 * math.sin(t * 5))))
        A.tube(pts, 0.0028, "M_LeafDark", seg=4)
        for i in range(1, 8):
            p = pts[i]
            A.leaf(0.075, 0.06, p, (rnd.uniform(40, 95), a + rnd.uniform(-60, 60), 0), "M_Leaf", fold=0.3, droop=0.3, rows=4, cols=3)
    return A

def build_neon():
    A = Asset("NeonSign")
    # crescent moon (outer circle R, inner circle r offset +x so it opens to the right)
    R, r, d = 0.32, 0.27, 0.1
    xi = (R * R - r * r + d * d) / (2 * d); yi = math.sqrt(R * R - xi * xi)
    t0 = math.atan2(yi, xi)
    pts = []
    for i in range(41):
        a = t0 + (2 * math.pi - 2 * t0) * i / 40
        pts.append((R * math.cos(a) - 0.08, R * math.sin(a), 0))
    b0 = math.atan2(-yi, xi - d); b1 = math.atan2(yi, xi - d)
    b0 = b0 % (2 * math.pi)
    for i in range(1, 40):
        a = b0 - (b0 - b1) * i / 40
        pts.append((d + r * math.cos(a) - 0.08, r * math.sin(a), 0))
    A.tube(pts, 0.0085, "M_NeonPink", closed=True, seg=8)
    def star(cx, cy, s, mat):
        p = []
        for i in range(11):
            ang = math.radians(90 + i * 36)
            rr = s if i % 2 == 0 else s * 0.42
            p.append((cx + rr * math.cos(ang), cy + rr * math.sin(ang), 0))
        A.tube(p, 0.007, mat, closed=False, seg=6)
    star(0.25, 0.22, 0.09, "M_NeonYellow"); star(0.4, -0.02, 0.06, "M_NeonYellow"); star(0.17, -0.2, 0.045, "M_NeonYellow")
    # music note
    nx, ny = 0.66, -0.12
    th = [(nx + 0.055 * math.cos(2 * math.pi * i / 20), ny + 0.04 * math.sin(2 * math.pi * i / 20), 0) for i in range(21)]
    A.tube(th, 0.007, "M_NeonCyan", closed=False, seg=6)
    A.tube([(nx + 0.055, ny, 0), (nx + 0.055, ny + 0.32, 0), (nx + 0.16, ny + 0.26, 0), (nx + 0.16, ny + 0.2, 0)], 0.007, "M_NeonCyan", seg=6)
    # standoffs
    for (x, y) in ((-0.38, 0.26), (0.3, 0.3), (0.0, -0.3), (0.72, 0.1)):
        A.cyl(0.008, 0.04, (x, y, 0.02), "M_Black", rot=(90, 0, 0), seg=8)
    return A

def build_strings():
    A = Asset("StringLights")
    rnd = random.Random(2)
    count = [0]
    def swag(p0, p1, sag):
        n = 14
        pts = []
        for i in range(n + 1):
            t = i / n
            p = [p0[k] + (p1[k] - p0[k]) * t for k in range(3)]
            p[1] -= sag * 4 * t * (1 - t)
            pts.append(tuple(p))
        A.tube(pts, 0.0022, "M_Wire", seg=4)
        L = math.dist(p0, p1)
        m = max(2, int(L / 0.2))
        for i in range(1, m):
            t = i / m
            p = [p0[k] + (p1[k] - p0[k]) * t for k in range(3)]
            p[1] -= sag * 4 * t * (1 - t) + 0.025
            A.sphere(0.017, tuple(p), "M_Bulb%d" % (count[0] % 2), seg=8, rings=6)
            count[0] += 1
        A.cyl(0.004, 0.03, (p1[0], p1[1] + 0.0, p1[2]), "M_Black", seg=5)
    y = 2.93
    xs = [-3.0, -1.8, -0.6, 0.6, 1.8, 3.0]
    for a, b in zip(xs[:-1], xs[1:]):                  # back wall
        swag((a, y, 2.94), (b, y, 2.94), 0.16 + rnd.uniform(0, 0.06))
    zs = [2.94, 1.55, 0.2, -1.15, -1.95]
    for a, b in zip(zs[:-1], zs[1:]):                  # side walls
        swag((-2.94, y, a), (-2.94, y, b), 0.16 + rnd.uniform(0, 0.06))
        swag((2.94, y, a), (2.94, y, b), 0.16 + rnd.uniform(0, 0.06))
    return A

def build_clock():
    A = Asset("Clock")
    A.cyl(0.17, 0.03, (0, 0, 0), "M_Brass", rot=(90, 0, 0), seg=40, bevel=0.003)
    A.cyl(0.15, 0.006, (0, 0, -0.016), "M_ClockFace", rot=(90, 0, 0), seg=40)
    for i in range(12):
        a = math.radians(30 * i)
        A.box((0.008, 0.02, 0.004), (math.sin(a) * 0.125, math.cos(a) * 0.125, -0.02), "M_Black", rot=(0, 0, -30 * i))
    A.box((0.008, 0.1, 0.003), (0.0, 0.04, -0.023), "M_Black", rot=(0, 0, 20))
    A.box((0.006, 0.07, 0.003), (-0.03, -0.01, -0.025), "M_Black", rot=(0, 0, -70))
    return A

def build_shelf():
    A = Asset("WallShelf")
    A.box((1.0, 0.035, 0.22), (0, 0, 0), "M_WoodLight", bevel=0.005, uv=0.6)
    for sx in (-1, 1):
        A.box((0.02, 0.1, 0.17), (sx * 0.35, -0.065, 0.025), "M_Black", bevel=0.003)
    # items: books, candle, tiny plant, figurine
    A.box((0.16, 0.025, 0.12), (-0.34, 0.03, 0), "M_Book2", bevel=0.002)
    A.box((0.14, 0.022, 0.11), (-0.34, 0.053, 0), "M_Book5", bevel=0.002, rot=(0, 10, 0))
    A.cyl(0.04, 0.05, (-0.15, 0.04, 0), "M_Ceramic", seg=16, bevel=0.003)
    A.cyl(0.03, 0.002, (-0.15, 0.066, 0), "M_Glow", seg=12)
    A.cyl(0.03, 0.05, (0.12, 0.04, 0.0), "M_Pot", r2=0.024, seg=12)
    for k in range(6):
        A.leaf(0.07, 0.03, (0.12, 0.062, 0), (50 + k * 4, 60 * k, 0), "M_Leaf", fold=0.3, droop=0.3, rows=4, cols=3)
    A.sphere(0.035, (0.3, 0.052, 0), "M_Teal", scale=(1, 1.1, 1), seg=12, rings=8)
    A.sphere(0.02, (0.3, 0.1, 0), "M_Teal", seg=10, rings=7)
    A.cyl(0.012, 0.03, (0.288, 0.125, 0), "M_Teal", r2=0.002, seg=6)
    A.cyl(0.012, 0.03, (0.312, 0.125, 0), "M_Teal", r2=0.002, seg=6)
    return A

def build_sidetable():
    A = Asset("SideTable")
    A.cyl(0.27, 0.03, (0, 0.52, 0), "M_WoodLight", seg=32, bevel=0.005, uv=0.6)
    for k in range(3):
        a = math.radians(120 * k + 30)
        A.cyl(0.02, 0.5, (math.sin(a) * 0.17, 0.25, math.cos(a) * 0.17), "M_Wood", r2=0.012, seg=10, rot=(math.cos(a) * 6, 0, -math.sin(a) * 6))
    A.cyl(0.1, 0.01, (0, 0.17, 0), "M_Wood", seg=24)
    return A

B = {"Mug": build_mug, "Headphones": build_headphones, "Cassettes": build_cassettes, "Notebook": build_notebook, "Succulent": build_succulent,
     "Cactus": build_cactus, "Monstera": build_monstera, "SnakePlant": build_snake, "Pothos": build_pothos, "NeonSign": build_neon,
     "StringLights": build_strings, "Clock": build_clock, "WallShelf": build_shelf, "SideTable": build_sidetable}

if __name__ == "__main__":
    reset()
    if "--sheet" in sys.argv:
        arts = []
        for i, n in enumerate(["Mug", "Headphones", "Cassettes", "Notebook", "Succulent", "Cactus", "Monstera", "SnakePlant", "SideTable", "Clock", "WallShelf", "NeonSign"]):
            a = B[n](); a.origin = Vector(((i % 6) * 0.9 - 2.2, 0 if i < 6 else 1.4 if n in ("Clock", "WallShelf", "NeonSign") else 0, 0 if i < 6 else 0)); 
            if i >= 6: a.origin = Vector(((i - 6) * 0.9 - 2.2, 0, 2.5))
            arts.append(a)
        preview(arts, "/tmp/prev_decor.png", (0, 1.4, -4.0), (0, 0.5, 1.2), lens=38, res=(1500, 760))
    else:
        for n, f in B.items():
            export(f(), n + ".fbx")
