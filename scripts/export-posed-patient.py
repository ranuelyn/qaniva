# Bake the CURRENT pose of the pose-studio .blend and export it for Unity.
# Usage: Blender -b art/patient-pose-studio.blend --python scripts/export-posed-patient.py -- <out.fbx>
import bpy, sys, math
out=sys.argv[sys.argv.index('--')+1]
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
meshes=[o for o in bpy.data.objects if o.type=='MESH' and o.parent==arm]
# report penetration of hands/feet/elbows into the mattress plane (z<0) before baking
bpy.context.view_layer.update()
for pb in arm.pose.bones:
    if any(k in pb.name for k in ('Hand','LowerArm','Foot','Head')):
        z=(arm.matrix_world@pb.tail).z
        if z < 0.0: print(f"WARN {pb.name} tail is {abs(z)*100:.0f} cm below the mattress top")
# bake: apply armature modifiers, then pose as rest; return the rig to the standing frame Unity expects
bpy.ops.object.mode_set(mode='OBJECT')
for o in meshes:
    bpy.context.view_layer.objects.active=o
    for m in list(o.modifiers):
        if m.type=='ARMATURE':
            bpy.ops.object.modifier_copy(modifier=m.name); bpy.ops.object.modifier_apply(modifier=o.modifiers[-1].name)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode='OBJECT')
arm.rotation_euler=(0,0,0); arm.location=(0,0,0)
for o in bpy.data.objects: o.select_set(o.type in ('MESH','ARMATURE') and not o.name.startswith('Bed_'))
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=False)
print(f"EXPORTED {out}")
