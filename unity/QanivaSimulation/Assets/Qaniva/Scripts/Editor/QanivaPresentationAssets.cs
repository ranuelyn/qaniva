using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Qaniva.Presentation;
using Qaniva.Simulation.Core;

namespace Qaniva.EditorTools
{
    /// <summary>
    /// Builds the reusable 3D presentation assets (materials + patient, bedside
    /// monitor and ED-resus environment prefabs) entirely from Unity primitives —
    /// authored as code so the whole art foundation is reproducible headlessly,
    /// license-clean, and tiny in the repo. Generated prefabs are committed; this
    /// builder only needs re-running when the presentation foundation changes.
    ///
    /// Run: -executeMethod Qaniva.EditorTools.QanivaPresentationAssets.CreateAll
    /// Preview: ...QanivaPresentationAssets.CapturePreview -previewOut <png path>
    /// </summary>
    public static class QanivaPresentationAssets
    {
        private const string MaterialsDir = "Assets/Qaniva/Materials";
        private const string EnvDir = "Assets/Qaniva/Resources/Qaniva/Environments";
        private const string PatientDir = "Assets/Qaniva/Resources/Qaniva/Patients";
        private const string PropsDir = "Assets/Qaniva/Resources/Qaniva/Props";

        public static void CreateAll()
        {
            Directory.CreateDirectory(MaterialsDir);
            Directory.CreateDirectory(EnvDir);
            Directory.CreateDirectory(PatientDir);
            Directory.CreateDirectory(PropsDir);

            CreateMaterials();
            var patient = CreatePatientPrefab();
            var rigged = CreateRiggedPatientPrefab();
            var monitor = CreateMonitorPrefab();
            CreateEnvironmentPrefab(monitor);
            AssetDatabase.SaveAssets();
            Debug.Log("[QanivaPresentationAssets] all presentation assets written");
            UnityEngine.Object.DestroyImmediate(patient);
            if (rigged != null)
            {
                UnityEngine.Object.DestroyImmediate(rigged);
            }
        }

        // --- materials -----------------------------------------------------

