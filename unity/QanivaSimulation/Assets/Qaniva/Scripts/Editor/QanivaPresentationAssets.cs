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
            const string fbxPath = "Assets/Qaniva/Art/Patients/adult_rigged_v1.fbx";
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[QanivaPresentationAssets] {fbxPath} missing — run scripts/generate-patient-blender.py in Blender first. Keeping the existing rigged prefab (if any).");
                return null;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.animationType = ModelImporterAnimationType.Generic; // procedural bone animation, no clips yet
            foreach (var (src, dst) in new[]
            {
                ("PatientSkinMat", "Skin"),
                ("PatientGownMat", "Gown"),
                ("PatientHairMat", "Hair"),
                ("PatientBlanketMat", "Blanket"),
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
            model.transform.localRotation =
                Quaternion.AngleAxis(180f, Vector3.forward) * Quaternion.Euler(90f, 0f, 0f);
            // Feet toward the bed's foot end; body rests on the mattress plane.
            model.transform.localPosition = new Vector3(0f, 0.14f, -0.85f);

            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in renderer.sharedMaterials)
                {
                    Debug.Log($"[QanivaPresentationAssets] rigged patient material: {renderer.name} -> {(m == null ? "NULL" : m.name)}");
                }
            }

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
            Box("StandBase", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.42f, 0.06f, 0.42f), "PlasticDark");
            Cyl("StandPole", root.transform, new Vector3(0f, 0.68f, 0f), 0.028f, 1.3f, "Metal");
            Box("Body", root.transform, new Vector3(0f, 1.42f, 0.015f), new Vector3(0.55f, 0.44f, 0.09f), "PlasticLight");

            Box("Screen", root.transform, new Vector3(0f, 1.42f, -0.035f), new Vector3(0.50f, 0.38f, 0.012f), "ScreenDark");

            var green = new Color(0.35f, 0.95f, 0.55f);
            var cyan = new Color(0.45f, 0.85f, 0.95f);
            var label = new Color(0.65f, 0.70f, 0.72f);
            const float zFace = -0.045f; // just in front of the screen face

            // Labels sit under the (unscaled) monitor root — never under the scaled
            // Screen box, which would distort TextMesh glyphs non-uniformly.
            Text("HrLabel", root.transform, new Vector3(-0.14f, 1.555f, zFace), "HR", 0.035f, label);
            Text("HrValue", root.transform, new Vector3(-0.14f, 1.475f, zFace), "--", 0.095f, green);
            Text("Spo2Label", root.transform, new Vector3(0.14f, 1.555f, zFace), "SpO2", 0.035f, label);
            Text("Spo2Value", root.transform, new Vector3(0.14f, 1.475f, zFace), "--", 0.095f, cyan);
            Text("BpLabel", root.transform, new Vector3(-0.14f, 1.385f, zFace), "BP", 0.035f, label);
            Text("BpValue", root.transform, new Vector3(-0.14f, 1.315f, zFace), "--/--", 0.062f, green);
            Text("RrLabel", root.transform, new Vector3(0.14f, 1.385f, zFace), "RR", 0.035f, label);
            Text("RrValue", root.transform, new Vector3(0.14f, 1.315f, zFace), "--", 0.075f, cyan);
            Text("ClockValue", root.transform, new Vector3(0f, 1.255f, zFace), "00:00", 0.032f, label);

            root.AddComponent<BedsideMonitorView>();

            var path = $"{PropsDir}/BedsideMonitor.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[QanivaPresentationAssets] wrote {path}");
            var result = root;
            return result;
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
            monitor.transform.localPosition = new Vector3(0.80f, 0f, 1.28f);
            monitor.transform.localEulerAngles = new Vector3(0f, -35f, 0f);

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
            keyLight.intensity = 0.76f;
            keyLight.color = new Color(1f, 0.985f, 0.95f);
            keyLight.shadows = LightShadows.Soft;

            var fill = new GameObject("FillLight");
            fill.transform.SetParent(root.transform, false);
            fill.transform.localPosition = new Vector3(-0.6f, 2.2f, -1.8f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.intensity = 0.40f;
            fillLight.range = 7f;
            fillLight.color = new Color(0.92f, 0.95f, 1f);
            fillLight.shadows = LightShadows.None;

            // Composed portrait camera: patient centred, monitor visible at right,
            // lower third left for the action UI, top band for the vitals bar.
            var camGo = new GameObject("PresentationCamera");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0.02f, 2.05f, -2.35f);
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.LookAt(root.transform.TransformPoint(new Vector3(0.05f, 0.35f, 0.75f)));
            cam.fieldOfView = 60f;
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
            var patientPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PatientDir}/adult_neutral_v1.prefab");
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
