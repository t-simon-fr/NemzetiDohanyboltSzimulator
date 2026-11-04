"""Run with Blender --background --factory-startup --python this-file.
Original cash register geometry. Unity coordinates are explicitly exported.
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
output = root / 'ArtSource/Checkout'
output.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = 'METRIC'

colors = {'Shell':(.74,.77,.74,1), 'Dark':(.065,.085,.079,1),
          'Accent':(.08,.23,.19,1), 'Metal':(.46,.51,.50,1),
          'Paper':(.94,.94,.87,1), 'LCD':(.91,.94,.85,1)}
materials = {}
for key, color in colors.items():
    material = bpy.data.materials.new(key)
    material.diffuse_color = color
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

def box(name, position, size, material, bevel=.008, tilt=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=to_blender(position))
    obj = bpy.context.object
    obj.dimensions = (size[0], size[2], size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler.x = math.radians(tilt)
    return finish(obj, name, material, bevel)

# A hollow cabinet keeps the moving insert unobstructed.
box('Cabinet_Base',(0,.018,0),(.65,.036,.49),'Dark',.012)
for side in (-1,1):
    box('Cabinet_Side_'+str(side),(side*.315,.087,0),(.022,.14,.49),'Dark',.008)
box('Cabinet_Back',(0,.085,-.235),(.61,.13,.022),'Dark')
box('Cabinet_Cap',(0,.15,0),(.65,.025,.49),'Shell',.01)
for x in (-.265,.265):
    for z in (-.18,.18): box('Rubber_Foot',(x,.002,z),(.075,.016,.065),'Dark',.006)

vertices = [(-.31,.16,-.23),(.31,.16,-.23),(.31,.16,.23),(-.31,.16,.23),
            (-.31,.32,-.23),(.31,.32,-.23),(.31,.20,.23),(-.31,.20,.23)]
faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(3,7,6,2),(0,4,7,3),(1,2,6,5)]
mesh = bpy.data.meshes.new('Sloped shell mesh')
mesh.from_pydata([to_blender(v) for v in vertices], [], [tuple(reversed(face)) for face in faces])
mesh.update()
obj = bpy.data.objects.new('Register_Shell', mesh)
bpy.context.collection.objects.link(obj)
finish(obj, 'Register_Shell', 'Shell', .016)
box('Front_Accent',(0,.18,.235),(.57,.024,.014),'Accent',.006)
box('Keypad_Recess',(0,.258,.060),(.435,.014,.224),'Dark',.01,18)

box('Display_Stem',(0,.35,-.185),(.075,.16,.052),'Accent',.012,-12)
box('Display_Bezel',(0,.49,-.15),(.56,.255,.045),'Dark',.017,-25)
normal = Vector((0, math.sin(math.radians(25)), math.cos(math.radians(25))))
screen = Vector((0,.49,-.15)) + normal*.027
box('LCD_Glass',screen,(.516,.213,.007),'LCD',.005,-25)
box('Printer_Cover',(.253,.305,-.13),(.105,.09,.13),'Shell',.012)
box('Printer_Slot',(.253,.351,-.12),(.080,.005,.018),'Dark',.002)
box('Receipt_Paper',(.253,.385,-.12),(.074,.070,.0015),'Paper',.0005,-12)
for row in range(4):
    box('Receipt_Ink_'+str(row),(.253,.405-row*.012,-.112),(.050 if row else .035,.002,.0005),'Dark',0,-12)

box('Drawer_Floor',(0,.044,.015),(.59,.014,.41),'Dark',.006)
for side in (-1,1): box('Drawer_Rim_'+str(side),(side*.29,.079,.015),(.014,.07,.41),'Dark',.004)
box('Drawer_Fascia',(0,.078,.229),(.63,.108,.027),'Shell',.012)
box('Drawer_Handle',(0,.079,.250),(.215,.027,.020),'Metal',.008)
box('Drawer_Handle_Inset',(0,.079,.244),(.245,.038,.007),'Dark',.008)
box('Drawer_Lock',(-.255,.078,.246),(.018,.018,.004),'Metal',.007)

template=box('Keycap_Template',(0,0,0),(1,1,1),'Shell',.10)
template.hide_render=True
template.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(output/'ModernRegister.blend'))
parts=[]
depsgraph=bpy.context.evaluated_depsgraph_get()
for obj in bpy.context.scene.objects:
    if obj.type != 'MESH': continue
    evaluated=obj.evaluated_get(depsgraph)
    mesh=evaluated.to_mesh()
    mesh.calc_loop_triangles()
    vertices=[]; normals=[]; triangles=[]
    normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
    for triangle in mesh.loop_triangles:
        start=len(vertices)
        for index in triangle.loops:
            vertex=obj.matrix_world @ mesh.vertices[mesh.loops[index].vertex_index].co
            normal=(normal_matrix @ mesh.corner_normals[index].vector).normalized()
            vertices.append({'x':vertex.x,'y':vertex.z,'z':-vertex.y})
            normals.append({'x':normal.x,'y':normal.z,'z':-normal.y})
        triangles.extend([start,start+1,start+2])
    color=colors[obj.data.materials[0].name]
    parts.append({'name':obj.name,'color':{'r':color[0],'g':color[1],'b':color[2],'a':1},
                  'vertices':vertices,'normals':normals,'triangles':triangles})
    evaluated.to_mesh_clear()
(output/'ModernRegister.mesh.json').write_text(json.dumps({'parts':parts},separators=(',',':')),encoding='utf-8')
print('Exported original register:',len(parts),'parts,',sum(len(part['triangles'])//3 for part in parts),'triangles')
