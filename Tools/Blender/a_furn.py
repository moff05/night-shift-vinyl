import sys, math, random
sys.path.insert(0, "/private/tmp/claude-501/-Users-nmoff/5c2b5fe1-51bf-4bb6-bec5-edba5506ae6a/scratchpad/bl")
from lib2 import *

PREV = {"M_DeskTop": (.55,.38,.22), "M_WoodDark": (.18,.1,.06), "M_Wood": (.36,.2,.1), "M_WoodLight": (.6,.42,.25),
 "M_Brass": (.8,.6,.2), "M_DeskMat": (.1,.12,.2), "M_Black": (.04,.04,.05), "M_Mustard": (.75,.5,.15), "M_Cream": (.85,.8,.7),
 "M_Teal": (.1,.4,.42), "M_Blanket": (.6,.25,.2), "M_Fur": (.85,.45,.15), "M_FurWhite": (.9,.88,.85), "M_Pink": (.9,.5,.55),
 "M_Leaf": (.12,.4,.16), "M_LeafDark": (.06,.25,.12), "M_Pot": (.7,.35,.2), "M_PotWhite": (.85,.85,.8), "M_Soil": (.08,.05,.03),
 "M_Speaker": (.3,.17,.09), "M_Cone": (.12,.12,.13), "M_Grille": (.08,.08,.09), "M_Steel": (.7,.7,.75),
 "M_Rug": (.7,.4,.25), "M_RugEdge": (.5,.28,.18), "M_Glow": (1,.75,.35), "M_Pendant": (.1,.35,.33),
 "M_Ceramic": (.9,.85,.75), "M_Coffee": (.15,.08,.04), "M_Paper": (.9,.88,.8), "M_Notebook": (.65,.3,.2),
 "M_FrameBlack": (.04,.04,.05), "M_FrameWood": (.45,.28,.14), "M_FrameMat": (.9,.88,.82), "M_Art": (.6,.5,.7),
 "M_Door": (.28,.15,.08), "M_Neutral": (.5,.5,.5)}
for i, c in enumerate([(.5,.15,.15),(.15,.3,.45),(.7,.55,.15),(.2,.4,.25),(.45,.25,.4),(.8,.4,.2),(.25,.25,.3),(.6,.6,.5)]):
    M("M_Book%d" % i, c)
for i, c in enumerate([(.9,.4,.5),(.3,.7,.8),(.95,.8,.3),(.5,.4,.8),(.3,.75,.5),(.9,.55,.25)]):
    M("M_Sleeve%d" % i, c)
for i, c in enumerate([(.8,.3,.3),(.3,.5,.8),(.9,.75,.2)]):
    M("M_Cassette%d" % i, c)
for n, c in PREV.items(): M(n, c)

# ------------------------------------------------------------------ desk
def build_desk():
    A = Asset("Desk")
    A.box((2.06, 0.05, 0.94), (0, 0.925, 0), "M_DeskTop", bevel=0.012, uv=0.8)
    # pedestal (left): carcass + 3 drawers
    A.box((0.5, 0.86, 0.82), (-0.72, 0.43 - 0.01, 0), "M_Wood", bevel=0.006, uv=0.8)
    for k in range(3):
        y = 0.18 + k * 0.255
        A.box((0.44, 0.22, 0.02), (-0.72, y, -0.415), "M_WoodLight", bevel=0.006, uv=0.8)
        A.box((0.14, 0.014, 0.022), (-0.72, y + 0.05, -0.44), "M_Brass", bevel=0.004)
    # right side: two tapered legs + side stretcher + modesty panel
    for sz in (-0.38, 0.38):
        A.cyl(0.035, 0.9, (0.93, 0.45, sz), "M_Wood", r2=0.022, seg=14)
        A.cyl(0.035, 0.9, (0.08, 0.45, sz), "M_Wood", r2=0.022, seg=14)
    A.box((0.04, 0.07, 0.8), (0.93, 0.78, 0), "M_Wood", bevel=0.004)
    A.box((0.84, 0.42, 0.02), (0.5, 0.58, 0.4), "M_WoodDark", bevel=0.004)
    A.box((0.84, 0.05, 0.04), (0.5, 0.82, 0.36), "M_Wood", bevel=0.004)
    # under-desk shelf with a stack of records/boxes
    A.box((0.88, 0.025, 0.72), (0.5, 0.28, 0.0), "M_Wood", bevel=0.004, uv=0.8)
    A.box((0.32, 0.18, 0.3), (0.6, 0.38, 0.0), "M_Sleeve1", bevel=0.01)
    A.box((0.31, 0.16, 0.3), (0.6, 0.545, 0.0), "M_Sleeve3", bevel=0.01, rot=(0, 7, 0))
    return A

