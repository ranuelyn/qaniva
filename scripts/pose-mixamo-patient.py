# Pose the Mixamo-rigged patient (standing frame) for the bed: arms down and settled against the
# back plane (= mattress once Unity lays the model down), semi-recumbent torso; bake; export.
# Usage: Blender -b --python scripts/pose-mixamo-patient.py -- <in.fbx> <out.fbx> [torsoBend]
import bpy, sys, math
from mathutils import Matrix
src,out=sys.argv[sys.argv.index('--')+1],sys.argv[sys.argv.index('--')+2]
BEND=float(sys.argv[sys.argv.index('--')+3]) if len(sys.argv)>sys.argv.index('--')+3 else 26.0
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=src)
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; meshes=[o for o in bpy.data.objects if o.type=='MESH']
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE'); pb=arm.pose.bones
B=lambda n: pb['mixamorig:'+n]
def rot(b, axis, deg):
    head=b.head.copy(); R=Matrix.Translation(head)@Matrix.Rotation(math.radians(deg),4,axis)@Matrix.Translation(-head)
    b.matrix=R@b.matrix; bpy.context.view_layer.update()
def wy(b): return (arm.matrix_world@b.tail).y
# body back plane (mattress) in the standing frame
# facing: eyes are in front of the head bone → sign of (eye_y - head_y) is the FRONT direction
eye_ys=[(o.matrix_world@v.co).y for o in meshes for pi,v in [(pl.material_index,v) for pl in o.data.polygons for v in [o.data.vertices[i] for i in pl.vertices]] if o.data.materials[pi].name=='Eyes'][:2000]
head_y=(arm.matrix_world@B('Head').head).y
front=1.0 if (sum(eye_ys)/len(eye_ys))>head_y else -1.0
ys=[(o.matrix_world@v.co).y for o in meshes for v in o.data.vertices if 0.9<(o.matrix_world@v.co).z<1.4]
back_plane = (min(ys) if front>0 else max(ys))          # the surface the patient lies on
print('facing', '+Y' if front>0 else '-Y', 'back plane y=%.3f'%back_plane)
REST=0.045
# torso lean back = semi-recumbent once lying
for n,f in (('Spine',0.35),('Spine1',0.35),('Spine2',0.30)): rot(B(n),'X',-front*BEND*f)
rot(B('Head'),'X',front*BEND*0.5)
# arms: T-pose -> down along the body
# T-pose -> arms down. Bone matrices live in ARMATURE space and the FBX armature is
# rotated 90° about X, so armature 'Z' is the world vertical axis: swing about 'Z'.
def arm_down(side, sign):
    sz=(arm.matrix_world@B(side+'Arm').head).z
    rot(B(side+'Arm'),'Z',sign*78)
    if (arm.matrix_world@B(side+'Hand').tail).z > sz: rot(B(side+'Arm'),'Z',-sign*156)
    rot(B(side+'Arm'),'Y',26 if side=='Left' else -26)   # slight spread away from the hips (armature Y = world depth)
    hz=(arm.matrix_world@B(side+'Hand').tail).z
    if hz > sz-0.3: rot(B(side+'Arm'),'Y',-52 if side=='Left' else 52)  # spread went the wrong way
    print(side,'hand z %.2f (shoulder %.2f) x %.2f'%((arm.matrix_world@B(side+'Hand').tail).z, sz,(arm.matrix_world@B(side+'Hand').tail).x))
arm_down('Left',1); arm_down('Right',-1)
# settle arm chain onto the back plane: slope-based solve (measure dy per degree, jump, refine)
def solve(bone, target, axis='X'):
    for _ in range(6):
        y0=wy(bone); err=target-y0
        if abs(err)<0.003: break
        rot(bone,axis,1.0); dy=wy(bone)-y0
        if abs(dy)<1e-5: break
        rot(bone,axis,max(-25.0,min(25.0,err/dy))-1.0)
for side in ('Left','Right'):
    solve(B(side+'Arm'), back_plane+front*REST)
    solve(B(side+'ForeArm'), back_plane+front*REST)
    solve(B(side+'Hand'), back_plane+front*REST*0.75)
    print(side,'elbow y %.3f wrist y %.3f hand y %.3f (plane %.3f)'%(wy(B(side+'Arm')),wy(B(side+'ForeArm')),wy(B(side+'Hand')),back_plane))
rot(B('LeftUpLeg'),'Z',4); rot(B('RightUpLeg'),'Z',-4)
bpy.ops.object.mode_set(mode='OBJECT')
for o in meshes:
    bpy.context.view_layer.objects.active=o
    for m in list(o.modifiers):
        if m.type=='ARMATURE': bpy.ops.object.modifier_copy(modifier=m.name); bpy.ops.object.modifier_apply(modifier=o.modifiers[-1].name)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode='OBJECT')
for o in bpy.data.objects: o.select_set(o.type in ('MESH','ARMATURE'))
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=False)
print('EXPORTED',out)
