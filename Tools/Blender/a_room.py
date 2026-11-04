import sys, math, random
sys.path.insert(0, "/private/tmp/claude-501/-Users-nmoff/5c2b5fe1-51bf-4bb6-bec5-edba5506ae6a/scratchpad/bl")
from lib2 import *
from mathutils import Vector

# preview colours (Unity builder owns the real look)
for n, c in {"M_Floor0": (.35,.2,.1), "M_Floor1": (.3,.17,.09), "M_Floor2": (.4,.24,.12), "M_Floor3": (.32,.19,.1),
             "M_FloorGap": (.03,.02,.01), "M_Wall": (.12,.18,.28), "M_WallPanel": (.22,.12,.07), "M_Trim": (.3,.17,.1),
             "M_Ceiling": (.08,.09,.14), "M_Beam": (.2,.11,.06), "M_WindowFrame": (.75,.7,.6), "M_WoodLight": (.6,.42,.25),
             "M_Brass": (.8,.6,.2), "M_Curtain": (.35,.1,.3), "M_Glow": (1,.7,.3)}.items():
    M(n, c)

X0, X1, Z0, Z1, H = -3.0, 3.0, -2.0, 3.0, 3.1
WIN_X, WIN_Y0, WIN_Y1 = 1.3, 1.0, 2.5

def quad_dir(A, mat, verts, want, uvs):
    v = [Vector(p) for p in verts]
    n = (v[1] - v[0]).cross(v[2] - v[1])
    if n.dot(Vector(want)) < 0:
        verts = list(reversed(verts)); uvs = list(reversed(uvs))
    A._emit(verts, [tuple(range(len(verts)))], mat, (0, 0, 0), (0, 0, 0), False, uvs=[uvs])

def build_shell():
    A = Asset("RoomShell")
    rnd = random.Random(11)
    # ---- floor: planks run along Z, staggered butt joints
    quad_dir(A, "M_FloorGap", [(X0, -0.004, Z0), (X0, -0.004, Z1), (X1, -0.004, Z1), (X1, -0.004, Z0)], (0, 1, 0), [(0, 0)] * 4)
    for i in range(30):
        x0 = X0 + i * 0.2 + 0.002; x1 = x0 + 0.196
        cut = rnd.uniform(-0.8, 2.3)
        for za, zb in ((Z0, cut - 0.002), (cut + 0.002, Z1)):
            m = "M_Floor%d" % rnd.randrange(4)
            v0 = rnd.random(); ln = (zb - za) / 1.2
            quad_dir(A, m, [(x0, 0, za), (x0, 0, zb), (x1, 0, zb), (x1, 0, za)], (0, 1, 0),
                     [(0, v0), (0, v0 + ln), (1, v0 + ln), (1, v0)])
    # ---- ceiling
    quad_dir(A, "M_Ceiling", [(X0, H, Z0), (X0, H, Z1), (X1, H, Z1), (X1, H, Z0)], (0, -1, 0), [(0, 0), (0, 2.5), (3, 2.5), (3, 0)])
    for z in (-1.0, 0.55, 2.1):
        A.box((6.0, 0.16, 0.2), (0, H - 0.08, z), "M_Beam", bevel=0.01)
    # ---- walls: (start, end) along the wall's own direction, inward normal
    walls = {
        "back":  ((X0, Z1), (X1, Z1), (0, 0, -1)),
        "left":  ((X0, Z0), (X0, Z1), (1, 0, 0)),
        "right": ((X1, Z1), (X1, Z0), (-1, 0, 0)),
        "front": ((X1, Z0), (X0, Z0), (0, 0, 1)),
    }
    def wpt(w, t, y):  # point on wall plane at distance t along wall
        p0, p1, n = walls[w]
        L = math.hypot(p1[0] - p0[0], p1[1] - p0[1])
        d = ((p1[0] - p0[0]) / L, (p1[1] - p0[1]) / L)
        return (p0[0] + d[0] * t, y, p0[1] + d[1] * t)
    def wlen(w):
        p0, p1, n = walls[w]
        return math.hypot(p1[0] - p0[0], p1[1] - p0[1])
    def wrect(w, t0, t1, y0, y1, mat, uvs=2.0):
        p0, p1, n = walls[w]
        quad_dir(A, mat, [wpt(w, t0, y0), wpt(w, t0, y1), wpt(w, t1, y1), wpt(w, t1, y0)], n,
                 [(t0 / uvs, y0 / uvs), (t0 / uvs, y1 / uvs), (t1 / uvs, y1 / uvs), (t1 / uvs, y0 / uvs)])
    def wbox(w, tc, length, y0, y1, depth, mat, bevel=0.0):
        p0, p1, n = walls[w]
        c = wpt(w, tc, (y0 + y1) / 2)
        c = (c[0] + n[0] * depth / 2, c[1], c[2] + n[2] * depth / 2)
        sx, sz = (length, depth) if n[2] != 0 else (depth, length)
        A.box((sx, y1 - y0, sz), c, mat, bevel=bevel)
    for w in walls:
        L = wlen(w)
        if w == "back":
            tw0, tw1 = 3.0 - WIN_X, 3.0 + WIN_X
            wrect(w, 0, tw0, 1.0, H, "M_Wall"); wrect(w, tw1, L, 1.0, H, "M_Wall"); wrect(w, tw0, tw1, WIN_Y1, H, "M_Wall")
            wrect(w, 0, L, 0, 1.0, "M_WallPanel", 1.0)
            # window reveal (opening is 0.2 deep)
            for (tx, nrm) in ((tw0, (1, 0, 0)), (tw1, (-1, 0, 0))):
                x = tx - 3.0
                quad_dir(A, "M_Wall", [(x, WIN_Y0, Z1), (x, WIN_Y1, Z1), (x, WIN_Y1, Z1 + 0.2), (x, WIN_Y0, Z1 + 0.2)], nrm, [(0, 0)] * 4)
            quad_dir(A, "M_Wall", [(-WIN_X, WIN_Y1, Z1), (WIN_X, WIN_Y1, Z1), (WIN_X, WIN_Y1, Z1 + 0.2), (-WIN_X, WIN_Y1, Z1 + 0.2)], (0, -1, 0), [(0, 0)] * 4)
        else:
            wrect(w, 0, L, 1.0, H, "M_Wall"); wrect(w, 0, L, 0, 1.0, "M_WallPanel", 1.0)
        # board-and-batten wainscot
        n_b = int(L / 0.45)
        for k in range(n_b + 1):
            tc = 0.1 + k * (L - 0.2) / n_b
            wbox(w, tc, 0.07, 0.11, 0.94, 0.018, "M_Trim", 0.003)
        wbox(w, L / 2, L, 0.0, 0.12, 0.03, "M_Trim", 0.004)          # baseboard
        wbox(w, L / 2, L, 0.94, 1.0, 0.045, "M_Trim", 0.006)          # chair rail
        wbox(w, L / 2, L, H - 0.09, H, 0.06, "M_Trim", 0.006)         # crown
    return A