def build_deskmat():
    A = Asset("DeskMat")
    A.box((1.78, 0.004, 0.62), (0, 0.002, 0), "M_DeskMat", bevel=0.0015, seg=1, uv=0.5)
    return A

# ------------------------------------------------------------------ couch
def build_couch():
    A = Asset("Couch")
    W, D = 1.95, 0.9
    A.box((W, 0.18, D), (0, 0.24, 0), "M_Wood", bevel=0.02)                    # frame
    for sx in (-1, 1):
        for sz in (-1, 1):
            A.cyl(0.035, 0.16, (sx * (W / 2 - 0.1), 0.08, sz * (D / 2 - 0.1)), "M_Wood", r2=0.022, seg=12)
    for sx in (-1, 1):                                                          # arms
        A.box((0.2, 0.34, D), (sx * (W / 2 - 0.1), 0.5, 0), "M_Mustard", bevel=0.06, seg=3, smooth=True, uv=0.6)
    for k in range(-1, 2):                                                      # seat cushions
        A.box((0.51, 0.16, 0.66), (k * 0.52, 0.41, -0.1), "M_Mustard", bevel=0.05, seg=3, smooth=True, uv=0.6)
    A.box((W - 0.38, 0.5, 0.2), (0, 0.55, 0.32), "M_Mustard", bevel=0.06, seg=3, smooth=True, uv=0.6)   # back frame
    for k in range(-1, 2):                                                      # back cushions
        A.box((0.52, 0.42, 0.16), (k * 0.52, 0.66, 0.2), "M_Mustard", bevel=0.05, seg=3, smooth=True, rot=(-12, 0, 0), uv=0.6)
    A.box((0.4, 0.4, 0.12), (-0.55, 0.62, 0.0), "M_Teal", bevel=0.05, seg=3, smooth=True, rot=(-18, 14, 6), uv=0.5)
    A.box((0.36, 0.36, 0.11), (0.62, 0.6, 0.03), "M_Cream", bevel=0.05, seg=3, smooth=True, rot=(-16, -18, -8), uv=0.5)
    # throw blanket over the right arm and cushion
    A.box((0.3, 0.03, 0.5), (0.78, 0.5, -0.12), "M_Blanket", bevel=0.012, rot=(0, 12, -4), uv=0.5)
    A.box((0.03, 0.34, 0.5), (0.935, 0.43, -0.12), "M_Blanket", bevel=0.012, rot=(0, 12, 0), uv=0.5)
    return A

def build_cat():
    A = Asset("Cat")
    # loaf: body, haunches, chest, head with ears, tail curling round the front
    A.sphere(0.16, (0, 0.11, 0), "M_Fur", scale=(1.0, 0.78, 1.55), seg=18, rings=12)
    A.sphere(0.1, (0.0, 0.07, 0.17), "M_Fur", scale=(1.3, 0.9, 1.0))
    A.sphere(0.085, (0.0, 0.1, -0.2), "M_FurWhite", scale=(1.1, 0.9, 0.8))             # chest
    A.sphere(0.09, (0.0, 0.14, -0.27), "M_Fur", scale=(1.0, 0.95, 0.9))                 # head
    for sx in (-1, 1):
        A.cyl(0.035, 0.05, (sx * 0.05, 0.215, -0.26), "M_Fur", r2=0.004, rot=(0, 0, sx * -14), seg=8)
        A.cyl(0.02, 0.01, (sx * 0.04, 0.12, -0.34), "M_Black", rot=(90, 0, 0), seg=8)      # closed eyes (tiny discs)
    A.sphere(0.014, (0, 0.115, -0.352), "M_Pink", seg=8, rings=6)                       # nose
    for k in range(4):                                                                    # tabby stripes
        A.box((0.2, 0.01, 0.025), (0, 0.222 - (k - 1.5) ** 2 * 0.004, -0.02 + k * 0.08), "M_Wood", rot=(0, 0, 0), bevel=0.004)
    pts = [(0.12, 0.05, 0.1), (0.2, 0.05, -0.02), (0.2, 0.05, -0.16), (0.12, 0.05, -0.26)]
    A.tube(pts, 0.028, "M_Fur", seg=8, rscale=lambda t: 1.0 - 0.45 * t)
    return A

