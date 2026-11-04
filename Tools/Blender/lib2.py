
"""Unity-space modelling helpers. Everything is authored with Unity axes (x right, y up, z forward, metres),
then mapped to Blender space (-x, -z, y) at build time so the FBX imports 1:1 into Unity."""
import bpy, bmesh, math, random
from mathutils import Vector, Matrix

OUT = "/Users/nmoff/chillhop-crate/Assets/Models/Chillhop/"

def C(v):
    return Vector((-v[0], -v[2], v[1]))

def rotm(rot):
    rx, ry, rz = [math.radians(a) for a in rot]
    Rx = Matrix(((1, 0, 0), (0, math.cos(rx), -math.sin(rx)), (0, math.sin(rx), math.cos(rx))))
    Ry = Matrix(((math.cos(ry), 0, math.sin(ry)), (0, 1, 0), (-math.sin(ry), 0, math.cos(ry))))
    Rz = Matrix(((math.cos(rz), -math.sin(rz), 0), (math.sin(rz), math.cos(rz), 0), (0, 0, 1)))
    return Ry @ Rx @ Rz

_mats = {}
def M(name, rgb=(0.5, 0.5, 0.5)):
    m = _mats.get(name)
    if m is None:
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        m.diffuse_color = (*rgb, 1)
        _mats[name] = m
    return m

def reset():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)