def build_window():
    A = Asset("Window")
    z = Z1 + 0.1
    # frame
    for sx in (-1, 1):
        A.box((0.08, 1.5, 0.14), (sx * (WIN_X - 0.04), 1.75, z), "M_WindowFrame", bevel=0.006)
    A.box((2.6, 0.08, 0.14), (0, WIN_Y1 - 0.04, z), "M_WindowFrame", bevel=0.006)
    A.box((2.6, 0.06, 0.14), (0, WIN_Y0 + 0.03, z), "M_WindowFrame", bevel=0.006)
    A.box((0.05, 1.42, 0.07), (0, 1.75, z + 0.01), "M_WindowFrame", bevel=0.004)
    for yy in (1.5, 2.0):
        A.box((2.52, 0.04, 0.07), (0, yy, z + 0.01), "M_WindowFrame", bevel=0.004)
    # deep sill + apron
    A.box((3.0, 0.05, 0.32), (0, 1.025, Z1 - 0.1), "M_WoodLight", bevel=0.008)
    A.box((2.7, 0.1, 0.03), (0, 0.95, Z1 - 0.02), "M_WoodLight", bevel=0.005)
    # curtain rod + finials + rings
    rod_y, rod_z = 2.68, Z1 - 0.34
    A.cyl(0.013, 3.7, (0, rod_y, rod_z), "M_Brass", rot=(0, 0, 90), seg=12)
    for sx in (-1, 1):
        A.sphere(0.03, (sx * 1.88, rod_y, rod_z), "M_Brass", seg=10, rings=7)
        A.box((0.03, 0.03, 0.34), (sx * 1.0, rod_y, Z1 - 0.17), "M_Brass")   # brackets
    # curtains: two floor-length drapes with pleats
    def drape(x0, x1):
        cols, rows = 22, 8
        verts, faces, uvs = [], [], []
        w = x1 - x0
        for r in range(rows + 1):
            t = r / rows
            y = rod_y - 0.04 - t * (rod_y - 0.1)
            for c in range(cols + 1):
                u = c / cols
                x = x0 + u * w
                zz = rod_z - 0.03 - 0.045 * (0.6 + 0.4 * t) * (0.5 + 0.5 * math.sin(u * math.pi * 2 * 5)) 
                verts.append((x, y, zz))
        for r in range(rows):
            for c in range(cols):
                a = r * (cols + 1) + c
                faces.append((a, a + 1, a + cols + 2, a + cols + 1))
        # winding: viewer at -z; check by building and let orientation be fixed by quad test below
        return verts, faces
    for (x0, x1) in ((-1.75, -0.95), (0.95, 1.75)):
        verts, faces = drape(x0, x1)
        # pick winding so RH normal faces -z
        v = [Vector(verts[i]) for i in faces[0][:3]]
        if (v[1] - v[0]).cross(v[2] - v[1]).z > 0:
            faces = [tuple(reversed(f)) for f in faces]
        A.raw(verts, faces, "M_Curtain", smooth=True, uv=1.0)
        # back side so it never disappears when viewed from behind
        A.raw(verts, [tuple(reversed(f)) for f in faces], "M_Curtain", smooth=True, uv=1.0)
        for k in range(8):
            A.torus(0.02, 0.004, (x0 + 0.06 + k * (x1 - x0 - 0.12) / 7, rod_y, rod_z), "M_Brass", rot=(0, 0, 90), seg=10, rseg=5)
    return A

if __name__ == "__main__":
    reset()
    sh, win = build_shell(), build_window()
    if "--preview" in sys.argv:
        preview([sh, win], "/tmp/prev_room.png", (0, 1.55, -1.9), (0, 1.5, 3), lens=18)
    else:
        export(sh, "RoomShell.fbx"); export(win, "Window.fbx")
