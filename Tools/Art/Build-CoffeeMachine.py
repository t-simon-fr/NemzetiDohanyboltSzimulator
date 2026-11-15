"""Run with D:\\Blender\\blender.exe --background --factory-startup --python this-file.
LaVazze Barista coffee machine. Unity coordinates are explicitly exported.
Keeps the original interactable envelope: cabinet footprint, body size,
display/output slot locals and colliders stay untouched in Unity code.
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
output = root / 'ArtSource/CoffeeMachine'
output.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = 'METRIC'

colors = {'Body': (.06, .18, .13, 1), 'Dark': (.065, .085, .079, 1),
          'Cream': (.93, .88, .74, 1), 'Trim': (.36, .10, .14, 1),
          'Metal': (.46, .51, .50, 1), 'Tank': (.50, .68, .82, 1)}
materials = {}
for key, color in colors.items():
    material = bpy.data.materials.new(key)
    material.diffuse_color = color
    principled = material.node_tree.nodes.get('Principled BSDF')
    if principled is not None:
        principled.inputs['Base Color'].default_value = color
    materials[key] = material

def to_blender(point): return (point[0], -point[2], point[1])

def finish(obj, name, material, bevel):
    obj.name = name
    obj.data.materials.append(materials[material])
    if bevel:
        modifier = obj.modifiers.new('Soft manufactured edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 3
        modifier.affect = 'EDGES'
        modifier = obj.modifiers.new('Weighted surface normals', 'WEIGHTED_NORMAL')
        modifier.keep_sharp = True
    return obj

def box(name, position, size, material, bevel=.008):
    bpy.ops.mesh.primitive_cube_add(location=to_blender(position))
    obj = bpy.context.object
    obj.dimensions = (size[0], size[2], size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, material, bevel)

def cylinder(name, position, radius, height, material, bevel=0, vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, location=to_blender(position))
    obj = bpy.context.object
    obj.dimensions = (radius * 2, radius * 2, height)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, material, bevel)

# Stand cabinet keeps the original .65 x .82 x .55 envelope.
box('Cabinet_Plinth', (0, .03, 0), (.61, .06, .51), 'Dark', .006)
box('Cabinet_Body', (0, .44, 0), (.65, .76, .55), 'Body', .015)
box('Cabinet_CreamStripe', (0, .70, -.276), (.55, .07, .012), 'Cream', .004)
box('Counter_Cap', (0, .825, 0), (.66, .02, .56), 'Dark', .006)

# Main body keeps the original .49 x .67 x .37 envelope.
box('Machine_Body', (0, 1.175, .05), (.49, .67, .37), 'Body', .03)

# Cream front panel with burgundy trim frames the Unity touchscreen.
box('Panel_Trim', (0, 1.26, -.137), (.47, .52, .008), 'Trim', .004)
box('Panel_Cream', (0, 1.26, -.140), (.45, .50, .012), 'Cream', .006)

# Decorative dial and two buttons in the right column below the touchscreen, clear of the spouts.
cylinder('Dial', (-.15, 1.12, -.150), .032, .02, 'Trim', .004)
box('Dial_Pointer', (-.15, 1.13, -.162), (.008, .03, .006), 'Dark', .002)
box('Button_Top', (-.15, 1.03, -.150), (.05, .05, .018), 'Trim', .006)
box('Button_Bottom', (-.15, .94, -.150), (.05, .05, .018), 'Trim', .006)

# Twin spouts above the unchanged output slot.
for x in (-.06, .06):
    box('Spout_Neck_' + str(x), (x, 1.13, -.19), (.05, .07, .12), 'Dark', .008)
    cylinder('Spout_' + str(x), (x, 1.03, -.24), .016, .10, 'Dark', .003, 12)

# Drip tray keeps the original size and position.
box('Drip_Tray', (0, .84, -.27), (.34, .025, .32), 'Dark', .006)

# Side water tank sits on the cabinet cap inside the original footprint.
box('Tank_Body', (.278, 1.03, .05), (.085, .40, .20), 'Tank', .008)
box('Tank_Lid', (.278, 1.245, .05), (.095, .03, .21), 'Dark', .006)

# Bean hopper keeps the original top position and size.
cylinder('Hopper', (0, 1.55, .05), .11, .07, 'Dark', .006)
cylinder('Hopper_Lid', (0, 1.59, .05), .115, .02, 'Body', .004)

bpy.ops.wm.save_as_mainfile(filepath=str(output / 'CoffeeMachine.blend'))
parts = []
depsgraph = bpy.context.evaluated_depsgraph_get()
for obj in bpy.context.scene.objects:
    if obj.type != 'MESH': continue
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    vertices = []; normals = []; triangles = []
    normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
    for triangle in mesh.loop_triangles:
        start = len(vertices)
        for index in triangle.loops:
            vertex = obj.matrix_world @ mesh.vertices[mesh.loops[index].vertex_index].co
            normal = (normal_matrix @ mesh.corner_normals[index].vector).normalized()
            vertices.append({'x': vertex.x, 'y': vertex.z, 'z': -vertex.y})
            normals.append({'x': normal.x, 'y': normal.z, 'z': -normal.y})
        triangles.extend([start, start + 1, start + 2])
    color = colors[obj.data.materials[0].name]
    parts.append({'name': obj.name, 'color': {'r': color[0], 'g': color[1], 'b': color[2], 'a': 1},
                  'vertices': vertices, 'normals': normals, 'triangles': triangles})
    evaluated.to_mesh_clear()
(output / 'CoffeeMachine.mesh.json').write_text(json.dumps({'parts': parts}, separators=(',', ':')), encoding='utf-8')
print('Exported LaVazze Barista:', len(parts), 'parts,', sum(len(part['triangles']) // 3 for part in parts), 'triangles')

# Preview renders for the model-update rule: front and three-quarter views.
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.film_transparent = False
world = bpy.data.worlds['World']
world.use_nodes = True
world.node_tree.nodes['Background'].inputs[0].default_value = (.85, .85, .86, 1)
bpy.ops.object.light_add(type='SUN', location=(2, 4, 5))
sun = bpy.context.object
sun.data.energy = 1.5
bpy.ops.object.light_add(type='AREA', location=(1.5, 3.0, 2.0))
bpy.context.object.data.energy = 300
bpy.context.object.data.size = 2.0
cam_data = bpy.data.cameras.new('PreviewCam')
cam = bpy.data.objects.new('PreviewCam', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.data.lens = 50

def frame(target, location, filename):
    direction = Vector(target) - Vector(location)
    cam.location = location
    cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(output / filename)
    bpy.ops.render.render(write_still=True)

front_target = to_blender((0, .8, -.1))
frame(front_target, (front_target[0], front_target[1] + 3.4, 1.4), 'preview-front.png')
three_target = to_blender((0, .8, 0))
frame(three_target, (1.9, three_target[1] + 3.0, 1.8), 'preview-three-quarter.png')
print('Preview renders saved.')
