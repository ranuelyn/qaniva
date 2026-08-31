# Qaniva adult_rigged_v1 patient generator — run INSIDE Blender (4.x/5.x):
#   /Applications/Blender.app/Contents/MacOS/Blender -b --python scripts/generate-patient-blender.py
#
# Builds a stylized low-poly adult patient (skin-modifier humanoid, ~10k tris)
# with a 17-bone humanoid armature (automatic weights + rigid fallbacks), a
# hospital-gown material split (PatientGownMat / PatientSkinMat / PatientHairMat)
# and closed-eye face details, then exports
#   unity/QanivaSimulation/Assets/Qaniva/Art/Patients/adult_rigged_v1.fbx
#
# First-party asset: zero external content, zero licensing surface (QAN-020 safe
# temporary rig; see docs/art/asset-manifest.md for the recommended-purchase
# production alternative). Deterministic: same script -> same FBX geometry.
#
# Runtime animation contract: Unity's PatientVisualController drives the Chest
# bone (breathing) and tints materials whose name contains "Skin". Bone names
# (Hips/Spine/Chest/Neck/Head/UpperArm.*/Forearm.*/Hand.*/Thigh.*/Shin.*/Foot.*)
# are part of that contract.

import bpy
from pathlib import Path


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.armatures):
        for d in list(block):
            if d.users == 0:
                block.remove(d)


def make_skin_object(name, verts, edges, radii, roots=(0,)):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, edges, [])
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    skin = obj.modifiers.new("Skin", 'SKIN')
    skin.use_smooth_shade = True
    sub = obj.modifiers.new("Sub", 'SUBSURF')
    sub.levels = 2
    sub.render_levels = 2
    sv = mesh.skin_vertices[0].data
    for i, r in enumerate(radii):
        sv[i].radius = (r, r)
    # the Skin modifier needs one marked root per disconnected island —
    # unrooted islands collapse to near-zero thickness.
    for i in roots:
        sv[i].use_root = True
    return obj


def apply_modifiers(obj):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    for m in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)
    obj.select_set(False)


def make_material(name, color, rough=0.85):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    return m


def add_prim(name, kind, loc, scale, material):
    if kind == 'cube':
        bpy.ops.mesh.primitive_cube_add(location=loc)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(location=loc, segments=24, ring_count=16)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    o.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return o


def make_blanket():
    """Draped blanket sheet modeled in standing space (covers feet->waist once
    the prefab lays the rig supine): center follows the legs, edges fall toward
    the mattress plane. Weighted rigidly to Hips by the caller."""
    import math
    mesh = bpy.data.meshes.new("BlanketMesh")
    obj = bpy.data.objects.new("BlanketMesh", mesh)
    bpy.context.collection.objects.link(obj)
    nx, nz = 17, 25
    xs = [(-0.34 + 0.68 * i / (nx - 1)) for i in range(nx)]
    zs = [(0.015 + 0.945 * j / (nz - 1)) for j in range(nz)]
    verts = []
    for z in zs:
        for x in xs:
            leg_bump = 0.065 * math.exp(-((abs(x) - 0.115) ** 2) / (2 * 0.055 ** 2))
            body = -(0.045 + leg_bump)
            edge = min(1.0, max(0.0, (abs(x) - 0.20) / 0.13))
            y = body * (1 - edge) + 0.03 * edge
            y += 0.006 * math.sin(x * 34) * math.sin(z * 21)  # fabric ripples
            verts.append((x, y, z))
    faces = []
    for j in range(nz - 1):
        for i in range(nx - 1):
            a = j * nx + i
            faces.append((a, a + 1, a + nx + 1, a + nx))
    mesh.from_pydata(verts, [], faces)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    sol = obj.modifiers.new("Sol", 'SOLIDIFY')
    sol.thickness = 0.018
    sub = obj.modifiers.new("Sub", 'SUBSURF')
    sub.levels = 1
    sub.render_levels = 1
    bpy.ops.object.shade_smooth()
    apply_modifiers(obj)
    return obj


