"""Build Marika (#183): MPFB female base + mixamo_unity rig + Karen styling.

Run: D:\\Blender\\blender.exe --background --python Tools/Art/Build-Marika.py
Source of truth for the Marika model. MPFB output is CC0. The bob is the CC0
Quaternius SimpleParted hairstyle (see ArtSource/Characters/Quaternius),
auto-fitted to Marika's head; brows are thin angled liner planes, mouths are
swap variants like Pisti's, clothes are painted material zones on the body.
Every custom part stays a separate skinned object with its own material, so no
material-slot remap can wash the colors out. Mouths stay separate so the Unity
rig can swap them like Pisti.

Writes ArtSource/Characters/Marika.fbx (+ previews) and copies the FBX into
Assets/Game/Resources/Customers/Marika/ for Resources loading.
"""
import bpy
import sys
from mathutils import Vector, kdtree

REPO = None


def find_bone(bones, *keys):
    for b in bones:
        low = b.lower()
        if all(k in low for k in keys):
            return b
    return None


def main():
    sys.path.insert(0, r'C:\Users\Thomas\AppData\Roaming\Blender Foundation\Blender\5.2\extensions\user_default')
    try:
        bpy.ops.preferences.addon_enable(module='bl_ext.user_default.mpfb')
    except Exception as e:
        print('ENABLE_FAIL', e)
    from bl_ext.user_default.mpfb.services.humanservice import HumanService
    from pathlib import Path

    repo = Path(__file__).resolve().parents[2]
    art = repo / 'ArtSource' / 'Characters'
    dest = repo / 'Assets' / 'Game' / 'Resources' / 'Customers' / 'Marika'
    dest.mkdir(parents=True, exist_ok=True)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

    human = HumanService.create_human(macro_detail_dict={
        'gender': 1.0, 'age': 0.62, 'muscle': 0.3, 'weight': 0.55,
        'height': 0.45, 'proportions': 0.5, 'cupsize': 0.5, 'firmness': 0.4,
        'race': {'asian': 0.0, 'caucasian': 1.0, 'african': 0.0}})
    print('HUMAN', human.name if human is not None else None)
    rig = HumanService.add_builtin_rig(human, 'mixamo_unity')
    print('RIG', rig.name if rig is not None else None)
    bones = [b.name for b in rig.pose.bones]
    print('BONES', len(bones))

    # Uniform scale so the envelope matches the legacy capsule (~1.78 m top).
    zs = [v.co.z for v in human.data.vertices]
    tall = max(zs) - min(zs)
    scale = 1.78 / tall if tall > 0 else 1.0
    human.scale = (scale, scale, scale)
    rig.scale = (scale, scale, scale)
    bpy.context.view_layer.update()
    print('SCALE', round(scale, 4), 'height', round((max(v.co.z for v in human.data.vertices) - min(v.co.z for v in human.data.vertices)) * scale, 3))

    head_bone = rig.data.bones.get('mixamorig:Head')
    head_w = rig.matrix_world @ head_bone.head_local
    hips_z = rig.matrix_world @ rig.data.bones.get('mixamorig:Hips').head_local
    hips_z = hips_z.z
    print('HEAD', tuple(round(v, 3) for v in head_w), 'HIPS_Z', round(hips_z, 3))

    def principled(name, color):
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*color, 1)
        node = mat.node_tree.nodes.get('Principled BSDF')
        if node is not None:
            node.inputs['Base Color'].default_value = (*color, 1)
        return mat

    mats = {
        'Hair': principled('MarikaHair', (.09, .055, .05)),
        'Blouse': principled('MarikaBlouse', (.58, .22, .33)),
        'Skirt': principled('MarikaSkirt', (.16, .15, .17)),
        'Shoe': principled('MarikaShoe', (.10, .09, .10)),
        'Brow': principled('MarikaBrow', (.07, .05, .045)),
        'Liner': principled('MarikaLiner', (.10, .07, .06)),
        'Blush': principled('MarikaBlush', (.80, .42, .40)),
    }
    mouth_cols = {
        'MouthNeutral': (.55, .08, .12),
        'MouthSmile': (.55, .10, .14),
        'MouthGrim': (.42, .06, .10),
        'MouthPanic': (.06, .025, .03),
    }

    sc = bpy.context.scene
    joined = []

    def finish_custom(obj, mat):
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        joined.append(obj)

    # Hair and brows come from the Quaternius CC0 pack (see
    # ArtSource/Characters/Quaternius/): professionally modeled, smooth,
    # textured. The pack's "Origin at 0" variants are fitted to Marika's head
    # below and ride the skull rigidly, like the previous attachments.

    # No separate clothing geometry: the smooth MPFB body keeps its silhouette and
    # clothing is painted as material zones on the body mesh itself (see the
    # zone pass after helper removal). Only hair, brows and mouths are separate
    # rigid face attachments.

    # Face surface anchors: lips and brows sit just off the skin, placed from
    # measured body verts so they survive proportion changes.
    body_verts = human.data.vertices
    body_world = human.matrix_world

    # Face anchors from a center raycast profile (deterministic for these fixed
    # body params): lips = the forward bulge, brows = just above the nose dip.
    bpy.context.view_layer.update()
    _deps = bpy.context.evaluated_depsgraph_get()
    _eval = human.evaluated_get(_deps)
    _prof = []
    _z = 1.52
    while _z < 1.72:
        _hit, _loc, _n, _i = _eval.ray_cast(Vector((0.0, 1.0, _z)), Vector((0.0, -1.0, 0.0)))
        _prof.append((_z, _loc.y if _hit else -1.0))
        _z += 0.01
    _lips = max([p for p in _prof if 1.58 < p[0] < 1.66], key=lambda p: p[1])
    _browp = min([p for p in _prof if p[0] > _lips[0] + 0.04], key=lambda p: abs(p[0] - (_lips[0] + 0.07)))
    mouth_y = _lips[1] + 0.006
    mouth_z = _lips[0]
    brow_y = _browp[1] + 0.008
    brow_z = _browp[0]
    brow_x = 0.058
    print('LIPS', round(mouth_y, 3), round(mouth_z, 3), 'BROW', round(brow_y, 3), round(brow_z, 3))

    # Quaternius hair + brows (CC0, see ArtSource/Characters/Quaternius/),
    # fitted to Marika's head: uniform scale about the feet origin, then
    # translate onto the head center. Eyeliner planes finish the Karen look.
    import bmesh as _bmesh
    _qdir = art / 'Quaternius'

    def _relink(_owner_mats):
        for _m in _owner_mats:
            if _m is None or not _m.use_nodes:
                continue
            for _n in _m.node_tree.nodes:
                if _n.type != 'TEX_IMAGE':
                    continue
                _nm = _n.image.name if _n.image is not None else ''
                _stem = _nm.split('.')[0]
                for _cand in _qdir.glob('*.png'):
                    if _cand.stem == _stem and _cand.stat().st_size > 0:
                        try:
                            if _n.image is not None and not _n.image.has_data:
                                _n.image.filepath = str(_cand)
                                _n.image.reload()
                            else:
                                _n.image = bpy.data.images.load(str(_cand))
                            print('TEXLINK', _cand.name, 'px:', _n.image.has_data)
                        except Exception as _e:
                            print('TEXLINK_FAIL', _cand.name, _e)
                        break

    _fit_parts = []
    for _fp in (_qdir / 'Hair_SimpleParted.fbx',):
        bpy.ops.import_scene.fbx(filepath=str(_fp))
        for _o in list(bpy.context.selected_objects):
            if _o.type != 'MESH':
                continue
            _o.rotation_euler = (_o.rotation_euler.x, _o.rotation_euler.y, _o.rotation_euler.z + 3.14159265)
            _relink(_o.data.materials)
            for _mm in _o.data.materials:
                if _mm is not None and _mm.use_nodes:
                    _bsdf = _mm.node_tree.nodes.get('Principled BSDF')
                    if _bsdf is not None:
                        for _link in [l for l in _mm.node_tree.links if l.to_node == _bsdf and l.to_socket.name == 'Base Color']:
                            _mm.node_tree.links.remove(_link)
                        _tint = (0.16, 0.10, 0.07, 1) if 'brow' in _o.name.lower() or 'eye' in _o.name.lower() else (0.25, 0.14, 0.08, 1)
                        _bsdf.inputs['Base Color'].default_value = _tint
                        _mm.diffuse_color = _tint
            _o.name = 'MarikaQ' + _o.name.replace('.', '_')
            _o.location = (0.0, 0.0, 0.0)
            _fit_parts.append(_o)
    bpy.ops.object.select_all(action='DESELECT')
    for _o in _fit_parts:
        _o.select_set(True)
    bpy.context.view_layer.objects.active = _fit_parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    print('BAKED', [(o.name, tuple(round(v, 3) for v in o.location), tuple(round(v, 3) for v in o.scale)) for o in _fit_parts])
    _hw = [body_world @ v.co for v in body_verts
           if abs((body_world @ v.co).x - head_w.x) < 0.16
           and abs((body_world @ v.co).y - head_w.y) < 0.16
           and abs((body_world @ v.co).z - head_w.z) < 0.20]
    _skullw = 2 * max(abs(w.x - head_w.x) for w in _hw)
    _crown = max(w.z for w in _hw)

    def _place(_o, _sx, _sy, _sz, _tx, _ty, _tz):
        _xs = [float(v.co.x) for v in _o.data.vertices]
        _ys = [float(v.co.y) for v in _o.data.vertices]
        _zs = [float(v.co.z) for v in _o.data.vertices]
        _cx = (min(_xs) + max(_xs)) / 2
        _cy = (min(_ys) + max(_ys)) / 2
        _cz = (min(_zs) + max(_zs)) / 2
        _o.scale = (_sx, _sy, _sz)
        _o.location = (_tx - _sx * _cx, _ty - _sy * _cy, _tz - _sz * _cz)
        joined.append(_o)
        print('PLACED', _o.name, 'scale', tuple(round(v, 3) for v in _o.scale), 'loc', tuple(round(v, 3) for v in _o.location))

    _qh = [o for o in _fit_parts if 'Hair' in o.name][0]
    _qhx = [float(v.co.x) for v in _qh.data.vertices]
    _qhz = [float(v.co.z) for v in _qh.data.vertices]
    _fx = (_skullw * 1.30) / ((max(_qhx) - min(_qhx)) or 1.0)
    _fz = 0.30 / ((max(_qhz) - min(_qhz)) or 1.0)
    print('HAIRFIT skull-w: %.3f quat-w: %.3f' % (_skullw, max(_qhx) - min(_qhx)))
    _place(_qh, _fx, _fx, _fz, 0.0, head_w.y - 0.01, _crown - (max(_qhz) - min(_qhz)) * _fz / 2 + 0.06)


    # Eyeliner: thin dark planes just under the brow ridge.
    for side, tag in ((-1, 'L'), (1, 'R')):
        bpy.ops.mesh.primitive_cube_add(location=(side * 0.058, brow_y - 0.002, brow_z - 0.028))
        liner = bpy.context.object
        liner.name = 'MarikaLiner' + tag
        liner.dimensions = (0.055, 0.010, 0.008)
        liner.rotation_euler = (0, -side * 0.22, 0)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        finish_custom(liner, mats['Liner'])

    # (Sleeves are painted zones on the arms, not geometry; see the zone pass.)
    bpy.context.view_layer.update()
    kd = kdtree.KDTree(len(body_verts))
    for i, v in enumerate(body_verts):
        kd.insert(body_world @ v.co, i)
    kd.balance()
    deform = {b.name for b in rig.data.bones if b.use_deform}

    def transfer(part):
        for v in part.data.vertices:
            _co, idx, _d = kd.find(part.matrix_world @ v.co)
            pairs = [(human.vertex_groups[g.group].name, g.weight)
                     for g in body_verts[idx].groups
                     if human.vertex_groups[g.group].name in deform]
            total = sum(w for _, w in pairs)
            if total <= 0 and pairs:
                total = 1.0
            seen = set()
            for gname, w in pairs:
                if gname in seen:
                    continue
                seen.add(gname)
                if gname not in part.vertex_groups:
                    part.vertex_groups.new(name=gname)
                part.vertex_groups[gname].add([v.index], w / total if total > 0 else 0.0, 'REPLACE')
        print('WEIGHTED', part.name, 'bones:', len(deform))

    for part in joined:
        transfer(part)

    # Face attachments ride the skull rigidly: 100% Head bone. The body's own
    # mouth-area verts sit on spine/neck weights, so nearest-transfer would
    # leave hair and brows behind on a head turn.
    for rigid in [b for b in joined if 'Brow' in b.name or 'Hair' in b.name
                or 'Eyebrow' in b.name or 'Liner' in b.name]:
        for _g in list(rigid.vertex_groups):
            rigid.vertex_groups.remove(_g)
        rigid.vertex_groups.new(name='mixamorig:Head')
        for _v in rigid.data.vertices:
            rigid.vertex_groups['mixamorig:Head'].add([_v.index], 1.0, 'REPLACE')
        print('RIGID', rigid.name)
    for part in joined:
        if 'Follow' not in part.modifiers:
            mod = part.modifiers.new('Follow', 'ARMATURE')
            mod.object = rig
        print('RIGGED', part.name)

    # Four separate mouth planes (swapped by the Unity rig like Pisti).
    # mouth_y/mouth_z come from the skin measurement above.
    mouths = []
    for name, size in (('MouthNeutral', (0.075, 0.014, 0.012)),
                       ('MouthSmile', (0.10, 0.024, 0.012)),
                       ('MouthGrim', (0.09, 0.012, 0.012)),
                       ('MouthPanic', (0.055, 0.062, 0.012))):
        bpy.ops.mesh.primitive_cube_add(location=(0, mouth_y, mouth_z))
        m = bpy.context.object
        m.name = 'Marika' + name
        m.dimensions = size
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        m.data.materials.append(principled('Marika' + name, mouth_cols[name]))
        for _g in list(m.vertex_groups):
            m.vertex_groups.remove(_g)
        m.vertex_groups.new(name='mixamorig:Head')
        for _v in m.data.vertices:
            m.vertex_groups['mixamorig:Head'].add([_v.index], 1.0, 'REPLACE')
        mod = m.modifiers.new('Follow', 'ARMATURE')
        mod.object = rig
        mouths.append(m)
        print('MOUTH', m.name)
    for _m in mouths:
        if 'Neutral' not in _m.name:
            _m.hide_render = True
    print('MOUTH_HIDE done')

    bpy.context.view_layer.update()
    _zs = [(human.matrix_world @ v.co).z for v in human.data.vertices]
    _cur = max(_zs) - min(_zs)
    _f = 1.80 / _cur if _cur > 0 else 1.0
    print('NORMALIZE body', round(_cur, 3), 'factor', round(_f, 4))
    _all = [human, rig] + [o for o in bpy.context.scene.objects
                            if o.type == 'MESH' and o.name.startswith('Marika')]
    for _o in _all:
        _o.scale = (_o.scale.x * _f, _o.scale.y * _f, _o.scale.z * _f)
    bpy.ops.object.select_all(action='DESELECT')
    for _o in _all:
        _o.select_set(True)
    bpy.context.view_layer.objects.active = human
    bpy.context.view_layer.update()
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.object.select_all(action='DESELECT')
    print('SCALES_BAKED')

    # --- Actions: walk / idle / present / complain, each with its own slot.
    # Normalize the envelope first so previews, blend and FBX share it: visible
    # body height -> 1.80 m (feet stay planted, origin is at ground level).
    thighL = find_bone(bones, 'left', 'upleg')
    thighR = find_bone(bones, 'right', 'upleg')
    shinL = next((b for b in bones if 'left' in b.lower() and 'leg' in b.lower()
                  and 'up' not in b.lower() and 'foot' not in b.lower() and 'toe' not in b.lower()), None)
    shinR = next((b for b in bones if 'right' in b.lower() and 'leg' in b.lower()
                  and 'up' not in b.lower() and 'foot' not in b.lower() and 'toe' not in b.lower()), None)
    armL = find_bone(bones, 'left', 'arm')
    armR = find_bone(bones, 'right', 'arm')
    foreL = find_bone(bones, 'left', 'forearm')
    foreR = find_bone(bones, 'right', 'forearm')
    head = 'mixamorig:Head'
    print('MAPPED', {'thighL': thighL, 'shinL': shinL, 'armL': armL, 'armR': armR, 'foreR': foreR})

    def make_action(name, frames):
        ad = rig.animation_data_create()
        act = bpy.data.actions.new(name)
        ad.action = act
        slot = act.slots.new(id_type='OBJECT', name=name + 'Slot')
        act.slots.active = slot
        sc.frame_start = 1
        sc.frame_end = frames
        return act

    def pose(frame, values):
        sc.frame_set(frame)
        for bname, euler in values.items():
            if bname is None:
                continue
            pb = rig.pose.bones.get(bname)
            if pb is None:
                continue
            pb.rotation_mode = 'XYZ'
            pb.rotation_euler = euler
            pb.keyframe_insert(data_path='rotation_euler', frame=frame)

    make_action('MarikaWalk', 33)
    sw = 0.5
    pose(1, {thighL: (sw, 0, 0), thighR: (-sw, 0, 0), shinL: (0.15, 0, 0), shinR: (0.35, 0, 0),
             armL: (-sw * .7, 0, 0), armR: (sw * .7, 0, 0)})
    pose(17, {thighL: (-sw, 0, 0), thighR: (sw, 0, 0), shinL: (0.35, 0, 0), shinR: (0.15, 0, 0),
              armL: (sw * .7, 0, 0), armR: (-sw * .7, 0, 0)})
    pose(33, {thighL: (sw, 0, 0), thighR: (-sw, 0, 0), shinL: (0.15, 0, 0), shinR: (0.35, 0, 0),
              armL: (-sw * .7, 0, 0), armR: (sw * .7, 0, 0)})

    make_action('MarikaIdle', 61)
    pose(1, {armL: (-0.06, 0, 0.03), armR: (-0.06, 0, -0.03), head: (0, 0, 0)})
    pose(31, {armL: (-0.10, 0, 0.03), armR: (-0.10, 0, -0.03), head: (0.04, 0, 0.05)})
    pose(61, {armL: (-0.06, 0, 0.03), armR: (-0.06, 0, -0.03), head: (0, 0, 0)})

    make_action('MarikaPresent', 25)
    pose(1, {armR: (0, 0, 0), head: (0, 0, 0)})
    pose(13, {armR: (-1.15, 0, 0), foreR: (-0.45, 0, 0), head: (0.08, 0, -0.06)})
    pose(25, {armR: (0, 0, 0), head: (0, 0, 0)})

    make_action('MarikaComplain', 33)
    pose(1, {head: (0.05, 0, -0.28), armL: (-0.2, 0, 0.5), armR: (-0.2, 0, -0.5)})
    pose(9, {head: (0.05, 0, 0.28), armL: (-0.2, 0, 0.5), armR: (-0.2, 0, -0.5)})
    pose(17, {head: (0.05, 0, -0.28), armL: (-0.2, 0, 0.5), armR: (-0.2, 0, -0.5)})
    pose(25, {head: (0.05, 0, 0.28), armL: (-0.2, 0, 0.5), armR: (-0.2, 0, -0.5)})
    pose(33, {head: (0.05, 0, -0.28), armL: (-0.2, 0, 0.5), armR: (-0.2, 0, -0.5)})
    print('ACTIONS', [a.name for a in bpy.data.actions])

    # Previews render after the zone pass so they show the final outfit.
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x = 1280
    sc.render.resolution_y = 720
    sc.render.film_transparent = False
    w = bpy.data.worlds['World']
    w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.85, .85, .86, 1)
    key = bpy.data.lights.new(name='Key', type='SUN')
    key.energy = 1.5
    key_obj = bpy.data.objects.new(name='Key', object_data=key)
    bpy.context.collection.objects.link(key_obj)
    key_obj.location = (2, -3, 5)
    fill = bpy.data.lights.new(name='Fill', type='AREA')
    fill.energy = 300
    fill.size = 2.0
    fill_obj = bpy.data.objects.new(name='Fill', object_data=fill)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.location = (1.5, -3.0, 2.0)
    cd = bpy.data.cameras.new('MarikaCam')
    cam = bpy.data.objects.new('MarikaCam', cd)
    bpy.context.collection.objects.link(cam)
    sc.camera = cam
    cam.data.lens = 50

    def frame(loc, tgt, filename, action_name=None, action_frame=1):
        if action_name is not None:
            rig.animation_data.action = bpy.data.actions.get(action_name)
            sc.frame_set(action_frame)
        else:
            sc.frame_set(1)
        tgt_v = Vector(tgt)
        loc_v = Vector(loc)
        cam.location = loc_v
        cam.rotation_euler = (tgt_v - loc_v).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = str(art / filename)
        bpy.ops.render.render(write_still=True)

    print('RIG_PREVIEWS_READY')

    # Drop MPFB helper geometry (mirrors exportservice: delete the verts, drop
    # the mask that hid them, strip non-deform groups) so the FBX only carries
    # the visible skinned body.
    _bm2 = _bmesh.new()
    _bm2.from_mesh(human.data)
    _bm2.verts.ensure_lookup_table()
    _junk = set()
    for _grp in ('HelperGeometry', 'JointCubes'):
        _g = human.vertex_groups.get(_grp)
        if _g is not None:
            _junk.add(_g.index)
    _killv = [v for v in _bm2.verts if any(g.group in _junk for g in human.data.vertices[v.index].groups)]
    _bmesh.ops.delete(_bm2, geom=_killv, context='VERTS')
    _bm2.to_mesh(human.data)
    _bm2.free()
    human.data.update()
    for _m in [m for m in human.modifiers if m.type == 'MASK']:
        human.modifiers.remove(_m)
    for _o in [human] + joined + mouths:
        for _g in [g for g in _o.vertex_groups
                   if g.name.startswith('helper-') or g.name.startswith('joint-')
                   or g.name in ('Mid', 'Left', 'Right', 'HelperGeometry', 'JointCubes')]:
            _o.vertex_groups.remove(_g)
    print('HELPERS_REMOVED verts:', len(_killv), 'body now:', len(human.data.vertices))

    # Zone-painted outfit on the smooth body: burgundy blouse, charcoal
    # trousers, dark shoes. No extra geometry, so the silhouette stays MPFB.
    # The install ships no skin pack, so the body arrives material-less: slot 0
    # is created as skin, then the clothing slots.
    _mesh = human.data
    _skin = principled('MarikaSkin', (.72, .52, .38))
    for _mat in (_skin, mats['Blouse'], mats['Skirt'], mats['Shoe'], mats['Blush']):
        _mesh.materials.append(_mat)
    _bidx = {m.name: i for i, m in enumerate(_mesh.materials)}
    # Blush band anchored to the actual mouth plane (post-normalize-proof).
    _m0 = mouths[0]
    _mcen = _m0.matrix_world @ _m0.data.vertices[0].co
    for _v in _m0.data.vertices[1:]:
        _mcen += _m0.matrix_world @ _v.co
    _mcen /= len(_m0.data.vertices)
    _counts = {}
    for _p in _mesh.polygons:
        _w = human.matrix_world @ _mesh.vertices[_p.vertices[0]].co
        _wx = abs(_w.x)
        _wy = _w.y
        _wz = _w.z
        _domname = ''
        _bw = 0.0
        for _g in _mesh.vertices[_p.vertices[0]].groups:
            _gn = human.vertex_groups[_g.group].name
            if _g.weight > _bw:
                _domname, _bw = _gn, _g.weight
        _is_hand = 'Hand' in _domname or 'Finger' in _domname or 'Thumb' in _domname
        _is_arm = ('Arm' in _domname) or (_wx > 0.24 and _wz > 0.60)
        _is_blush = (_mcen.z + 0.012 < _wz < _mcen.z + 0.095
                     and 0.035 < _wx < 0.115 and _wy > _mcen.y - 0.02)
        _zone = None
        if _is_hand:
            _zone = None
        elif _is_blush:
            _zone = 'MarikaBlush'
        elif _is_arm:
            _zone = 'MarikaBlouse' if 1.24 <= _wz <= 1.54 else None
        elif _wz < 1.02 and abs(_wx) < 0.30:
            _zone = 'MarikaShoe' if _wz < 0.12 else 'MarikaSkirt'
        else:
            if 1.00 <= _wz <= 1.54:
                _zone = 'MarikaBlouse'
            elif 0.40 <= _wz < 1.00:
                _zone = 'MarikaSkirt'
        if _zone is not None:
            _p.material_index = _bidx[_zone]
            _counts[_zone] = _counts.get(_zone, 0) + 1
    print('ZONES', _counts)

    frame((0, 4.4, 1.5), (0, 0, 0.9), 'marika-preview-front.png', 'MarikaIdle', 31)
    frame((4.4, 0, 1.5), (0, 0, 0.9), 'marika-preview-side.png', 'MarikaIdle', 31)
    frame((0, 4.4, 1.5), (0, 0, 0.9), 'marika-preview-walk.png', 'MarikaWalk', 13)
    frame((0, 1.35, 1.60), (0, 0.02, 1.52), 'marika-preview-face.png', 'MarikaIdle', 31)
    print('PREVIEWS_DONE')

    for _o in [human] + joined + mouths:
        _o.hide_render = False
    print('UNHIDDEN_FOR_EXPORT')

    bpy.ops.wm.save_as_mainfile(filepath=str(art / 'Marika.blend'))
    print('BLEND_SAVED verts:', len(human.data.vertices))
    bpy.ops.object.select_all(action='DESELECT')
    for o in list(bpy.context.scene.objects):
        if o.type in ('MESH', 'ARMATURE'):
            o.select_set(True)
    bpy.context.view_layer.objects.active = human
    fbx = art / 'Marika.fbx'
    bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, apply_unit_scale=True,
                             bake_space_transform=True, bake_anim=True,
                             bake_anim_use_all_actions=True, bake_anim_simplify_factor=1.0,
                             add_leaf_bones=False)
    import shutil
    shutil.copy(str(fbx), str(dest / 'Marika.fbx'))
    print('MARIKA_DONE verts:', len(human.data.vertices))


main()