# ------------------------------------------------------------------ bookshelf + records
def build_bookshelf():
    A = Asset("Bookshelf")
    W, D, H = 1.1, 0.3, 2.0
    rnd = random.Random(5)
    for sx in (-1, 1):
        A.box((0.035, H, D), (sx * (W / 2 - 0.0175), H / 2, 0), "M_Wood", bevel=0.004, uv=0.8)
    A.box((W, H, 0.015), (0, H / 2, 0.14), "M_WoodDark")
    levels = [0.06, 0.4, 0.74, 1.08, 1.42, 1.76, 1.985]
    for y in levels:
        A.box((W, 0.03, D), (0, y, 0), "M_Wood", bevel=0.004, uv=0.8)
    inner = W - 0.07
    for i in range(len(levels) - 2):
        y0 = levels[i] + 0.015
        ch = levels[i + 1] - y0 - 0.015
        x = -inner / 2 + 0.01
        mode = rnd.random()
        end = inner / 2 - 0.01
        gap_at = rnd.uniform(0.35, 0.65) * inner
        while x < end - 0.03:
            if abs((x + inner / 2) - gap_at) < 0.12 and rnd.random() < 0.9:
                x += 0.24      # space for a plant / ornament
                continue
            w = rnd.uniform(0.022, 0.05)
            h = rnd.uniform(0.7, 0.97) * ch
            d = rnd.uniform(0.17, 0.24)
            mat = "M_Book%d" % rnd.randrange(8)
            lean = rnd.uniform(-3, 3) if rnd.random() < 0.1 else 0
            A.box((w, h, d), (x + w / 2, y0 + h / 2, 0.04 - (0.24 - d) / 2 + 0.01), mat, rot=(0, 0, lean), uv=0.4)
            x += w + 0.002
    # a horizontal stack on the lowest shelf
    A.box((0.28, 0.035, 0.2), (0.2, 0.095, -0.02), "M_Book1", bevel=0.003)
    A.box((0.26, 0.03, 0.19), (0.2, 0.13, -0.02), "M_Book5", bevel=0.003, rot=(0, 8, 0))
    return A

def build_vinylrack():
    A = Asset("VinylRack")
    W, D, H = 1.3, 0.38, 0.86
    A.box((W, 0.03, D), (0, H - 0.015, 0), "M_WoodLight", bevel=0.005, uv=0.8)
    A.box((W, 0.04, D), (0, 0.07, 0), "M_WoodLight", bevel=0.005, uv=0.8)
    for k in range(4):
        A.box((0.03, H - 0.06, D), (-W / 2 + 0.015 + k * (W - 0.03) / 3, H / 2, 0), "M_WoodLight", bevel=0.004, uv=0.8)
    A.box((W, 0.025, D), (0, H / 2 - 0.02, 0), "M_WoodLight", bevel=0.004, uv=0.8)
    A.box((W, H, 0.015), (0, H / 2, D / 2 - 0.0075), "M_WoodDark")
    rnd = random.Random(9)
    cw = (W - 0.03) / 3 - 0.03
    for row, y0 in enumerate((0.09, H / 2 - 0.007)):
        for col in range(3):
            cx = -W / 2 + 0.03 + col * (W - 0.03) / 3 + cw / 2 + 0.015
            n = rnd.randrange(14, 20)
            for k in range(n):
                t = cw * (k / n) - cw / 2 + 0.01
                lean = -6 if k > n - 4 else 0
                A.box((0.006, 0.31, 0.31), (cx + t, y0 + 0.16, 0.0), "M_Sleeve%d" % rnd.randrange(6), rot=(0, 0, lean), uv=0.3)
    return A

