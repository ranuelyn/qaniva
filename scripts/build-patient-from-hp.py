# Owner-supplied "Hospital Patient" OBJ set -> rigged, posed, textured FBX for Qaniva.
# Usage: Blender -b --python scripts/build-patient-from-hp.py -- <obj_dir> <out.fbx> <preview_prefix> [torsoBend] [armDown] [armBack]

# Rig + pose + texture the owner-supplied "Hospital Patient" OBJ set (T-pose, cm, facing -Y).
import bpy, sys, glob, os, math
from mathutils import Vector, Matrix
args=sys.argv[sys.argv.index('--')+1:]
SRC, OUT, PREVIEW = args[0], args[1], args[2]
TORSO_BEND=float(args[3]) if len(args)>3 else 28.0
bpy.ops.wm.read_factory_settings(use_empty=True)
for f in sorted(glob.glob(os.path.join(SRC,'*.obj'))):
    bpy.ops.wm.obj_import(filepath=f)
meshes=[o for o in bpy.data.objects if o.type=='MESH']
# cm -> m
for o in meshes: o.scale=(0.01,0.01,0.01)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.transform_apply(scale=True)
# --- textures by part (dims-based identification; names are generic model_N) ---
tex={'model_0':('Skin_Arms','Arms.png'),'model_1':('Skin_Legs','Legs.png'),'model_3':('Skin_Face','Face.png'),
     'model_5':('Skin_Torso','Torso.png'),'model_8':('Gown','gown.png'),'model_6':('Slippers','slippers.png'),'model_7':('Slippers','slippers.png'),
     'model_4':('Eyes','Eyes.png'),'model_9':('Bracelet','bracelet.png'),'model_2':('Mouth','Mouth.png'),'model_10':('Eyelashes','Eyelashes.png'),'model_11':('Eyebrows','Eyelashes.png')}
mats={}
for o in meshes:
    name,img=tex.get(o.name,('Misc','Torso.png'))
    if name not in mats:
        m=bpy.data.materials.new(name); m.use_nodes=True
        bsdf=m.node_tree.nodes.get('Principled BSDF'); t=m.node_tree.nodes.new('ShaderNodeTexImage')
        t.image=bpy.data.images.load(os.path.join(SRC,img)); m.node_tree.links.new(t.outputs['Color'],bsdf.inputs['Base Color'])
        bsdf.inputs['Roughness'].default_value=0.75
        mats[name]=m
    o.data.materials.clear(); o.data.materials.append(mats[name])
    for p in o.data.polygons: p.use_smooth=True
# --- armature from proportions (origin at feet, height ~1.84, facing -Y) ---
arm_data=bpy.data.armatures.new('Armature'); arm=bpy.data.objects.new('Armature',arm_data); bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=arm_data.edit_bones.new(name); b.head=Vector(head); b.tail=Vector(tail)
    if parent: b.parent=arm_data.edit_bones[parent]
    return b
bone('Hips',(0,0,0.98),(0,0,1.10)); bone('Spine',(0,0,1.10),(0,0,1.22),'Hips'); bone('Chest',(0,0,1.22),(0,0,1.36),'Spine')
bone('UpperChest',(0,0,1.36),(0,0,1.50),'Chest'); bone('Neck',(0,0,1.50),(0,0,1.60),'UpperChest'); bone('Head',(0,0,1.60),(0,0,1.85),'Neck')
for s,sx in (('L',1),('R',-1)):
    bone(f'Shoulder.{s}',(sx*0.04,0,1.50),(sx*0.19,0,1.50),'UpperChest')
    bone(f'UpperArm.{s}',(sx*0.19,0,1.50),(sx*0.46,0,1.50),f'Shoulder.{s}')
    bone(f'LowerArm.{s}',(sx*0.46,0,1.50),(sx*0.74,0,1.50),f'UpperArm.{s}')
    bone(f'Hand.{s}',(sx*0.74,0,1.50),(sx*0.92,0,1.50),f'LowerArm.{s}')
    bone(f'Thigh.{s}',(sx*0.10,0,0.96),(sx*0.10,0,0.52),'Hips')
    bone(f'Calf.{s}',(sx*0.10,0,0.52),(sx*0.10,0,0.08),f'Thigh.{s}')
    bone(f'Foot.{s}',(sx*0.10,0,0.08),(sx*0.10,-0.14,0.02),f'Calf.{s}')
bpy.ops.object.mode_set(mode='OBJECT')
# --- weights: join the skin parts into one Body, auto-weight it, then TRANSFER those
#     weights to every garment/accessory so sleeves, slippers, eyes and lashes follow the limbs ---
skin=[o for o in meshes if o.name in ('model_0','model_1','model_3','model_5')]
others=[o for o in meshes if o not in skin and o.name not in ('model_6','model_7')]  # no slippers in bed
for o in [o for o in meshes if o.name in ('model_6','model_7')]: bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.object.select_all(action='DESELECT')
for o in skin: o.select_set(True)
bpy.context.view_layer.objects.active=skin[0]; bpy.ops.object.join(); body=bpy.context.active_object; body.name='Body'
# decimate the very dense gown for mobile
gownobj=[o for o in others if o.name=='model_8'][0]
dec=gownobj.modifiers.new('Dec','DECIMATE'); dec.ratio=0.45
bpy.context.view_layer.objects.active=gownobj; bpy.ops.object.modifier_apply(modifier='Dec')
bpy.ops.object.select_all(action='DESELECT'); body.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
for o in others:
    dt=o.modifiers.new('Weights','DATA_TRANSFER'); dt.object=body; dt.use_vert_data=True; dt.data_types_verts={'VGROUP_WEIGHTS'}
    dt.vert_mapping='NEAREST'; dt.layers_vgroup_select_src='ALL'; dt.layers_vgroup_select_dst='NAME'
    bpy.context.view_layer.objects.active=o; bpy.ops.object.datalayout_transfer(modifier='Weights'); bpy.ops.object.modifier_apply(modifier='Weights')
    o.parent=arm; am=o.modifiers.new('Armature','ARMATURE'); am.object=arm
