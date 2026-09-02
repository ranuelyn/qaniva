# Mixamo-rigged Hospital Patient (T-pose, with skin) -> Unity-ready FBX:
#   * gown split into its own mesh ("Gown", decimated) so Unity can drive it with Cloth
#   * materials renamed to the Qaniva contract (Skin_* for tint), textures copied
# Usage: Blender -b --python scripts/prepare-mixamo-patient.py -- <mixamo_tpose.fbx> <out.fbx>
import bpy, sys
src, out = sys.argv[sys.argv.index('--')+1], sys.argv[sys.argv.index('--')+2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
body=[o for o in bpy.data.objects if o.type=='MESH'][0]
rename={'model_0_mat':'Skin_Arms','model_1_mat':'Skin_Legs','model_3_mat':'Skin_Face','model_5_mat':'Skin_Torso','model_8_mat':'Gown',
        'model_4_mat':'Eyes','model_9_mat':'Bracelet','model_2_mat':'Mouth','model_10_mat':'Eyelashes','model_11_mat':'Eyebrows'}
for m in bpy.data.materials:
    if m.name in rename: m.name=rename[m.name]
# split the gown out by material
bpy.context.view_layer.objects.active=body; body.select_set(True)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT')
gi=[i for i,m in enumerate(body.data.materials) if m and m.name=='Gown'][0]
body.active_material_index=gi; bpy.ops.object.material_slot_select(); bpy.ops.mesh.separate(type='SELECTED'); bpy.ops.object.mode_set(mode='OBJECT')
gown=[o for o in bpy.data.objects if o.type=='MESH' and o is not body][0]; gown.name='Gown'; body.name='Body'
# lighter gown for the cloth solver
bpy.context.view_layer.objects.active=gown
dec=gown.modifiers.new('Dec','DECIMATE'); dec.ratio=0.35; bpy.ops.object.modifier_apply(modifier='Dec')
for o in (body,gown):
    for p in o.data.materials: pass
print('GOWN verts',len(gown.data.vertices),'BODY verts',len(body.data.vertices))
for o in bpy.data.objects: o.select_set(o.type in ('MESH','ARMATURE'))
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=False)
print('EXPORTED',out)