# ------------------------------------------------------------------ speaker
def build_speaker():
    A = Asset("Speaker")
    W, D, H = 0.32, 0.3, 0.95
    A.box((W, H - 0.12, D), (0, 0.12 + (H - 0.12) / 2, 0), "M_Speaker", bevel=0.012, uv=0.8)
    A.box((W + 0.02, 0.03, D + 0.02), (0, 0.135, 0), "M_WoodDark", bevel=0.006)
    for sx in (-1, 1):
        for sz in (-1, 1):
            A.cyl(0.016, 0.12, (sx * 0.11, 0.06, sz * 0.1), "M_Black", r2=0.008, seg=8)
    A.box((W - 0.04, H - 0.2, 0.01), (0, 0.55, -D / 2 - 0.001), "M_Grille")             # baffle
    A.cyl(0.034, 0.02, (0, 0.82, -D / 2 - 0.01), "M_Steel", rot=(90, 0, 0), seg=18)     # tweeter ring
    A.sphere(0.024, (0, 0.82, -D / 2 - 0.015), "M_Cone", scale=(1, 1, 0.5))
    A.cyl(0.006, 0.0, (0, 0.4, -D / 2 - 0.01), "M_Black")                                # placeholder to keep slot order stable
    A.torus(0.115, 0.009, (0, 0.5, -D / 2 - 0.004), "M_Black", rot=(90, 0, 0), seg=32, rseg=6)  # woofer surround
    A.box((0.1, 0.02, 0.01), (0, 0.18, -D / 2 - 0.006), "M_Brass", bevel=0.003)         # badge
    wf = A.node("Woofer", (0, 0.5, -D / 2 - 0.008))
    wf.cyl(0.11, 0.05, (0, 0.5, -D / 2 + 0.012), "M_Cone", r2=0.04, rot=(90, 0, 0), seg=28)
    wf.sphere(0.04, (0, 0.5, -D / 2 - 0.006), "M_Steel", scale=(1, 1, 0.55), seg=16)
    return A

# ------------------------------------------------------------------ rug
def build_rug():
    A = Asset("Rug")
    W, D, r, h = 2.9, 2.1, 0.14, 0.014
    pts = []
    for cx, cz, a0 in ((W/2 - r, D/2 - r, 0), (-W/2 + r, D/2 - r, 90), (-W/2 + r, -D/2 + r, 180), (W/2 - r, -D/2 + r, 270)):
        for k in range(7):
            a = math.radians(a0 + 90 * k / 6)
            pts.append((cx + r * math.cos(a), cz + r * math.sin(a)))
    top = [(x, h, z) for x, z in pts]
    A.ngon(top, "M_Rug", (0, 1, 0), uvfn=lambda v: (v.x / W + 0.5, v.z / D + 0.5))
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        quad = [(pts[i][0], 0, pts[i][1]), (pts[j][0], 0, pts[j][1]), (pts[j][0], h, pts[j][1]), (pts[i][0], h, pts[i][1])]
        mid = ((pts[i][0] + pts[j][0]) / 2, (pts[i][1] + pts[j][1]) / 2)
        A.ngon(quad, "M_RugEdge", (mid[0], 0, mid[1]), uvfn=lambda v: (0, 0))
    return A

# ------------------------------------------------------------------ lamps
def build_desklamp():
    A = Asset("DeskLamp")
    A.cyl(0.085, 0.02, (0, 0.01, 0), "M_Brass", seg=28, bevel=0.004)
    A.cyl(0.03, 0.03, (0, 0.035, 0), "M_Black", seg=18)
    # arms
    A.tube([(0, 0.04, 0), (0.02, 0.17, 0.0), (0.06, 0.3, 0.0)], 0.007, "M_Brass", seg=8)
    A.sphere(0.016, (0.06, 0.3, 0), "M_Brass", seg=10, rings=7)
    A.tube([(0.06, 0.3, 0), (0.14, 0.4, 0.04), (0.21, 0.43, 0.08)], 0.006, "M_Brass", seg=8)
    # head: dome shade pointing down/forward
    hx, hy, hz = 0.22, 0.42, 0.09
    A.cyl(0.075, 0.09, (hx, hy, hz), "M_Pendant", r2=0.02, rot=(55, 0, -25), seg=24, bevel=0.003)
    A.sphere(0.02, (hx + 0.012, hy - 0.04, hz + 0.045), "M_Glow", seg=10, rings=7)
    b = A.node("Bulb", (hx + 0.02, hy - 0.06, hz + 0.06))
    b.sphere(0.001, (hx + 0.02, hy - 0.06, hz + 0.06), "M_Glow", seg=4, rings=3)
    return A