meshes=[body]+others
# --- pose: lying on back, semi-recumbent, arms at sides ---
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE'); pb=arm.pose.bones
def rot_world(name, axis, deg):
    b=pb[name]; head=b.head.copy()
    R=Matrix.Translation(head) @ Matrix.Rotation(math.radians(deg),4,axis) @ Matrix.Translation(-head)
    b.matrix = R @ b.matrix; bpy.context.view_layer.update()
ARM_DOWN=float(args[4]) if len(args)>4 else 78
ARM_BACK=float(args[5]) if len(args)>5 else 8
rot_world('UpperArm.L','Y', ARM_DOWN); rot_world('UpperArm.R','Y',-ARM_DOWN)      # T-pose -> down along the body
rot_world('UpperArm.L','X', ARM_BACK); rot_world('UpperArm.R','X', ARM_BACK)      # into the mattress plane
rot_world('UpperArm.L','Z', 4); rot_world('UpperArm.R','Z',-4)
rot_world('LowerArm.L','X', 6); rot_world('LowerArm.R','X', 6)
for name,frac in (('Spine',0.35),('Chest',0.35),('UpperChest',0.30)): rot_world(name,'X',TORSO_BEND*frac)  # this model faces -Y: +X = lean back = semi-recumbent when lying
rot_world('Head','X', -TORSO_BEND*0.5)
rot_world('Thigh.L','Z',4); rot_world('Thigh.R','Z',-4)
STUDIO = os.environ.get('QANIVA_POSE_STUDIO')  # path of the .blend to write instead of baking/exporting
if STUDIO:
    bpy.ops.object.mode_set(mode='OBJECT')
    # Bed proxies at Qaniva's environment dimensions (metres). Patient lies with head toward +Y here.
    def box(name, loc, size, rot=(0,0,0)):
        bpy.ops.mesh.primitive_cube_add(location=loc); b=bpy.context.active_object; b.name=name
        b.scale=(size[0]/2,size[1]/2,size[2]/2); b.rotation_euler=rot; b.display_type='WIRE'; return b
    box('Bed_Mattress', (0,0.0,-0.06), (0.90,2.10,0.12))
    box('Bed_Backrest', (0,0.78,0.16), (0.90,0.62,0.10), (math.radians(32),0,0))
    box('Bed_RailL', (-0.50,0.10,0.14), (0.04,1.50,0.22)); box('Bed_RailR', (0.50,0.10,0.14), (0.04,1.50,0.22))
    # Lay the rig on its back: this model faces -Y standing; X-90 turns the face up, feet toward -Y.
    arm.rotation_euler=(math.radians(-90),0,0); arm.location=(0,-0.85,0.02)
    for o in meshes: o.hide_select=False
    bpy.context.view_layer.objects.active=arm; arm.select_set(True)
    scene=bpy.context.scene; scene.render.engine='BLENDER_WORKBENCH'
    bpy.ops.wm.save_as_mainfile(filepath=STUDIO)
    print(f"STUDIO saved {STUDIO}")
    sys.exit(0)
bpy.ops.object.mode_set(mode='OBJECT')
for o in meshes:
    bpy.context.view_layer.objects.active=o
    for m in list(o.modifiers):
        if m.type=='ARMATURE':
            bpy.ops.object.modifier_copy(modifier=m.name); bpy.ops.object.modifier_apply(modifier=o.modifiers[-1].name)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode='OBJECT')
# --- previews: lay the figure on its back (rotate everything -90° about X so the face looks up) ---
scene=bpy.context.scene
scene.render.engine='BLENDER_WORKBENCH'; scene.display.shading.light='STUDIO'; scene.display.shading.color_type='TEXTURE'
scene.render.resolution_x=700; scene.render.resolution_y=900
def render(name, loc, rot):
    cam=bpy.data.objects.new(name, bpy.data.cameras.new(name)); scene.collection.objects.link(cam)
    cam.location=loc; cam.rotation_euler=rot; scene.camera=cam; scene.render.filepath=PREVIEW+'-'+name+'.png'; bpy.ops.render.render(write_still=True)
render('front', (0,-3.2,1.1), (math.radians(85),0,0))
render('threequarter', (1.8,-2.4,1.6), (math.radians(72),0,math.radians(36)))
# --- export (standing rest orientation; Unity lays it down like the previous rig) ---
for o in bpy.data.objects: o.select_set(o.type in ('MESH','ARMATURE'))
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=False)
tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
print(f"EXPORTED {OUT} tris={tris} bones={len(arm.data.bones)}")
