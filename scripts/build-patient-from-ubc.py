# Qaniva patient build from the CC0 Quaternius "Universal Base Characters (Standard)" pack.
# Usage (headless):
#   /Applications/Blender.app/Contents/MacOS/Blender -b --python scripts/build-patient-from-ubc.py -- \
#     "<pack>/Base Characters/Unity/Superhero_Male_FullBody.fbx" \
#     "<pack>/Hairstyles/Rigged to Head Bone/FBX (Unity)/Hair_Buzzed.fbx" \
#     unity/QanivaSimulation/Assets/Qaniva/Art/Patients/adult_ubc_v1.fbx preview.png 38
# The pack is CC0 (see Art/Patients/LICENSE-quaternius.txt); it is NOT committed — download from quaternius.com.
# Builds Qaniva's patient from the CC0 Quaternius Universal Base Characters (Standard):
# renames bones to the Qaniva contract, dresses the torso in a gown material, adds hair,
# poses semi-recumbent with arms at the sides, bakes the pose, exports FBX + a preview.
import bpy, sys, math, os
from mathutils import Matrix, Vector
args=sys.argv[sys.argv.index('--')+1:]
SRC, HAIR, OUT, PREVIEW = args[0], args[1], args[2], args[3]
TORSO_BEND = float(args[4]) if len(args)>4 else 30.0

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
meshes=sorted([o for o in bpy.data.objects if o.type=='MESH'], key=lambda o: len(o.data.vertices), reverse=True)
print('IMPORTED', [(o.type,o.name) for o in bpy.data.objects])
body=meshes[0]
others=[o for o in bpy.data.objects if o.type=='MESH' and o is not body]

# --- bone-name contract (PatientVisualController keys off "Chest"; keep readable names) ---
rename={'pelvis':'Hips','spine_01':'Spine','spine_02':'Chest','spine_03':'UpperChest','neck_01':'Neck',
        'upperarm_l':'UpperArm.L','upperarm_r':'UpperArm.R','lowerarm_l':'LowerArm.L','lowerarm_r':'LowerArm.R',
        'hand_l':'Hand.L','hand_r':'Hand.R','thigh_l':'Thigh.L','thigh_r':'Thigh.R','calf_l':'Calf.L','calf_r':'Calf.R',
        'foot_l':'Foot.L','foot_r':'Foot.R'}
for b in arm.data.bones:
    if b.name in rename: b.name=rename[b.name]

# --- materials: textured skin keeps its texture but is named for the tint contract ---
skin=body.data.materials[0]; skin.name='Skin_Body'
gown=bpy.data.materials.new('Gown'); gown.use_nodes=True
bsdf=gown.node_tree.nodes.get('Principled BSDF'); bsdf.inputs['Base Color'].default_value=(0.42,0.60,0.72,1); bsdf.inputs['Roughness'].default_value=0.85
body.data.materials.append(gown)
# gown = torso + hips + upper arms (rest pose z-ranges), leaves head/forearms/legs as skin
for p in body.data.polygons:
    c=sum((body.data.vertices[i].co for i in p.vertices), Vector())/len(p.vertices)
    x,z=abs(c.x),c.z
    if (0.56<=z<=1.48 and x<0.30) or (1.28<=z<=1.48 and x<0.40):
        p.material_index=1

# --- hair rigged to the Head bone ---
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=HAIR)
new=[o for o in bpy.data.objects if o not in before]
hair_mesh=[o for o in new if o.type=='MESH'][0]
for o in new:
    if o.type=='ARMATURE': bpy.data.objects.remove(o, do_unlink=True)
hair_mesh.parent=arm
for m in hair_mesh.modifiers:
    if m.type=='ARMATURE': m.object=arm
if not any(m.type=='ARMATURE' for m in hair_mesh.modifiers):
    hair_mesh.modifiers.new('Armature','ARMATURE').object=arm
for m in hair_mesh.data.materials:
    if m: m.name='Hair'
hair_mesh.name='Hair'

# --- pose: semi-recumbent torso, arms down at the sides, slight head lift ---
bpy.context.view_layer.objects.active=arm
bpy.ops.object.mode_set(mode='POSE')
pb=arm.pose.bones
def rot_world(name, axis, deg):
    b=pb[name]; head=b.head.copy()
    R=Matrix.Translation(head) @ Matrix.Rotation(math.radians(deg),4,axis) @ Matrix.Translation(-head)
    b.matrix = R @ b.matrix
    bpy.context.view_layer.update()
# flexion about the body's left-right axis (+X in armature space, character faces -Y? Quaternius faces -Y (rot z=180)).
for name,frac in (('Spine',0.35),('Chest',0.35),('UpperChest',0.30)):
    rot_world(name,'X',TORSO_BEND*frac)
rot_world('Head','X',-TORSO_BEND*0.45)
# arms: from T-pose down to the sides (rotate about the depth axis Y at the shoulder)
rot_world('UpperArm.L','Y', 84)
rot_world('UpperArm.R','Y',-84)
rot_world('LowerArm.L','Y', 6); rot_world('LowerArm.R','Y',-6)
# bake pose as rest so the export is a static, posed character
bpy.ops.object.mode_set(mode='OBJECT')
for o in [body,hair_mesh]+others:
    bpy.context.view_layer.objects.active=o
    for m in list(o.modifiers):
        if m.type=='ARMATURE':
            bpy.ops.object.modifier_copy(modifier=m.name)
            bpy.ops.object.modifier_apply(modifier=o.modifiers[-1].name)
bpy.context.view_layer.objects.active=arm
bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode='OBJECT')

# --- preview render (three-quarter view) ---
scene=bpy.context.scene
cam=bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); scene.collection.objects.link(cam)
cam.location=(1.9,2.7,1.7); cam.rotation_euler=(math.radians(72),0,math.radians(145)); scene.camera=cam
light=bpy.data.objects.new('Sun', bpy.data.lights.new('Sun','SUN')); light.data.energy=3; light.rotation_euler=(math.radians(50),0,math.radians(30)); scene.collection.objects.link(light)
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=700; scene.render.resolution_y=900; scene.render.filepath=PREVIEW
try:
    bpy.ops.render.render(write_still=True)
except Exception as e:
    print('render failed', e)

# --- export ---
for o in bpy.data.objects: o.select_set(o.type in ('MESH','ARMATURE'))
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                         add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=False)
tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.data.objects if o.type=='MESH')
print(f"EXPORTED {OUT} tris={tris} bones={len(arm.data.bones)} meshes={[o.name for o in bpy.data.objects if o.type=='MESH']}")
