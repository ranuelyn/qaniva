# Deterministic "settle onto the mattress": iteratively rotate the arm chain so the
# forearm and hand rest ON the mattress top (z = 0 in the studio scene), never inside it.
# Usage: Blender -b art/patient-pose-studio.blend --python scripts/settle-patient-on-bed.py [-- <save.blend>]
import bpy, sys, math
from mathutils import Matrix
argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
save=argv[0] if argv else bpy.data.filepath
REST=0.045   # target height of the limb axis above the mattress (≈ half forearm thickness)
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE'); pb=arm.pose.bones
def wz(b, tail=True): return (arm.matrix_world@(b.tail if tail else b.head)).z
def rot(name, axis_world, deg):
    b=pb[name]; head=b.head.copy()
    R=Matrix.Translation(head)@Matrix.Rotation(math.radians(deg),4,axis_world)@Matrix.Translation(-head)
    b.matrix=R@b.matrix; bpy.context.view_layer.update()
# Rotations are expressed in ARMATURE space: the rig is rotated -90° about X, so armature-local
# X is still the left-right axis; a rotation about local X lifts/lowers the arm chain.
def settle_side(s):
    ua,la,ha=f'UpperArm.{s}',f'LowerArm.{s}',f'Hand.{s}'
    # 1) upper arm: lift until the elbow is on the mattress plane
    for _ in range(80):
        z=wz(pb[ua]); 
        if z>=REST: break
        rot(ua,'X',0.5 if wz(pb[ua])<REST else -0.5)
        if wz(pb[ua])<z: rot(ua,'X',-1.0)   # wrong direction → flip
    # 2) forearm: lift/lower until the wrist rests on the plane
    for _ in range(120):
        z=wz(pb[la]); err=REST-z
        if abs(err)<0.004: break
        step=0.5 if err>0 else -0.5
        before=z; rot(la,'X',step)
        if (wz(pb[la])-before)*err<0: rot(la,'X',-2*step)
    # 3) hand: flat on the sheet
    for _ in range(80):
        z=wz(pb[ha]); err=REST*0.7-z
        if abs(err)<0.004: break
        step=0.5 if err>0 else -0.5
        before=z; rot(ha,'X',step)
        if (wz(pb[ha])-before)*err<0: rot(ha,'X',-2*step)
    print(f"{s}: elbow {wz(pb[ua])*100:.1f} cm, wrist {wz(pb[la])*100:.1f} cm, fingertips {wz(pb[ha])*100:.1f} cm above mattress")
for s in ('L','R'): settle_side(s)
# legs: rest the heel on the sheet (direction-detecting, like the arms)
for s in ('L','R'):
    calf,foot=f'Calf.{s}',f'Foot.{s}'
    for _ in range(60):
        z=wz(pb[foot],tail=False); err=REST*0.6-z
        if abs(err)<0.005: break
        step=0.4 if err>0 else -0.4
        before=z; rot(calf,'X',step)
        if (wz(pb[foot],tail=False)-before)*err<0: rot(calf,'X',-2*step)
    print(f"{s}: heel {wz(pb[foot],tail=False)*100:.1f} cm above mattress")
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.wm.save_as_mainfile(filepath=save)
print("SETTLED", save)
