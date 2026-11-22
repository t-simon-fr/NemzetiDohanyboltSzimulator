"""Run with D:\\Blender\\blender.exe --background --factory-startup --python this-file.
Pisti: young hurried customer. Proportions and pivots in Unity meters, origin at
feet, facing Unity +Z (converted with to_blender like the coffee machine script).
Every part carries its animation pivot; Unity parents parts by the parent field
and places each part GameObject at its pivot, so procedural animation rotates
around shoulders, elbows, wrists, hips, knees and ankles.
Keeps the original envelope: total height ~1.79 m, right-hand idle position near
the old (±.28, .76, .025) hand, basket attach point untouched (actor-owned).
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector, Matrix

root = Path(__file__).resolve().parents[2]
output = root / 'ArtSource/Characters'
output.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = 'METRIC'

colors = {
    'Jacket': (.18, .33, .65, 1),
    'Pants': (.10, .13, .22, 1),
    'Shoe': (.13, .13, .15, 1),
    'Skin': (.72, .52, .38, 1),
    'Hair': (.10, .075, .06, 1),
    'Pack': (.16, .16, .19, 1),
    'Strap': (.09, .09, .11, 1),
    'White': (.92, .90, .86, 1),
    'Dark': (.04, .035, .035, 1),
    'Mouth': (.35, .10, .12, 1),
}
materials = {}
for key, color in colors.items():
    material = bpy.data.materials.new(key)
    material.diffuse_color = color
    principled = material.node_tree.nodes.get('Principled BSDF')
    if principled is not None:
        principled.inputs['Base Color'].default_value = color
    materials[key] = material


def to_blender(point):
    return (point[0], -point[2], point[1])


def place_part(name, parent, pivot_unity, material):
    return {'name': name, 'parent': parent, 'pivot': pivot_unity, 'material': material}


PARTS = []


def finish(obj, spec, bevel, smooth):
    obj.name = spec['name']
    obj.data.materials.append(materials[spec['material']])
    if bevel:
        modifier = obj.modifiers.new('Soft edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 2
        modifier.affect = 'EDGES'
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
        weighted = obj.modifiers.new('Weighted surface normals', 'WEIGHTED_NORMAL')
        weighted.keep_sharp = True
    anchor = Vector(to_blender(spec['anchor']))
    pivot = to_blender(spec['pivot'])
    mesh = obj.data
    mesh.transform(Matrix.Translation((anchor[0] - pivot[0], anchor[1] - pivot[1], anchor[2] - pivot[2])))
    obj.location = (0, 0, 0)
    obj.rotation_euler = (0, 0, 0)
    PARTS.append(spec)


def box(name, parent, pivot, position, size, material, bevel=.012, rot_y=0.0, anchor=None):
    bpy.ops.mesh.primitive_cube_add(location=to_blender(position))
    obj = bpy.context.object
    obj.dimensions = (size[0], size[2], size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if rot_y:
        obj.rotation_euler = (0, rot_y, 0)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    spec = place_part(name, parent, pivot, material)
    spec['anchor'] = position if anchor is None else anchor
    return finish(obj, spec, bevel, False)


def ball(name, parent, pivot, position, size, material, bevel=0.0):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=12, location=to_blender(position))
    obj = bpy.context.object
    obj.dimensions = (size[0], size[2], size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    spec = place_part(name, parent, pivot, material)
    spec['anchor'] = position
    return finish(obj, spec, bevel, True)


ROOT = 'ROOT'
HIPS = (0, .95, 0)
NECK = (0, 1.50, 0)
MOUTH = (0, 1.545, .125)

# Torso: green-blue tracksuit jacket, hips to shoulders.
box('Torso', ROOT, HIPS, (0, 1.24, 0), (.38, .58, .25), 'Jacket', .02)
# Head and messy hair.
ball('Head', 'Torso', NECK, (0, 1.65, .01), (.24, .27, .25), 'Skin')
ball('Hair', 'Head', NECK, (0, 1.735, -.02), (.27, .17, .27), 'Hair')
for i, dx in enumerate((-.07, 0.0, .07)):
    ball('HairSpike%d' % i, 'Head', NECK, (dx, 1.83, .01), (.07, .09, .07), 'Hair')
# Face: nose, eyes with pupils, rushed angled brows, three mouth variants.
ball('Nose', 'Head', NECK, (0, 1.605, .13), (.05, .055, .05), 'Skin')
for side, tag in ((-1, 'L'), (1, 'R')):
    ball('EyeWhite' + tag, 'Head', NECK, (side * .082, 1.66, .108), (.054, .062, .03), 'White')
    ball('Pupil' + tag, 'Head', NECK, (side * .082, 1.657, .13), (.024, .028, .012), 'Dark')
    box('Brow' + tag, 'Head', NECK, (side * .082, 1.718, .112), (.08, .02, .02), 'Hair', 0.0, rot_y=-side * .28)
box('MouthNeutral', 'Head', MOUTH, (0, 1.54, .122), (.07, .012, .012), 'Mouth')
box('MouthSmile', 'Head', MOUTH, (0, 1.545, .122), (.095, .022, .012), 'Mouth')
box('MouthPanic', 'Head', MOUTH, (0, 1.535, .122), (.05, .06, .012), 'Dark')
box('MouthGrim', 'Head', MOUTH, (0, 1.532, .122), (.085, .012, .012), 'Mouth')

# Arms: shoulder pivots, white tracksuit stripes on the outer upper arms.
for side, tag in ((-1, 'L'), (1, 'R')):
    shoulder = (side * .26, 1.47, 0)
    elbow = (side * .26, 1.12, 0)
    wrist = (side * .26, .82, 0)
    box('ArmUpper' + tag, 'Torso', shoulder, (side * .26, 1.30, 0), (.11, .36, .11), 'Jacket')
    box('StripeArm' + tag, 'ArmUpper' + tag, shoulder, (side * .321, 1.30, 0), (.014, .30, .07), 'White')
    box('ArmLower' + tag, 'ArmUpper' + tag, elbow, (side * .26, .96, 0), (.10, .30, .10), 'Jacket')
    ball('Hand' + tag, 'ArmLower' + tag, wrist, (side * .26, .75, .01), (.095, .13, .095), 'Skin')

# Legs: hip pivots, dark pants, chunky shoes with forward toes.
for side, tag in ((-1, 'L'), (1, 'R')):
    hip = (side * .11, .95, 0)
    knee = (side * .11, .50, 0)
    ankle = (side * .11, .10, 0)
    box('LegUpper' + tag, ROOT, hip, (side * .11, .725, 0), (.15, .46, .16), 'Pants')
    box('LegLower' + tag, 'LegUpper' + tag, knee, (side * .11, .295, 0), (.13, .40, .14), 'Pants')
    box('Shoe' + tag, 'LegLower' + tag, ankle, (side * .11, .05, .03), (.13, .10, .30), 'Shoe', .01)

# Backpack with chest straps, rides on the torso.
box('Pack', 'Torso', HIPS, (0, 1.24, -.215), (.30, .44, .17), 'Pack', .02)
for side, tag in ((-1, 'L'), (1, 'R')):
    box('Strap' + tag, 'Torso', HIPS, (side * .115, 1.30, .128), (.07, .30, .025), 'Strap')

bpy.ops.wm.save_as_mainfile(filepath=str(output / 'Pisti.blend'))
parts = []
depsgraph = bpy.context.evaluated_depsgraph_get()
for spec in PARTS:
    obj = bpy.data.objects[spec['name']]
    if obj.type != 'MESH':
        continue
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    vertices = []
    normals = []
    triangles = []
    normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
    for triangle in mesh.loop_triangles:
        start = len(vertices)
        for index in triangle.loops:
            vertex = obj.matrix_world @ mesh.vertices[mesh.loops[index].vertex_index].co
            normal = (normal_matrix @ mesh.corner_normals[index].vector).normalized()
            vertices.append({'x': vertex.x, 'y': vertex.z, 'z': -vertex.y})
            normals.append({'x': normal.x, 'y': normal.z, 'z': -normal.y})
        triangles.extend([start, start + 1, start + 2])
    color = colors[spec['material']]
    parts.append({'name': spec['name'], 'parent': spec['parent'],
                  'pivot': {'x': spec['pivot'][0], 'y': spec['pivot'][1], 'z': spec['pivot'][2]},
                  'color': {'r': color[0], 'g': color[1], 'b': color[2], 'a': 1},
                  'vertices': vertices, 'normals': normals, 'triangles': triangles})
    evaluated.to_mesh_clear()
(output / 'Pisti.mesh.json').write_text(json.dumps({'parts': parts}, separators=(',', ':')), encoding='utf-8')
print('Exported Pisti:', len(parts), 'parts,', sum(len(part['triangles']) // 3 for part in parts), 'triangles')

# Preview renders for the model-update rule: front, side and back views.
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.film_transparent = False
world = bpy.data.worlds['World']
world.use_nodes = True
world.node_tree.nodes['Background'].inputs[0].default_value = (.85, .85, .86, 1)
bpy.ops.object.light_add(type='SUN', location=(2, 4, 5))
bpy.context.object.data.energy = 1.5
bpy.ops.object.light_add(type='AREA', location=(1.5, 3.0, 2.0))
bpy.context.object.data.energy = 300
bpy.context.object.data.size = 2.0
cam_data = bpy.data.cameras.new('PreviewCam')
cam = bpy.data.objects.new('PreviewCam', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.data.lens = 40


def frame(location, filename):
    target = Vector((0, 0, 0.9))
    direction = target - Vector(location)
    cam.location = location
    cam.rotation_euler = direction.to_track_quat('-Z', 'Z').to_euler()
    scene.render.filepath = str(output / filename)
    bpy.ops.render.render(write_still=True)


frame((0, -3.6, 1.6), 'pisti-preview-front.png')
frame((3.6, 0, 1.6), 'pisti-preview-side.png')
frame((0, 3.6, 1.6), 'pisti-preview-back.png')
print('Pisti preview renders saved.')