class Asset:
    def __init__(self, name, origin=(0, 0, 0)):
        self.name = name
        self.origin = Vector(origin)
        self.bm = bmesh.new()
        self.slots = []
        self.children = []

    def node(self, name, origin):
        a = Asset(name, origin)
        self.children.append(a)
        return a

    # ---- internals
    def _slot(self, mat):
        n = mat if isinstance(mat, str) else mat.name
        if n not in self.slots:
            self.slots.append(n)
        return self.slots.index(n)

    def _emit(self, verts, faces, mat, pos, rot, smooth=False, uvfn=None, uvs=None):
        """verts: local unity-space coords; faces: index tuples; uvfn(face_normal_unity, vert_unity)->uv or uvs per-face list"""
        R = rotm(rot)
        P = Vector(pos) - self.origin
        bm = self.bm
        slot = self._slot(mat)
        uvl = bm.loops.layers.uv.verify()
        bv = [bm.verts.new(R @ Vector(v) + P) for v in verts]
        for fi, f in enumerate(faces):
            try:
                face = bm.faces.new([bv[i] for i in f])
            except ValueError:
                continue
            face.material_index = slot
            face.smooth = smooth
            if uvs is not None:
                for l, uv in zip(face.loops, uvs[fi]):
                    l[uvl].uv = uv
            elif uvfn is not None:
                n = face.normal
                for l in face.loops:
                    l[uvl].uv = uvfn(n, l.vert.co)
        return bv

    def _bm_to_parts(self, bm):
        vs = [tuple(v.co) for v in bm.verts]
        bm.verts.ensure_lookup_table()
        fs = [tuple(v.index for v in f.verts) for f in bm.faces]
        return vs, fs

    def _local_bm_emit(self, bm, mat, pos, rot, smooth, uv):
        vs, fs = self._bm_to_parts(bm)
        bm.free()
        s = uv
        def uvfn(n, c):
            ax = max(range(3), key=lambda i: abs(n[i]))
            if ax == 0: return (c.z / s, c.y / s)
            if ax == 1: return (c.x / s, c.z / s)
            return (c.x / s, c.y / s)
        return self._emit(vs, fs, mat, pos, rot, smooth, uvfn=uvfn)

    # ---- primitives (unity space)
    def box(self, size, pos, mat, rot=(0, 0, 0), bevel=0.0, seg=2, uv=1.0, smooth=False):
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co.x *= size[0]; v.co.y *= size[1]; v.co.z *= size[2]
        if bevel > 0:
            bmesh.ops.bevel(bm, geom=list(bm.edges), offset=min(bevel, min(size) * 0.45), segments=seg, affect='EDGES')
        return self._local_bm_emit(bm, mat, pos, rot, smooth, uv)

    def cyl(self, r, h, pos, mat, rot=(0, 0, 0), seg=24, r2=None, bevel=0.0, smooth=True, uv=1.0, caps=True):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=caps, cap_tris=False, segments=seg, radius1=r, radius2=(r if r2 is None else r2), depth=h)
        # blender cone is along Z; swap to unity Y
        for v in bm.verts:
            v.co = Vector((v.co.x, v.co.z, -v.co.y))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        if bevel > 0:
            ring = [e for e in bm.edges if len(e.link_faces) == 2 and abs(e.verts[0].co.y - e.verts[1].co.y) < 1e-6]
            bmesh.ops.bevel(bm, geom=ring, offset=bevel, segments=2, affect='EDGES')
        return self._local_bm_emit(bm, mat, pos, rot, smooth, uv)

    def sphere(self, r, pos, mat, scale=(1, 1, 1), rot=(0, 0, 0), seg=16, rings=10, smooth=True, uv=1.0):
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=r)
        for v in bm.verts:
            v.co = Vector((v.co.x * scale[0], v.co.z * scale[1], -v.co.y * scale[2]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        return self._local_bm_emit(bm, mat, pos, rot, smooth, uv)

    def torus(self, R, r, pos, mat, rot=(0, 0, 0), seg=40, rseg=8, arc=360.0, smooth=True):
        """Ring around local Y axis; arc<360 gives a partial arc starting at +X sweeping toward +Z."""
        full = arc >= 359.9
        n = seg if full else seg + 1
        verts, faces = [], []
        for i in range(n):
            a = math.radians(arc) * i / seg
            for j in range(rseg):
                b = 2 * math.pi * j / rseg
                rr = R + r * math.cos(b)
                verts.append((rr * math.cos(a), r * math.sin(b), rr * math.sin(a)))
        cols = seg if full else seg
        for i in range(cols):
            i2 = (i + 1) % n
            for j in range(rseg):
                j2 = (j + 1) % rseg
                faces.append((i * rseg + j, i * rseg + j2, i2 * rseg + j2, i2 * rseg + j))
        return self._emit(verts, faces, mat, pos, rot, smooth, uvfn=lambda n_, c: (c.x, c.z))

    def tube(self, pts, r, mat, closed=False, seg=6, smooth=True, rscale=None):
        """Round wire along a polyline of unity-space points."""
        pts = [Vector(p) for p in pts]
        N = len(pts)
        verts, faces = [], []
        prev_n = None
        for i, p in enumerate(pts):
            if closed:
                t = (pts[(i + 1) % N] - pts[(i - 1) % N]).normalized()
            else:
                t = (pts[min(i + 1, N - 1)] - pts[max(i - 1, 0)]).normalized()
            if prev_n is None:
                ref = Vector((0, 1, 0)) if abs(t.y) < 0.9 else Vector((1, 0, 0))
                nrm = t.cross(ref).normalized()
            else:
                nrm = (prev_n - t * prev_n.dot(t)).normalized()
            prev_n = nrm
            bn = t.cross(nrm).normalized()
            rr = r * (rscale(i / max(1, N - 1)) if rscale else 1.0)
            for k in range(seg):
                a = 2 * math.pi * k / seg
                verts.append(tuple(p + nrm * math.cos(a) * rr + bn * math.sin(a) * rr))
        rng = N if closed else N - 1
        for i in range(rng):
            i2 = (i + 1) % N
            for k in range(seg):
                k2 = (k + 1) % seg
                faces.append((i * seg + k, i * seg + k2, i2 * seg + k2, i2 * seg + k))
        if not closed:
            faces.append(tuple(reversed(range(seg))))
            faces.append(tuple(range((N - 1) * seg, N * seg)))
        bv = self._emit(verts, faces, mat, (0, 0, 0), (0, 0, 0), smooth, uvfn=lambda n_, c: (c.x, c.z))
        return bv

    def poly(self, pts2d, thick, pos, mat, rot=(0, 0, 0), uv=1.0, bevel=0.0):
        """Extruded 2D polygon in local XY, thickness along Z (centred)."""
        bm = bmesh.new()
        v0 = [bm.verts.new((a, b, -thick / 2)) for a, b in pts2d]
        f = bm.faces.new(v0)
        ext = bmesh.ops.extrude_face_region(bm, geom=[f])
        vs = [e for e in ext['geom'] if isinstance(e, bmesh.types.BMVert)]
        bmesh.ops.translate(bm, verts=vs, vec=(0, 0, thick))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        if bevel > 0:
            bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=1, affect='EDGES')
        return self._local_bm_emit(bm, mat, pos, rot, False, uv)

    def plane(self, w, h, pos, mat, rot=(0, 0, 0)):
        """Single quad facing -Z (toward a viewer at -Z), UV 0-1 with (0,0) bottom-left as seen by that viewer."""
        verts = [(-w / 2, -h / 2, 0), (-w / 2, h / 2, 0), (w / 2, h / 2, 0), (w / 2, -h / 2, 0)]
        faces = [(0, 1, 2, 3)]
        uvs = [[(0, 0), (0, 1), (1, 1), (1, 0)]]
        # make sure normal faces -Z
        return self._emit(verts, faces, mat, pos, rot, False, uvs=uvs)

    def disc(self, r, pos, mat, seg=48, rot=(0, 0, 0), inner=0.0, down=False):
        """Flat disc/annulus facing +Y (or -Y), circular UV 0-1."""
        verts, faces, uvs = [], [], []
        for i in range(seg):
            a = 2 * math.pi * i / seg
            verts.append((math.cos(a) * r, 0, math.sin(a) * r))
        if inner > 0:
            for i in range(seg):
                a = 2 * math.pi * i / seg
                verts.append((math.cos(a) * inner, 0, math.sin(a) * inner))
            for i in range(seg):
                j = (i + 1) % seg
                f = (i, seg + i, seg + j, j)
                faces.append(f if not down else tuple(reversed(f)))
        else:
            f = tuple(range(seg))
            faces.append(f if down else tuple(reversed(f)))
        for f in faces:
            uvs.append([(0.5 + verts[i][0] / (2 * r), 0.5 + verts[i][2] / (2 * r)) for i in f])
        return self._emit(verts, faces, mat, pos, rot, False, uvs=uvs)

    def raw(self, verts, faces, mat, pos=(0, 0, 0), rot=(0, 0, 0), smooth=False, uv=1.0):
        s = uv
        def uvfn(n, c):
            ax = max(range(3), key=lambda i: abs(n[i]))
            if ax == 0: return (c.z / s, c.y / s)
            if ax == 1: return (c.x / s, c.z / s)
            return (c.x / s, c.y / s)
        return self._emit(verts, faces, mat, pos, rot, smooth, uvfn=uvfn)


    def ngon(self, pts, mat, want, uvfn=None):
        """Single convex polygon with winding auto-fixed to face `want`."""
        v = [Vector(p) for p in pts]
        n = Vector((0, 0, 0))
        for i in range(len(v)):
            n += v[i].cross(v[(i + 1) % len(v)])
        idx = list(range(len(v)))
        if n.dot(Vector(want)) < 0:
            idx.reverse()
        uvs = None
        if uvfn:
            uvs = [[uvfn(v[i]) for i in idx]]
        self._emit([tuple(p) for p in v], [tuple(idx)], mat, (0, 0, 0), (0, 0, 0), False, uvs=uvs)

    def leaf(self, L, W, pos, rot, mat, fold=0.22, droop=0.25, rows=7, cols=5):
        """Pointed leaf blade lying along local +Z, widening then tapering; double sided."""
        verts, faces = [], []
        for r in range(rows + 1):
            v = r / rows
            wv = W * 0.5 * (math.sin(math.pi * (v ** 0.75)) ** 0.9)
            for c in range(cols + 1):
                u = c / cols * 2 - 1
                x = u * wv
                y = fold * abs(u) * wv - droop * L * v * v * 0.5
                verts.append((x, y, v * L))
        for r in range(rows):
            for c in range(cols):
                a = r * (cols + 1) + c
                faces.append((a, a + cols + 1, a + cols + 2, a + 1))
        self._emit(verts, faces, mat, pos, rot, True, uvfn=lambda n, p: (p.x, p.z))
        self._emit(verts, [tuple(reversed(f)) for f in faces], mat, pos, rot, True, uvfn=lambda n, p: (p.x, p.z))

    # ---- finishing
    def build(self):
        objs = []
        for a in [self] + self.children:
            bm = a.bm
            for v in bm.verts:
                v.co = C(v.co)
            bmesh.ops.reverse_faces(bm, faces=bm.faces)   # mirrored mapping flips winding
            me = bpy.data.meshes.new(a.name)
            bm.to_mesh(me)
            bm.free()
            for n in a.slots:
                me.materials.append(M(n))
            o = bpy.data.objects.new(a.name, me)
            bpy.context.scene.collection.objects.link(o)
            o.location = C(a.origin)
            objs.append(o)
        return objs

def export(asset, filename):
    objs = asset.build()
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=OUT + filename, use_selection=True, object_types={'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, axis_forward='-Z', axis_up='Y',
        mesh_smooth_type='FACE', add_leaf_bones=False, path_mode='STRIP', bake_anim=False)
    tris = sum(len(o.data.polygons) for o in objs)
    print("EXPORTED %s polys=%d slots=%s" % (filename, tris, asset.slots))
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)


def preview(assets, png, eye, look, lens=35, res=(1280, 720), bg=(0.05, 0.05, 0.08), sun=True):
    """Workbench render of one or more Assets from a unity-space eye/look."""
    objs = []
    for a in assets:
        objs += a.build()
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'
    sc.display.shading.color_type = 'MATERIAL'
    sc.display.shading.show_cavity = False
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = False
    if sc.world is None:
        sc.world = bpy.data.worlds.new("W")
    sc.world.color = bg
    cam = bpy.data.objects.get("PrevCam")
    if cam is None:
        cd = bpy.data.cameras.new("PrevCam")
        cam = bpy.data.objects.new("PrevCam", cd)
        sc.collection.objects.link(cam)
    cam.data.lens = lens
    cam.data.clip_start = 0.02
    cam.location = C(eye)
    d = C(look) - C(eye)
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    sc.camera = cam
    sc.render.filepath = png
    bpy.ops.render.render(write_still=True)
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