        private static Material Mat(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/{name}.mat");

        private static void CreateMaterials()
        {
            void Make(string name, Color color, float metallic = 0f, float smooth = 0.35f, Color? emission = null)
            {
                var path = $"{MaterialsDir}/{name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.SetColor("_BaseColor", color);
                mat.color = color;
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smooth);
                if (emission.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emission.Value);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                EditorUtility.SetDirty(mat);
            }

            Make("HospitalWall", new Color(0.82f, 0.81f, 0.77f), 0f, 0.15f);
            Make("HospitalFloor", new Color(0.55f, 0.62f, 0.61f), 0f, 0.45f);
            Make("Metal", new Color(0.75f, 0.77f, 0.80f), 0.85f, 0.65f);
            Make("PlasticDark", new Color(0.16f, 0.18f, 0.20f), 0f, 0.40f);
            Make("PlasticLight", new Color(0.74f, 0.77f, 0.79f), 0f, 0.40f);
            Make("Mattress", new Color(0.18f, 0.47f, 0.44f), 0f, 0.25f);
            Make("Blanket", new Color(0.40f, 0.53f, 0.76f), 0f, 0.15f);
            Make("Gown", new Color(0.50f, 0.62f, 0.70f), 0f, 0.20f);
            Make("Skin", new Color(0.78f, 0.60f, 0.48f), 0f, 0.22f);
            Make("ScreenDark", new Color(0.03f, 0.05f, 0.06f), 0f, 0.70f);
            Make("AccentTeal", new Color(0.12f, 0.43f, 0.55f), 0f, 0.35f);
            Make("IvBag", new Color(0.85f, 0.90f, 0.92f, 1f), 0f, 0.6f);
            Make("CeilingPanel", Color.white, 0f, 0.2f, new Color(1.6f, 1.6f, 1.55f));
            Make("Hair", new Color(0.14f, 0.12f, 0.11f), 0f, 0.45f);
            Make("Eyes", new Color(0.35f, 0.25f, 0.18f), 0f, 0.75f);

            // Owner-supplied "Hospital Patient" textures (Art/Patients/Textures/hp): one URP Lit
            // material per part. Skin parts carry "Skin" in the name for the tint contract.
            void MakeTextured(string name, string texFile, float smooth)
            {
                Make(name, Color.white, 0f, smooth);
                var m = Mat(name);
                var texPath = $"Assets/Qaniva/Art/Patients/Textures/hp/{texFile}";
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (m != null && tex != null)
                {
                    m.SetTexture("_BaseMap", tex);
                    m.mainTexture = tex;
                    EditorUtility.SetDirty(m);
                }
            }
            MakeTextured("Skin_Arms", "Arms.png", 0.28f);
            MakeTextured("Skin_Legs", "Legs.png", 0.28f);
            MakeTextured("Skin_Face", "Face.png", 0.30f);
            MakeTextured("Skin_Torso", "Torso.png", 0.28f);
            MakeTextured("GownTex", "gown.png", 0.12f);
            MakeTextured("Slippers", "slippers.png", 0.25f);
            MakeTextured("EyesTex", "Eyes.png", 0.70f);
            MakeTextured("Mouth", "Mouth.png", 0.40f);
            MakeTextured("Eyelashes", "Eyelashes.png", 0.20f);
            MakeTextured("Bracelet", "bracelet.png", 0.30f);

            // Textured skin for the Quaternius-based patient. The material name must
            // contain "Skin" — PatientVisualController tints by that contract.
            Make("SkinTextured", new Color(0.92f, 0.82f, 0.74f), 0f, 0.30f);
            var skinTex = Mat("SkinTextured");
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Qaniva/Art/Patients/Textures/T_Ubc_Male_BaseColor.png");
            var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Qaniva/Art/Patients/Textures/T_Superhero_Male_Normal.png");
            if (skinTex != null && baseMap != null)
            {
                skinTex.SetTexture("_BaseMap", baseMap);
                skinTex.mainTexture = baseMap;
                if (normalMap != null)
                {
                    skinTex.SetTexture("_BumpMap", normalMap);
                    skinTex.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(skinTex);
            }
        }

        // --- helpers ------------------------------------------------------

        private static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, string mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = Mat(mat);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Cyl(string name, Transform parent, Vector3 pos, float radius, float height, string mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = Mat(mat);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 pos, float radius, string mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * radius * 2f;
            go.GetComponent<Renderer>().sharedMaterial = Mat(mat);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Capsule(string name, Transform parent, Vector3 pos, float radius, float height, string mat, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            go.transform.localEulerAngles = euler;
            go.GetComponent<Renderer>().sharedMaterial = Mat(mat);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Anchor(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
        }

        private static TextMesh Text(string name, Transform parent, Vector3 pos, string text, float worldHeight, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            // Text faces -Z of the screen (screen's forward is toward the camera).
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = worldHeight * 10f / 64f;
            tm.color = color;
            tm.fontStyle = style;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            return tm;
        }

        // --- patient -----------------------------------------------------

        private static GameObject CreatePatientPrefab()
        {
            var root = new GameObject("adult_neutral_v1");

            // Supine on the mattress; +Z = toward the head end of the bed.
            Sphere("Head", root.transform, new Vector3(0f, 0.10f, 0.80f), 0.115f, "Skin");
            Box("Pillow", root.transform, new Vector3(0f, 0.005f, 0.94f), new Vector3(0.40f, 0.05f, 0.16f), "PlasticLight");
            Box("Chest", root.transform, new Vector3(0f, 0.045f, 0.42f), new Vector3(0.42f, 0.17f, 0.52f), "Gown");
            Box("Pelvis", root.transform, new Vector3(0f, 0.02f, 0.02f), new Vector3(0.40f, 0.14f, 0.30f), "Gown");
            Capsule("LegLeft", root.transform, new Vector3(-0.11f, 0.01f, -0.48f), 0.085f, 0.85f, "Blanket", new Vector3(90f, 0f, 0f));
            Capsule("LegRight", root.transform, new Vector3(0.11f, 0.01f, -0.48f), 0.085f, 0.85f, "Blanket", new Vector3(90f, 0f, 0f));
            Box("Blanket", root.transform, new Vector3(0f, 0.045f, -0.38f), new Vector3(0.52f, 0.09f, 0.72f), "Blanket");
            Capsule("ArmLeft", root.transform, new Vector3(-0.27f, 0.02f, 0.34f), 0.055f, 0.52f, "Gown", new Vector3(90f, 0f, 0f));
            Capsule("ArmRight", root.transform, new Vector3(0.27f, 0.02f, 0.34f), 0.055f, 0.52f, "Gown", new Vector3(90f, 0f, 0f));
            Sphere("HandLeft", root.transform, new Vector3(-0.27f, 0.02f, 0.04f), 0.06f, "Skin");
            Sphere("HandRight", root.transform, new Vector3(0.27f, 0.02f, 0.04f), 0.06f, "Skin");

            // Generic future-procedure anchors (cheap now, awkward to add later).
            Anchor("AnchorHead", root.transform, new Vector3(0f, 0.15f, 0.80f));
            Anchor("AnchorChest", root.transform, new Vector3(0f, 0.15f, 0.42f));
            Anchor("AnchorLeftArm", root.transform, new Vector3(-0.30f, 0.05f, 0.30f));
            Anchor("AnchorRightArm", root.transform, new Vector3(0.30f, 0.05f, 0.30f));

            root.AddComponent<PatientVisualController>();

            var path = $"{PatientDir}/adult_neutral_v1.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[QanivaPresentationAssets] wrote {path}");
            return root; // caller destroys
        }

        // --- rigged patient (QAN-020) -------------------------------------

        /// <summary>
        /// Wraps the first-party Blender-generated rig
        /// (Assets/Qaniva/Art/Patients/adult_rigged_v1.fbx, built by
        /// scripts/generate-patient-blender.py) in the SAME patient prefab
        /// contract as the primitive patient: root carries PatientVisualController
        /// + procedure anchors; +Z = toward the head end of the bed; the model's
        /// materials are remapped onto the shared URP set (Skin/Gown/Hair) so the
        /// controller's skin tinting works identically.
        /// </summary>
        private static GameObject CreateRiggedPatientPrefab()
        {
            // CC0 Quaternius Universal Base Character, processed by scripts (see
            // Art/Patients/LICENSE-quaternius.txt): Qaniva bone names, gown material,
            // baked semi-recumbent pose, hair. Replaces the primitive first-party rig.
            const string fbxPath = "Assets/Qaniva/Art/Patients/mixamo/adult_mx_v1_posed.fbx";
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[QanivaPresentationAssets] {fbxPath} missing — run scripts/generate-patient-blender.py in Blender first. Keeping the existing rigged prefab (if any).");
                return null;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.animationType = ModelImporterAnimationType.Generic; // procedural bone animation, no clips yet
            importer.isReadable = true; // Cloth needs readable gown geometry
            foreach (var (src, dst) in new[]
            {
                ("Skin_Arms", "Skin_Arms"),
                ("Skin_Legs", "Skin_Legs"),
                ("Skin_Face", "Skin_Face"),
                ("Skin_Torso", "Skin_Torso"),
                ("Gown", "GownTex"),
                ("Slippers", "Slippers"),
                ("Eyes", "EyesTex"),
                ("model_8_mat", "GownTex"),
                ("Mouth", "Mouth"),
                ("Eyelashes", "Eyelashes"),
                ("Eyebrows", "Eyelashes"),
                ("Bracelet", "Bracelet"),
            })
            {
                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), src), Mat(dst));
            }
            importer.SaveAndReimport();

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var root = new GameObject("adult_rigged_v1");

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            // Supine: X+90 lays the imported rig down with the head toward +Z
            // (bed-head) but face-down; the extra 180° roll about the body's long
            // axis turns it onto its back. Empirical values verified via PlayMode
            // captures (the Blender FBX import bakes its own axis compensation).
            // The supplied patient faces -Y in Blender: X+90 lays it face-down, the extra
            // 180° roll about the body axis turns it onto its back (verified on device).
            model.transform.localRotation =
                Quaternion.AngleAxis(180f, Vector3.forward) * Quaternion.Euler(90f, 0f, 0f);
            // Feet toward the bed's foot end; body rests on the mattress plane.
            model.transform.localPosition = new Vector3(0f, 0.14f, -0.85f);