def build_pendant():
    A = Asset("Pendant")
    A.cyl(0.04, 0.02, (0, -0.01, 0), "M_Black", seg=14)
    A.cyl(0.004, 1.0, (0, -0.52, 0), "M_Black", seg=6)
    A.cyl(0.035, 0.05, (0, -1.04, 0), "M_Black", r2=0.045, seg=14)
    A.cyl(0.22, 0.2, (0, -1.15, 0), "M_Pendant", r2=0.045, seg=28, bevel=0.004)
    A.disc(0.215, (0, -1.249, 0), "M_Glow", seg=28, down=True)
    A.sphere(0.055, (0, -1.27, 0), "M_Glow", seg=14, rings=9)
    A.node("Bulb", (0, -1.28, 0))
    return A

def build_door():
    A = Asset("Door")
    A.box((1.04, 2.12, 0.05), (0, 1.06, 0.01), "M_Trim", bevel=0.006)
    A.box((0.92, 2.04, 0.04), (0, 1.02, -0.02), "M_Door", bevel=0.01, uv=0.8)
    for (y, hh) in ((1.5, 0.7), (0.5, 0.7)):
        A.box((0.64, hh, 0.012), (0, y, -0.045), "M_WoodDark", bevel=0.004)
        A.box((0.58, hh - 0.08, 0.012), (0, y, -0.052), "M_Door", bevel=0.004)
    A.sphere(0.03, (-0.34, 1.0, -0.08), "M_Brass", seg=14, rings=9)
    A.cyl(0.01, 0.05, (-0.34, 1.0, -0.055), "M_Brass", rot=(90, 0, 0), seg=8)
    return A

# ------------------------------------------------------------------ frames
def build_frame(name, w, h, fmat):
    A = Asset(name)
    b, d = 0.035, 0.03
    A.box((w, b, d), (0, h / 2 - b / 2, 0), fmat, bevel=0.004)
    A.box((w, b, d), (0, -h / 2 + b / 2, 0), fmat, bevel=0.004)
    A.box((b, h - 2 * b, d), (-w / 2 + b / 2, 0, 0), fmat, bevel=0.004)
    A.box((b, h - 2 * b, d), (w / 2 - b / 2, 0, 0), fmat, bevel=0.004)
    A.box((w - 2 * b, h - 2 * b, 0.006), (0, 0, 0.008), "M_FrameMat")                  # backing / mat
    m = 0.05
    A.plane(w - 2 * b - 2 * m, h - 2 * b - 2 * m, (0, 0, -0.0105), "M_Art")
    return A

if __name__ == "__main__":
    reset()
    B = {"Desk": build_desk, "DeskMat": build_deskmat, "Couch": build_couch, "Cat": build_cat, "Bookshelf": build_bookshelf,
         "VinylRack": build_vinylrack, "Speaker": build_speaker, "Rug": build_rug, "DeskLamp": build_desklamp,
         "Pendant": build_pendant, "Door": build_door}
    if "--sheet" in sys.argv:
        items = [("Speaker", (-1.5, 0, 0)), ("DeskLamp", (-0.5, 0, 0)), ("Pendant", (0.3, 1.4, 0)), ("Door", (1.3, 0, 0)), ("Cat", (2.1, 0, 0)), ("VinylRack", (3.2, 0, 0))]
        arts = []
        for n, o in items:
            a = B[n](); a.origin = Vector(o); arts.append(a)
        preview(arts, "/tmp/prev_sheet.png", (0.9, 1.1, -9.5), (0.9, 0.8, 0), lens=45, res=(1500, 560))
    elif "--preview" in sys.argv:
        which = sys.argv[sys.argv.index("--preview") + 1]
        a = B[which]()
        eye = tuple(float(x) for x in sys.argv[sys.argv.index("--preview") + 2].split(","))
        look = tuple(float(x) for x in sys.argv[sys.argv.index("--preview") + 3].split(","))
        preview([a], "/tmp/prev_%s.png" % which, eye, look, lens=35, res=(960, 720))
    else:
        for n, f in B.items():
            export(f(), n + ".fbx")
        specs = [("Frame_P_Black", .5, .7, "M_FrameBlack"), ("Frame_L_Wood", .7, .5, "M_FrameWood"), ("Frame_S_Black", .4, .4, "M_FrameBlack"),
                 ("Frame_W_Black", 1.2, .45, "M_FrameBlack"), ("Frame_P_Wood", .5, .7, "M_FrameWood")]
        for n, w, h, m in specs:
            export(build_frame(n, w, h, m), n + ".fbx")