def build():
    clear_scene()

    # --- gown body: torso, sleeves to the elbows, gown skirt to the knees ---
    gv = [
        (0, 0, 1.44), (0, 0, 1.36), (0, 0, 1.22), (0, 0, 1.06), (0, 0, 0.94),
        (-0.115, 0, 0.86), (0.115, 0, 0.86),
        (-0.115, 0, 0.52), (0.115, 0, 0.52),
        (-0.185, 0, 1.37), (0.185, 0, 1.37),
        (-0.255, 0.02, 1.10), (0.255, 0.02, 1.10),
    ]
    ge = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (4, 6), (5, 7), (6, 8),
          (1, 9), (1, 10), (9, 11), (10, 12)]
    gr = [0.062, 0.128, 0.150, 0.128, 0.138, 0.088, 0.088, 0.072, 0.072,
          0.066, 0.066, 0.054, 0.054]
    gown = make_skin_object("GownBody", gv, ge, gr)

    # --- skin parts: head+neck, forearms+hands, shins+feet ---
    sv = [
        (0, 0, 1.42), (0, 0.005, 1.52), (0, 0.01, 1.60), (0, 0.01, 1.66),
        (-0.248, 0.018, 1.14), (0.248, 0.018, 1.14),
        (-0.295, 0.03, 0.88), (0.295, 0.03, 0.88),
        (-0.305, 0.035, 0.79), (0.305, 0.035, 0.79),
        (-0.115, 0, 0.585), (0.115, 0, 0.585),
        (-0.115, 0, 0.10), (0.115, 0, 0.10),
        (-0.115, -0.055, 0.02), (0.115, -0.055, 0.02),
    ]
    se = [(0, 1), (1, 2), (2, 3), (4, 6), (6, 8), (5, 7), (7, 9),
          (10, 12), (11, 13), (12, 14), (13, 15)]
    sr = [0.054, 0.078, 0.098, 0.075, 0.052, 0.052, 0.044, 0.044,
          0.048, 0.048, 0.066, 0.066, 0.048, 0.048, 0.044, 0.044]
    skinp = make_skin_object("SkinParts", sv, se, sr, roots=(0, 4, 5, 10, 11))

    apply_modifiers(gown)
    apply_modifiers(skinp)

    skin_mat = make_material("PatientSkinMat", (0.68, 0.44, 0.32))
    gown_mat = make_material("PatientGownMat", (0.55, 0.68, 0.72))
    hair_mat = make_material("PatientHairMat", (0.12, 0.10, 0.09))
    blanket_mat = make_material("PatientBlanketMat", (0.40, 0.53, 0.76))
    gown.data.materials.append(gown_mat)
    skinp.data.materials.append(skin_mat)
    blanket = make_blanket()
    blanket.data.materials.append(blanket_mat)

    # face details (closed eyes — the patient is supine and unwell) + hair cap
    details = [
        add_prim("EyeL", 'cube', (-0.033, -0.076, 1.612), (0.014, 0.0025, 0.003), hair_mat),
        add_prim("EyeR", 'cube', (0.033, -0.076, 1.612), (0.014, 0.0025, 0.003), hair_mat),
        add_prim("Nose", 'sphere', (0, -0.088, 1.585), (0.012, 0.014, 0.018), skin_mat),
        add_prim("Mouth", 'cube', (0, -0.080, 1.548), (0.015, 0.0025, 0.002), hair_mat),
        add_prim("EarL", 'sphere', (-0.090, 0.008, 1.592), (0.010, 0.018, 0.024), skin_mat),
        add_prim("EarR", 'sphere', (0.090, 0.008, 1.592), (0.010, 0.018, 0.024), skin_mat),
        add_prim("Hair", 'sphere', (0, 0.028, 1.616), (0.101, 0.100, 0.086), hair_mat),
    ]
    bpy.ops.object.select_all(action='DESELECT')
    for o in details + [skinp]:
        o.select_set(True)
    bpy.context.view_layer.objects.active = skinp
    bpy.ops.object.join()

    # --- armature (Unity-facing bone-name contract) ---
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.object.armature_add(location=(0, 0, 0))
    arm = bpy.context.active_object
    arm.name = "PatientArmature"
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm.data.edit_bones
    root = eb[0]
    root.name = "Hips"
    root.head = (0, 0, 0.94)
    root.tail = (0, 0, 1.06)

    def bone(name, head, tail, parent, connected=False):
        b = eb.new(name)
        b.head = head
        b.tail = tail
        b.parent = eb[parent]
        b.use_connect = connected
        return b

    bone("Spine", (0, 0, 1.06), (0, 0, 1.22), "Hips", True)
    bone("Chest", (0, 0, 1.22), (0, 0, 1.44), "Spine", True)
    bone("Neck", (0, 0, 1.44), (0, 0.005, 1.53), "Chest", True)
    bone("Head", (0, 0.005, 1.53), (0, 0.01, 1.70), "Neck", True)
    for side, sx in (("L", -1), ("R", 1)):
        bone(f"UpperArm.{side}", (sx * 0.185, 0, 1.37), (sx * 0.255, 0.02, 1.10), "Chest")
        bone(f"Forearm.{side}", (sx * 0.255, 0.02, 1.10), (sx * 0.295, 0.03, 0.88), f"UpperArm.{side}", True)
        bone(f"Hand.{side}", (sx * 0.295, 0.03, 0.88), (sx * 0.310, 0.04, 0.76), f"Forearm.{side}", True)
        bone(f"Thigh.{side}", (sx * 0.115, 0, 0.90), (sx * 0.115, 0, 0.52), "Hips")
        bone(f"Shin.{side}", (sx * 0.115, 0, 0.52), (sx * 0.115, 0, 0.10), f"Thigh.{side}", True)
        bone(f"Foot.{side}", (sx * 0.115, 0, 0.10), (sx * 0.115, -0.13, 0.05), f"Shin.{side}", True)
    bpy.ops.object.mode_set(mode='OBJECT')

    # automatic weights + deterministic fallback for verts the heat solve missed
    bpy.ops.object.select_all(action='DESELECT')
    gown.select_set(True)
    skinp.select_set(True)
    blanket.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    # the blanket rides the Hips rigidly (it is bedding, not anatomy)
    bvg = blanket.vertex_groups
    for g in list(bvg):
        bvg.remove(g)
    hips_grp = bvg.new(name="Hips")
    hips_grp.add(list(range(len(blanket.data.vertices))), 1.0, 'REPLACE')

    mids = {b.name: arm.matrix_world @ ((b.head_local + b.tail_local) / 2)
            for b in arm.data.bones}
    for obj in (gown, skinp):
        vg = obj.vertex_groups
        for v in obj.data.vertices:
            total = sum(g.weight for g in v.groups)
            wpos = obj.matrix_world @ v.co
            if total < 1e-4:
                nearest = min(mids, key=lambda n: (mids[n] - wpos).length)
                grp = vg.get(nearest) or vg.new(name=nearest)
                grp.add([v.index], 1.0, 'REPLACE')
            elif wpos.z > 1.50:
                for g in list(v.groups):
                    vg[g.group].remove([v.index])
                grp = vg.get("Head") or vg.new(name="Head")
                grp.add([v.index], 1.0, 'REPLACE')

    # --- export ---
    out = Path(__file__).resolve().parent.parent / \
        "unity/QanivaSimulation/Assets/Qaniva/Art/Patients/adult_rigged_v1.fbx"
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    gown.select_set(True)
    skinp.select_set(True)
    blanket.select_set(True)
    arm.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(out),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        add_leaf_bones=False,
        bake_anim=False,
        path_mode='COPY',
    )
    tris = sum(
        sum(len(p.vertices) - 2 for p in o.data.polygons)
        for o in (gown, skinp))
    print(f"exported {out} — approx {tris} tris, {len(arm.data.bones)} bones")


build()