            // Semi-recumbent (head of bed ~30°): bend the torso up at Spine/Chest so
            // the face and chest read to the three-quarter camera. Pure pose — the
            // breathing/tint controller keys off bone NAMES, which are unchanged.
            float torsoBend = GetArgFloat("-torsoBend", 0f); // legacy primitive rig: keep supine (pose is baked into the new asset)
            foreach (var bone in model.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "Spine")
                {
                    bone.localRotation *= Quaternion.Euler(-torsoBend * 0.55f, 0f, 0f);
                }
                else if (bone.name == "Chest")
                {
                    bone.localRotation *= Quaternion.Euler(-torsoBend * 0.45f, 0f, 0f);
                }
                else if (bone.name == "Head")
                {
                    bone.localRotation *= Quaternion.Euler(torsoBend * 0.35f, 0f, 0f);
                }
            }

            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in renderer.sharedMaterials)
                {
                    Debug.Log($"[QanivaPresentationAssets] rigged patient material: {renderer.name} -> {(m == null ? "NULL" : m.name)}");
                }
            }

            // Gown → Unity Cloth: shoulders/chest pinned, hem free, capsule colliders on the
            // torso and thighs so the hem drapes over the body instead of following bones.
            SetupGownCloth(model);

            // Pillow stays a primitive; the draped blanket is part of the FBX now
            // (BlanketMesh, modeled in Blender and weighted to the Hips bone).
            Box("Pillow", root.transform, new Vector3(0f, 0.05f, 0.92f), new Vector3(0.42f, 0.07f, 0.20f), "PlasticLight");

            Anchor("AnchorHead", root.transform, new Vector3(0f, 0.20f, 0.80f));
            Anchor("AnchorChest", root.transform, new Vector3(0f, 0.22f, 0.35f));
            Anchor("AnchorLeftArm", root.transform, new Vector3(-0.30f, 0.10f, 0.15f));
            Anchor("AnchorRightArm", root.transform, new Vector3(0.30f, 0.10f, 0.15f));

            root.AddComponent<PatientVisualController>();

            var path = $"{PatientDir}/adult_rigged_v1.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[QanivaPresentationAssets] wrote {path}");
            return root;
        }

        // --- bedside monitor ---------------------------------------------

        private static GameObject CreateMonitorPrefab()
        {
            var root = new GameObject("BedsideMonitor");

            // Rolling stand + body + screen. Screen faces -Z (toward the camera).
            // Lower than a wall unit so it sits below the vitals strip in the frame.
            Box("StandBase", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.42f, 0.06f, 0.42f), "PlasticDark");
            Cyl("StandPole", root.transform, new Vector3(0f, 0.52f, 0f), 0.028f, 0.98f, "Metal");
            Box("Body", root.transform, new Vector3(0f, 1.16f, 0.015f), new Vector3(0.58f, 0.50f, 0.09f), "PlasticLight");
            Box("Screen", root.transform, new Vector3(0f, 1.16f, -0.035f), new Vector3(0.53f, 0.44f, 0.012f), "ScreenDark");

            // Waveform strip (emissive unlit texture) across the top of the screen.
            var strip = GameObject.CreatePrimitive(PrimitiveType.Quad);
            strip.name = "EcgStrip";
            strip.transform.SetParent(root.transform, false);
            strip.transform.localPosition = new Vector3(0f, 1.30f, -0.045f);
            strip.transform.localScale = new Vector3(0.48f, 0.10f, 1f);
            strip.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            UnityEngine.Object.DestroyImmediate(strip.GetComponent<Collider>());
            var stripMatPath = $"{MaterialsDir}/EcgStrip.mat";
            var stripMat = AssetDatabase.LoadAssetAtPath<Material>(stripMatPath);
            if (stripMat == null)
            {
                stripMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(stripMat, stripMatPath);
            }
            const string stripTexPath = "Assets/Qaniva/Art/Props/ecg_strip.png";
            AssetDatabase.ImportAsset(stripTexPath, ImportAssetOptions.ForceSynchronousImport);
            var stripTex = AssetDatabase.LoadAssetAtPath<Texture2D>(stripTexPath);
            if (stripTex == null)
            {
                Debug.LogWarning("[QanivaPresentationAssets] ecg_strip.png not found — monitor waveform will be blank");
            }
            else
            {
                stripMat.SetTexture("_BaseMap", stripTex);
                stripMat.mainTexture = stripTex;
                stripMat.SetColor("_BaseColor", Color.white);
            }
            EditorUtility.SetDirty(stripMat);
            strip.GetComponent<Renderer>().sharedMaterial = stripMat;
            // Back-to-back twin so the strip reads regardless of the quad's facing.
            var strip2 = GameObject.CreatePrimitive(PrimitiveType.Quad);
            strip2.name = "EcgStripBack";
            strip2.transform.SetParent(root.transform, false);
            strip2.transform.localPosition = new Vector3(0f, 1.30f, -0.0445f);
            strip2.transform.localScale = new Vector3(0.48f, 0.10f, 1f);
            strip2.transform.localEulerAngles = Vector3.zero;
            UnityEngine.Object.DestroyImmediate(strip2.GetComponent<Collider>());
            strip2.GetComponent<Renderer>().sharedMaterial = stripMat;

            // Clinical color coding (monitor convention): HR green, SpO2 cyan, NIBP red-orange, RR yellow.
            var green = new Color(0.35f, 0.95f, 0.55f);
            var cyan = new Color(0.45f, 0.85f, 0.95f);
            var red = new Color(0.98f, 0.35f, 0.30f);
            var yellow = new Color(0.98f, 0.85f, 0.30f);
            var label = new Color(0.65f, 0.70f, 0.72f);
            const float zFace = -0.045f; // just in front of the screen face

            // Labels sit under the (unscaled) monitor root — never under the scaled
            // Screen box, which would distort TextMesh glyphs non-uniformly.
            Text("HrLabel", root.transform, new Vector3(-0.15f, 1.215f, zFace), "NABIZ", 0.03f, label);
            Text("HrValue", root.transform, new Vector3(-0.15f, 1.145f, zFace), "--", 0.085f, green);
            Text("Spo2Label", root.transform, new Vector3(0.15f, 1.215f, zFace), "SpO2", 0.03f, label);
            Text("Spo2Value", root.transform, new Vector3(0.15f, 1.145f, zFace), "--", 0.085f, cyan);
            Text("BpLabel", root.transform, new Vector3(-0.15f, 1.06f, zFace), "TA", 0.03f, label);
            Text("BpValue", root.transform, new Vector3(-0.15f, 1.0f, zFace), "--/--", 0.058f, red);
            Text("RrLabel", root.transform, new Vector3(0.15f, 1.06f, zFace), "SS", 0.03f, label);
            Text("RrValue", root.transform, new Vector3(0.15f, 1.0f, zFace), "--", 0.07f, yellow);
            Text("ClockValue", root.transform, new Vector3(0f, 0.955f, zFace), "00:00", 0.028f, label);

            root.AddComponent<BedsideMonitorView>();

            var path = $"{PropsDir}/BedsideMonitor.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[QanivaPresentationAssets] wrote {path}");
            var result = root;
            return result;
        }


        // --- gown cloth --------------------------------------------------------

        private static Transform FindBone(Transform root, string suffix)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                if (t.name.EndsWith(suffix)) return t;
            }
            return null;
        }

        private static CapsuleCollider Capsule(Transform bone, Transform toward, float radius)
        {
            if (bone == null) return null;
            var go = new GameObject("ClothCollider");
            go.transform.SetParent(bone, false);
            var c = go.AddComponent<CapsuleCollider>();
            c.radius = radius;
            float len = toward != null ? Vector3.Distance(bone.position, toward.position) : 0.25f;
            c.height = len + radius * 2f;
            c.direction = 1; // Y (bone axis for Mixamo rigs)
            c.center = new Vector3(0f, len * 0.5f, 0f);
            c.isTrigger = true;
            return c;
        }

        private static void SetupGownCloth(GameObject model)
        {
            SkinnedMeshRenderer gown = null;
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (r.name == "Gown") gown = r;
            }
            if (gown == null || gown.sharedMesh == null)
            {
                Debug.LogWarning("[QanivaPresentationAssets] no Gown renderer — cloth skipped");
                return;
            }
            var cloth = gown.gameObject.AddComponent<Cloth>();
            var verts = gown.sharedMesh.vertices;
            // pin the top of the gown (above the armpit line), free below; mesh space is the
            // T-pose rest space of the FBX (Y up after import).
            float top = float.MinValue, bottom = float.MaxValue;
            foreach (var v in verts) { top = Mathf.Max(top, v.y); bottom = Mathf.Min(bottom, v.y); }
            float pinLine = top - (top - bottom) * 0.22f;
            var coeffs = new ClothSkinningCoefficient[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                float t = Mathf.InverseLerp(pinLine, bottom, verts[i].y);
                coeffs[i].maxDistance = verts[i].y > pinLine ? 0f : Mathf.Lerp(0.02f, 0.16f, t);
                coeffs[i].collisionSphereDistance = 0f;
            }
            cloth.coefficients = coeffs;
            cloth.stretchingStiffness = 0.95f;
            cloth.bendingStiffness = 0.5f;
            cloth.damping = 0.5f;
            cloth.worldVelocityScale = 0.4f;
            cloth.worldAccelerationScale = 0.6f;
            cloth.selfCollisionDistance = 0f;
            var root = model.transform;
            var caps = new System.Collections.Generic.List<CapsuleCollider>();
            void Add(string a, string b, float r) { var c = Capsule(FindBone(root, a), FindBone(root, b), r); if (c != null) caps.Add(c); }
            Add("mixamorig:Hips", "mixamorig:Spine1", 0.16f);
            Add("mixamorig:Spine1", "mixamorig:Neck", 0.16f);
            Add("mixamorig:LeftUpLeg", "mixamorig:LeftLeg", 0.085f);
            Add("mixamorig:RightUpLeg", "mixamorig:RightLeg", 0.085f);
            Add("mixamorig:LeftLeg", "mixamorig:LeftFoot", 0.065f);
            Add("mixamorig:RightLeg", "mixamorig:RightFoot", 0.065f);
            Add("mixamorig:LeftArm", "mixamorig:LeftForeArm", 0.055f);
            Add("mixamorig:RightArm", "mixamorig:RightForeArm", 0.055f);
            Add("mixamorig:LeftForeArm", "mixamorig:LeftHand", 0.045f);
            Add("mixamorig:RightForeArm", "mixamorig:RightHand", 0.045f);
            cloth.capsuleColliders = caps.ToArray();
            Debug.Log($"[QanivaPresentationAssets] gown cloth: {verts.Length} verts, {caps.Count} colliders");
        }

        // --- environment ---------------------------------------------------

        private static void CreateEnvironmentPrefab(GameObject monitorTemplate)
        {
            var root = new GameObject("ed_resus_v1");

            // Shell: floor + three walls (front stays open for the camera).
            Box("Floor", root.transform, new Vector3(0f, -0.05f, 0.5f), new Vector3(6.4f, 0.1f, 7f), "HospitalFloor");
            Box("WallBack", root.transform, new Vector3(0f, 1.7f, 2.6f), new Vector3(6.4f, 3.4f, 0.1f), "HospitalWall");
            Box("WallLeft", root.transform, new Vector3(-3.15f, 1.7f, 0.5f), new Vector3(0.1f, 3.4f, 7f), "HospitalWall");
            Box("WallRight", root.transform, new Vector3(3.15f, 1.7f, 0.5f), new Vector3(0.1f, 3.4f, 7f), "HospitalWall");
            Box("BaseboardBack", root.transform, new Vector3(0f, 0.06f, 2.54f), new Vector3(6.4f, 0.12f, 0.04f), "AccentTeal");
            Box("BaseboardLeft", root.transform, new Vector3(-3.09f, 0.06f, 0.5f), new Vector3(0.04f, 0.12f, 7f), "AccentTeal");
            Box("BaseboardRight", root.transform, new Vector3(3.09f, 0.06f, 0.5f), new Vector3(0.04f, 0.12f, 7f), "AccentTeal");

            // Believable back-wall dressing: supply cabinet, gas panel, door on left wall.
            Box("SupplyCabinet", root.transform, new Vector3(-1.8f, 1.45f, 2.47f), new Vector3(1.3f, 1.0f, 0.28f), "PlasticLight");
            Box("CabinetSeam", root.transform, new Vector3(-1.8f, 1.45f, 2.32f), new Vector3(0.02f, 0.94f, 0.012f), "PlasticDark");
            Box("GasPanel", root.transform, new Vector3(0.9f, 1.5f, 2.53f), new Vector3(0.8f, 0.35f, 0.06f), "PlasticLight");
            Box("GasOutlet1", root.transform, new Vector3(0.72f, 1.5f, 2.49f), new Vector3(0.09f, 0.09f, 0.05f), "AccentTeal");
            Box("GasOutlet2", root.transform, new Vector3(0.94f, 1.5f, 2.49f), new Vector3(0.09f, 0.09f, 0.05f), "PlasticDark");
            Box("GasOutlet3", root.transform, new Vector3(1.16f, 1.5f, 2.49f), new Vector3(0.09f, 0.09f, 0.05f), "Metal");
            Box("Door", root.transform, new Vector3(-3.08f, 1.1f, -1.3f), new Vector3(0.06f, 2.2f, 1.0f), "PlasticLight");
            Box("DoorHandle", root.transform, new Vector3(-3.03f, 1.05f, -0.92f), new Vector3(0.03f, 0.03f, 0.16f), "Metal");
            Box("WallLightStrip", root.transform, new Vector3(0f, 2.55f, 2.53f), new Vector3(4.2f, 0.10f, 0.06f), "CeilingPanel");

            // Resus bed (head toward the back wall, +Z).
            var bed = new GameObject("ResusBed");
            bed.transform.SetParent(root.transform, false);
            bed.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            Box("BedBase", bed.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.8f, 0.28f, 2.0f), "PlasticDark");
            Box("BedFrame", bed.transform, new Vector3(0f, 0.47f, 0f), new Vector3(0.95f, 0.10f, 2.15f), "Metal");
            Box("Mattress", bed.transform, new Vector3(0f, 0.58f, 0f), new Vector3(0.9f, 0.12f, 2.1f), "Mattress");
            // Raised backrest section (head of bed ~30°) — reads as a real ED bed and
            // supports the semi-recumbent patient pose.
            Box("Backrest", bed.transform, new Vector3(0f, 0.80f, 0.78f), new Vector3(0.9f, 0.10f, 0.62f), "Mattress", new Vector3(-32f, 0f, 0f));
            Box("RailLeft", bed.transform, new Vector3(-0.50f, 0.78f, 0.1f), new Vector3(0.04f, 0.22f, 1.5f), "Metal");
            Box("RailRight", bed.transform, new Vector3(0.50f, 0.78f, 0.1f), new Vector3(0.04f, 0.22f, 1.5f), "Metal");
            Cyl("WheelFL", bed.transform, new Vector3(-0.35f, 0.07f, -0.85f), 0.07f, 0.05f, "PlasticDark", new Vector3(0f, 0f, 90f));
            Cyl("WheelFR", bed.transform, new Vector3(0.35f, 0.07f, -0.85f), 0.07f, 0.05f, "PlasticDark", new Vector3(0f, 0f, 90f));
            Cyl("WheelBL", bed.transform, new Vector3(-0.35f, 0.07f, 0.85f), 0.07f, 0.05f, "PlasticDark", new Vector3(0f, 0f, 90f));
            Cyl("WheelBR", bed.transform, new Vector3(0.35f, 0.07f, 0.85f), 0.07f, 0.05f, "PlasticDark", new Vector3(0f, 0f, 90f));

            // Patient anchor on the mattress (patient prefab spawns here).
            Anchor("PatientAnchor", root.transform, new Vector3(0f, 0.64f, 0.35f));

            // Bedside monitor: reuse the shared prefab, right of the bed head.
            var monitor = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsDir}/BedsideMonitor.prefab"));
            monitor.transform.SetParent(root.transform, false);
            monitor.transform.localPosition = new Vector3(0.66f, 0f, 2.05f);
            // Face the presentation camera: the screen normal is -Z, the camera sits
            // front-left and above ((0.02, 1.92, -2.18)), so yaw slightly left and
            // tilt the face up toward it — the vitals must be readable in-frame.
            monitor.transform.localEulerAngles = new Vector3(10f, 12f, 0f);

            // IV pole, left of the bed head.
            var iv = new GameObject("IvPole");
            iv.transform.SetParent(root.transform, false);
            iv.transform.localPosition = new Vector3(-0.85f, 0f, 1.25f);
            Box("IvBase", iv.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.34f, 0.04f, 0.34f), "Metal");
            Cyl("IvShaft", iv.transform, new Vector3(0f, 0.95f, 0f), 0.016f, 1.9f, "Metal");
            Box("IvHook", iv.transform, new Vector3(0.09f, 1.88f, 0f), new Vector3(0.20f, 0.02f, 0.02f), "Metal");
            Box("IvBag", iv.transform, new Vector3(0.16f, 1.70f, 0f), new Vector3(0.14f, 0.24f, 0.05f), "IvBag");

            // Medical cart, right-front.
            var cart = new GameObject("MedicalCart");
            cart.transform.SetParent(root.transform, false);
            cart.transform.localPosition = new Vector3(1.9f, 0f, -0.6f);
            cart.transform.localEulerAngles = new Vector3(0f, 14f, 0f);
            Box("CartBody", cart.transform, new Vector3(0f, 0.52f, 0f), new Vector3(0.62f, 0.78f, 0.45f), "AccentTeal");
            Box("CartTop", cart.transform, new Vector3(0f, 0.93f, 0f), new Vector3(0.66f, 0.04f, 0.49f), "PlasticLight");
            Box("Drawer1", cart.transform, new Vector3(0f, 0.74f, -0.235f), new Vector3(0.56f, 0.14f, 0.02f), "PlasticLight");
            Box("Drawer2", cart.transform, new Vector3(0f, 0.55f, -0.235f), new Vector3(0.56f, 0.14f, 0.02f), "PlasticLight");
            Box("Drawer3", cart.transform, new Vector3(0f, 0.36f, -0.235f), new Vector3(0.56f, 0.14f, 0.02f), "PlasticLight");
            Box("CartItem1", cart.transform, new Vector3(-0.12f, 0.99f, 0.05f), new Vector3(0.16f, 0.08f, 0.12f), "PlasticDark");
            Box("CartItem2", cart.transform, new Vector3(0.14f, 0.98f, -0.06f), new Vector3(0.10f, 0.06f, 0.10f), "IvBag");
            Cyl("CartWheel1", cart.transform, new Vector3(-0.24f, 0.06f, -0.16f), 0.06f, 0.04f, "PlasticDark", new Vector3(0f, 0f, 90f));
            Cyl("CartWheel2", cart.transform, new Vector3(0.24f, 0.06f, -0.16f), 0.06f, 0.04f, "PlasticDark", new Vector3(0f, 0f, 90f));
            Cyl("CartWheel3", cart.transform, new Vector3(-0.24f, 0.06f, 0.16f), 0.06f, 0.04f, "PlasticDark", new Vector3(0f, 0f, 90f));
            Cyl("CartWheel4", cart.transform, new Vector3(0.24f, 0.06f, 0.16f), 0.06f, 0.04f, "PlasticDark", new Vector3(0f, 0f, 90f));

            // Lighting: one soft-shadow key light + shadowless fill (mobile budget).
            var key = new GameObject("KeyLight");
            key.transform.SetParent(root.transform, false);
            key.transform.localEulerAngles = new Vector3(52f, -28f, 0f);
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 0.82f;
            keyLight.color = new Color(1f, 0.985f, 0.95f);
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowBias = 0.03f;
            keyLight.shadowNormalBias = 0.8f;

            var fill = new GameObject("FillLight");
            fill.transform.SetParent(root.transform, false);
            fill.transform.localPosition = new Vector3(-0.6f, 2.2f, -1.8f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.intensity = 0.34f;
            fillLight.range = 7f;
            fillLight.color = new Color(0.92f, 0.95f, 1f);
            fillLight.shadows = LightShadows.None;

            // Composed portrait camera: patient centred, monitor visible at right,
            // lower third left for the action UI, top band for the vitals bar.
            var camGo = new GameObject("PresentationCamera");
            camGo.transform.SetParent(root.transform, false);
            // Elevated three-quarter view from the foot-left: the patient reads as a
            // person (face + torso + legs), the bed runs diagonally, the monitor sits
            // at the head-right turned toward the viewer, the lower third stays clear
            // for the action sheet. Iterated with CapturePreview.
            camGo.transform.localPosition = new Vector3(-0.38f, 1.98f, -2.55f);
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.LookAt(root.transform.TransformPoint(new Vector3(0.02f, 0.72f, 0.62f)));
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            var path = $"{EnvDir}/ed_resus_v1.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[QanivaPresentationAssets] wrote {path}");
            UnityEngine.Object.DestroyImmediate(root);
            if (monitorTemplate != null)
            {
                UnityEngine.Object.DestroyImmediate(monitorTemplate);
            }
        }

        // --- headless composition preview ---------------------------------

        /// <summary>Renders the composed environment + patient to a portrait PNG so
        /// camera/layout can be iterated without a device build.
        /// Args: -previewOut <path> [-previewState Normal|Distressed|Unconscious|Unresponsive]</summary>
        public static void CapturePreview()
        {
            string outPath = GetArg("-previewOut") ?? "presentation-preview.png";
            string stateArg = GetArg("-previewState") ?? "Distressed";

            var env = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>($"{EnvDir}/ed_resus_v1.prefab"));
            var patientPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PatientDir}/{GetArg("-previewPatient") ?? "adult_rigged_v1"}.prefab");
            var anchor = env.transform.Find("PatientAnchor");
            var patient = (GameObject)PrefabUtility.InstantiatePrefab(patientPrefab);
            patient.transform.SetParent(anchor, false);

            // Demo-case initial canonical values, for preview realism only.
            var snapshot = new SimulationSnapshotView
            {
                Hr = 38, SbpMmHg = 84, DbpMmHg = 52, Spo2 = 95, RrPerMin = 18, SimTimeSec = 0,
                Circulation = "poor_perfusion", Neuro = "alert",
            };
            env.GetComponentInChildren<BedsideMonitorView>()?.SetVitals(snapshot);
            var pvc = patient.GetComponent<PatientVisualController>();
            if (pvc != null)
            {
                // Awake ran on InstantiatePrefab in edit mode? Not reliably — call Apply guarded.
                var state = (PatientVisualState)Enum.Parse(typeof(PatientVisualState), stateArg);
                try { pvc.Apply(state, snapshot.RrPerMin); } catch { /* edit-mode material access */ }
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.35f);
            if (GetArg("-previewDark") != null)
            {
                RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.05f);
                foreach (var l in env.GetComponentsInChildren<Light>())
                {
                    l.intensity *= 0.5f;
                }
            }

            var cam = env.GetComponentInChildren<Camera>();
            var rt = new RenderTexture(1206, 2622, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            // Linear color space: RT holds linear values; convert to sRGB for PNG.
            var px = tex.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = px[i].gamma;
            }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(env);
            Debug.Log($"[QanivaPresentationAssets] preview written -> {outPath}");
        }

        private static float GetArgFloat(string name, float fallback)

        {

            var raw = GetArg(name);

            return raw != null && float.TryParse(raw, System.Globalization.NumberStyles.Float,

                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;

        }


        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
