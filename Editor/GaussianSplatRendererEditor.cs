// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using GaussianSplatting.Runtime;
using GK;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;
using GaussianSplatRenderer = GaussianSplatting.Runtime.GaussianSplatRenderer;

namespace GaussianSplatting.Editor
{
    [CustomEditor(typeof(GaussianSplatRenderer))]
    [CanEditMultipleObjects]
    public class GaussianSplatRendererEditor : UnityEditor.Editor
    {
        const string kPrefExportBake = "nesnausk.GaussianSplatting.ExportBakeTransform";

        SerializedProperty m_PropAsset;
        SerializedProperty m_PropSplatScale;
        SerializedProperty m_PropOpacityScale;
        SerializedProperty m_PropSHOrder;
        SerializedProperty m_PropSHOnly;
        SerializedProperty m_PropSortNthFrame;
        SerializedProperty m_PropRenderMode;
        SerializedProperty m_PropPointDisplaySize;
        SerializedProperty m_PropCutouts;
        SerializedProperty m_PropShaderSplats;
        SerializedProperty m_PropShaderComposite;
        SerializedProperty m_PropShaderDebugPoints;
        SerializedProperty m_PropShaderDebugBoxes;
        SerializedProperty m_PropCSSplatUtilities;

        bool m_ResourcesExpanded = false;
        int m_CameraIndex = 0;

        bool m_ExportBakeTransform;

        static int s_EditStatsUpdateCounter = 0;

        static HashSet<GaussianSplatRendererEditor> s_AllEditors = new();

        public static void BumpGUICounter()
        {
            ++s_EditStatsUpdateCounter;
        }

        public static void RepaintAll()
        {
            foreach (var e in s_AllEditors)
                e.Repaint();
        }

        public void OnEnable()
        {
            m_ExportBakeTransform = EditorPrefs.GetBool(kPrefExportBake, false);

            m_PropAsset = serializedObject.FindProperty("m_Asset");
            m_PropSplatScale = serializedObject.FindProperty("m_SplatScale");
            m_PropOpacityScale = serializedObject.FindProperty("m_OpacityScale");
            m_PropSHOrder = serializedObject.FindProperty("m_SHOrder");
            m_PropSHOnly = serializedObject.FindProperty("m_SHOnly");
            m_PropSortNthFrame = serializedObject.FindProperty("m_SortNthFrame");
            m_PropRenderMode = serializedObject.FindProperty("m_RenderMode");
            m_PropPointDisplaySize = serializedObject.FindProperty("m_PointDisplaySize");
            m_PropCutouts = serializedObject.FindProperty("m_Cutouts");
            m_PropShaderSplats = serializedObject.FindProperty("m_ShaderSplats");
            m_PropShaderComposite = serializedObject.FindProperty("m_ShaderComposite");
            m_PropShaderDebugPoints = serializedObject.FindProperty("m_ShaderDebugPoints");
            m_PropShaderDebugBoxes = serializedObject.FindProperty("m_ShaderDebugBoxes");
            m_PropCSSplatUtilities = serializedObject.FindProperty("m_CSSplatUtilities");

            s_AllEditors.Add(this);
            CacheComponents();
        }
        
        GaussianSplatRenderer m_gaussianSplatRenderer;
        GameObject m_gameobject;
        Mesh m_mesh;
        MeshFilter m_meshFilter;
        MeshCollider m_meshCollider;
        MeshRenderer m_meshRenderer;
        
        private void CacheComponents()
        {
	        m_gaussianSplatRenderer = (target as GaussianSplatRenderer);
	        if (m_gaussianSplatRenderer != null)
	        {
		        m_gameobject = m_gaussianSplatRenderer.gameObject;
		        m_meshFilter = m_gameobject.GetComponent<MeshFilter>();
		        m_meshCollider = m_gameobject.GetComponent<MeshCollider>();
		        m_meshRenderer = m_gameobject.GetComponent<MeshRenderer>();
	        }
        }

        public void OnDisable()
        {
            s_AllEditors.Remove(this);
        }

        public override void OnInspectorGUI()
        {
            var gs = target as GaussianSplatRenderer;
            if (!gs)
                return;

            serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Data Asset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PropAsset);

            if (!gs.HasValidAsset)
            {
                var msg = gs.asset != null && gs.asset.formatVersion != GaussianSplatAsset.kCurrentVersion
                    ? "Gaussian Splat asset version is not compatible, please recreate the asset"
                    : "Gaussian Splat asset is not assigned or is empty";
                EditorGUILayout.HelpBox(msg, MessageType.Error);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Render Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PropSplatScale);
            EditorGUILayout.PropertyField(m_PropOpacityScale);
            EditorGUILayout.PropertyField(m_PropSHOrder);
            EditorGUILayout.PropertyField(m_PropSHOnly);
            EditorGUILayout.PropertyField(m_PropSortNthFrame);

            EditorGUILayout.Space();
            GUILayout.Label("Debugging Tweaks", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PropRenderMode);
            if (m_PropRenderMode.intValue is (int)GaussianSplatRenderer.RenderMode.DebugPoints or (int)GaussianSplatRenderer.RenderMode.DebugPointIndices)
                EditorGUILayout.PropertyField(m_PropPointDisplaySize);

            EditorGUILayout.Space();
            m_ResourcesExpanded = EditorGUILayout.Foldout(m_ResourcesExpanded, "Resources", true, EditorStyles.foldoutHeader);
            if (m_ResourcesExpanded)
            {
                EditorGUILayout.PropertyField(m_PropShaderSplats);
                EditorGUILayout.PropertyField(m_PropShaderComposite);
                EditorGUILayout.PropertyField(m_PropShaderDebugPoints);
                EditorGUILayout.PropertyField(m_PropShaderDebugBoxes);
                EditorGUILayout.PropertyField(m_PropCSSplatUtilities);
            }
            bool validAndEnabled = gs && gs.enabled && gs.gameObject.activeInHierarchy && gs.HasValidAsset;
            if (validAndEnabled && !gs.HasValidRenderSetup)
            {
                EditorGUILayout.HelpBox("Shader resources are not set up", MessageType.Error);
                validAndEnabled = false;
            }

            if (validAndEnabled && targets.Length == 1)
            {
                EditCameras(gs);
                EditGUI(gs);
            }
            if (validAndEnabled && targets.Length > 1)
            {
                MultiEditGUI();
            }

            if (EditorGUI.EndChangeCheck())
            {
	            serializedObject.ApplyModifiedProperties();
	            EditorUtility.SetDirty(target);
            }
        }

        void EditCameras(GaussianSplatRenderer gs)
        {
            var asset = gs.asset;
            var cameras = asset.cameras;
            if (cameras != null && cameras.Length != 0)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Cameras", EditorStyles.boldLabel);
                var camIndex = EditorGUILayout.IntSlider("Camera", m_CameraIndex, 0, cameras.Length - 1);
                camIndex = math.clamp(camIndex, 0, cameras.Length - 1);
                if (camIndex != m_CameraIndex)
                {
                    m_CameraIndex = camIndex;
                    gs.ActivateCamera(camIndex);
                }
            }
        }

        void MultiEditGUI()
        {
            DrawSeparator();
            CountTargetSplats(out var totalSplats, out var totalObjects);
            EditorGUILayout.LabelField("Total Objects", $"{totalObjects}");
            EditorGUILayout.LabelField("Total Splats", $"{totalSplats:N0}");
            if (totalSplats > GaussianSplatAsset.kMaxSplats)
            {
                EditorGUILayout.HelpBox($"Can't merge, too many splats (max. supported {GaussianSplatAsset.kMaxSplats:N0})", MessageType.Warning);
                return;
            }

            var targetGs = (GaussianSplatRenderer) target;
            if (!targetGs || !targetGs.HasValidAsset || !targetGs.isActiveAndEnabled)
            {
                EditorGUILayout.HelpBox($"Can't merge into {target.name} (no asset or disable)", MessageType.Warning);
                return;
            }

            if (targetGs.asset.chunkData != null)
            {
                EditorGUILayout.HelpBox($"Can't merge into {target.name} (needs to use Very High quality preset)", MessageType.Warning);
                return;
            }
            if (GUILayout.Button($"Merge into {target.name}"))
            {
                MergeSplatObjects();
            }
        }

        void CountTargetSplats(out int totalSplats, out int totalObjects)
        {
            totalObjects = 0;
            totalSplats = 0;
            foreach (var obj in targets)
            {
                var gs = obj as GaussianSplatRenderer;
                if (!gs || !gs.HasValidAsset || !gs.isActiveAndEnabled)
                    continue;
                ++totalObjects;
                totalSplats += gs.splatCount;
            }
        }

        void MergeSplatObjects()
        {
            CountTargetSplats(out var totalSplats, out _);
            if (totalSplats > GaussianSplatAsset.kMaxSplats)
                return;
            var targetGs = (GaussianSplatRenderer) target;

            int copyDstOffset = targetGs.splatCount;
            targetGs.EditSetSplatCount(totalSplats);
            foreach (var obj in targets)
            {
                var gs = obj as GaussianSplatRenderer;
                if (!gs || !gs.HasValidAsset || !gs.isActiveAndEnabled)
                    continue;
                if (gs == targetGs)
                    continue;
                gs.EditCopySplatsInto(targetGs, 0, copyDstOffset, gs.splatCount);
                copyDstOffset += gs.splatCount;
                gs.gameObject.SetActive(false);
            }
            Debug.Assert(copyDstOffset == totalSplats, $"Merge count mismatch, {copyDstOffset} vs {totalSplats}");
            Selection.activeObject = targetGs;
        }

        void EditGUI(GaussianSplatRenderer gs)
        {
            ++s_EditStatsUpdateCounter;

            DrawSeparator();
            bool wasToolActive = ToolManager.activeContextType == typeof(GaussianToolContext);
            GUILayout.BeginHorizontal();
            bool isToolActive = GUILayout.Toggle(wasToolActive, "Edit", EditorStyles.miniButton);
            using (new EditorGUI.DisabledScope(!gs.editModified))
            {
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                {
                    if (EditorUtility.DisplayDialog("Reset Splat Modifications?",
                            $"This will reset edits of {gs.name} to match the {gs.asset.name} asset. Continue?",
                            "Yes, reset", "Cancel"))
                    {
                        gs.enabled = false;
                        gs.enabled = true;
                    }
                }
            }

            GUILayout.EndHorizontal();
            if (!wasToolActive && isToolActive)
            {
                ToolManager.SetActiveContext<GaussianToolContext>();
                if (Tools.current == Tool.View)
                    Tools.current = Tool.Move;
            }

            if (wasToolActive && !isToolActive)
            {
                ToolManager.SetActiveContext<GameObjectToolContext>();
            }

            if (isToolActive && gs.asset.chunkData != null)
            {
                EditorGUILayout.HelpBox("Splat move/rotate/scale tools need Very High splat quality preset", MessageType.Warning);
            }

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Cutout"))
            {
                GaussianCutout cutout = ObjectFactory.CreateGameObject("GSCutout", typeof(GaussianCutout)).GetComponent<GaussianCutout>();
                Transform cutoutTr = cutout.transform;
                cutoutTr.SetParent(gs.transform, false);
                cutoutTr.localScale = (gs.asset.boundsMax - gs.asset.boundsMin) * 0.25f;
                gs.m_Cutouts ??= Array.Empty<GaussianCutout>();
                ArrayUtility.Add(ref gs.m_Cutouts, cutout);
                gs.UpdateEditCountsAndBounds();
                EditorUtility.SetDirty(gs);
                Selection.activeGameObject = cutout.gameObject;
            }
            if (GUILayout.Button("Use All Cutouts"))
            {
                gs.m_Cutouts = FindObjectsByType<GaussianCutout>(FindObjectsSortMode.InstanceID);
                gs.UpdateEditCountsAndBounds();
                EditorUtility.SetDirty(gs);
            }

            if (GUILayout.Button("No Cutouts"))
            {
                gs.m_Cutouts = Array.Empty<GaussianCutout>();
                gs.UpdateEditCountsAndBounds();
                EditorUtility.SetDirty(gs);
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_PropCutouts);

            bool hasCutouts = gs.m_Cutouts != null && gs.m_Cutouts.Length != 0;
            bool modifiedOrHasCutouts = gs.editModified || hasCutouts;

            var asset = gs.asset;
            EditorGUILayout.Space();



            if (asset.posFormat > GaussianSplatAsset.VectorFormat.Norm16 ||
                asset.scaleFormat > GaussianSplatAsset.VectorFormat.Norm16 ||
                asset.colorFormat > GaussianSplatAsset.ColorFormat.Float16x4 ||
                asset.shFormat > GaussianSplatAsset.SHFormat.Float16)
            {
                EditorGUILayout.HelpBox(
                    "It is recommended to use High or VeryHigh quality preset for editing splats, lower levels are lossy",
                    MessageType.Warning);
            }

            bool displayEditStats = isToolActive || modifiedOrHasCutouts;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Splats", $"{gs.splatCount:N0}");
            if (displayEditStats)
            {
                EditorGUILayout.LabelField("Cut", $"{gs.editCutSplats:N0}");
                EditorGUILayout.LabelField("Deleted", $"{gs.editDeletedSplats:N0}");
                EditorGUILayout.LabelField("Selected", $"{gs.editSelectedSplats:N0}");
                if (hasCutouts)
                {
                    if (s_EditStatsUpdateCounter > 10)
                    {
                        gs.UpdateEditCountsAndBounds();
                        s_EditStatsUpdateCounter = 0;
                    }
                }
            }

	        EditorGUILayout.Space();
	        GUILayout.Label("Collider", EditorStyles.boldLabel);

	        using (new EditorGUI.DisabledScope(m_meshCollider != null))
	        {
		        if (GUILayout.Button("Add Mesh collider"))
		        {
			        if (m_meshFilter == null)
			        {
				        AddMeshFilter(gs.gameObject, gs);
			        }
			        AddMeshCollider(gs.gameObject, m_mesh);
			        EditorUtility.SetDirty(this);
		        }
	        }

	        using (new EditorGUI.DisabledScope(m_meshCollider == null))
	        {
		        if (GUILayout.Button("Remove Mesh collider"))
		        {
			        DestroyImmediate(m_meshCollider);
			        m_meshCollider = null;

			        if (m_meshFilter && !m_meshRenderer)
			        {
				        DestroyImmediate(m_meshFilter);
				        m_meshFilter = null;
			        }
			        EditorUtility.SetDirty(this);
		        }
	        }

	        EditorGUILayout.Space();
	        GUILayout.Label("Shadow", EditorStyles.boldLabel);

	        using (new EditorGUI.DisabledScope(m_meshRenderer != null))
	        {
		        if (GUILayout.Button("Add shadow"))
		        {
			        if (m_meshFilter == null)
			        {
				        AddMeshFilter(gs.gameObject, gs);
			        }
			        
			        m_meshRenderer = gs.gameObject.AddComponent<MeshRenderer>();
		        
			        Material meshRendererMaterial = null;
			        #if GS_ENABLE_URP
			        meshRendererMaterial = Resources.Load<Material>("ShadowMaterialURP");
			        #elif GS_ENABLE_HDRP
			        meshRendererMaterial = Resources.Load<Material>("ShadowMaterialHDRP");
			        #else
			        meshRendererMaterial = Resources.Load<Material>("ShadowMaterialStandard");
			        #endif

			        if (meshRendererMaterial != null)
			        {
				        m_meshRenderer.sharedMaterial = meshRendererMaterial;
				        m_meshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
			        }
			        
			        EditorUtility.SetDirty(this);
		        }
	        }

	        using (new EditorGUI.DisabledScope(m_meshRenderer == null))
	        {
		        if (GUILayout.Button("Remove shadow"))
		        {
			        DestroyImmediate(m_meshRenderer);
			        m_meshRenderer = null;
			        
			        if (m_meshFilter && !m_meshCollider)
			        {
				        DestroyImmediate(m_meshFilter);
				        m_meshFilter = null;
			        }
			        EditorUtility.SetDirty(this);
		        }
	        }
            
            EditorGUILayout.Space();
            GUILayout.Label("Export", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            m_ExportBakeTransform = EditorGUILayout.Toggle("Export in world space", m_ExportBakeTransform);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(kPrefExportBake, m_ExportBakeTransform);
            }
            
            if (GUILayout.Button("Export PLY"))
            {
                var path = EditorUtility.SaveFilePanel(
                    "Export Gaussian Splat PLY file", "", $"{gs.asset.name}-edit.ply", "ply");
                if (string.IsNullOrWhiteSpace(path))
                    return;
                MeshConversionUtility.ExportPlyFile(gs, m_ExportBakeTransform, path);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Convert to Mesh"))
            {
                MeshConversionEditor.ConvertGaussianSplat(gs, m_ExportBakeTransform);
            }
        }



        static void DrawSeparator()
        {
            EditorGUILayout.Space(12f, true);
            GUILayout.Box(GUIContent.none, "sv_iconselector_sep", GUILayout.Height(2), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();
        }

        bool HasFrameBounds()
        {
            return true;
        }

        Bounds OnGetFrameBounds()
        {
            var gs = target as GaussianSplatRenderer;
            if (!gs || !gs.HasValidRenderSetup)
                return new Bounds(Vector3.zero, Vector3.one);
            Bounds bounds = default;
            bounds.SetMinMax(gs.asset.boundsMin, gs.asset.boundsMax);
            if (gs.editSelectedSplats > 0)
            {
                bounds = gs.editSelectedBounds;
            }
            bounds.extents *= 0.7f;
            return TransformBounds(gs.transform, bounds);
        }

        public static Bounds TransformBounds(Transform tr, Bounds bounds )
        {
            var center = tr.TransformPoint(bounds.center);

            var ext = bounds.extents;
            var axisX = tr.TransformVector(ext.x, 0, 0);
            var axisY = tr.TransformVector(0, ext.y, 0);
            var axisZ = tr.TransformVector(0, 0, ext.z);

            // sum their absolute value to get the world extents
            ext.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            ext.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            ext.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = ext };
        }

        private void AddMeshFilter(GameObject gameObject, GaussianSplatRenderer gs)
        {
	        var splatPositions = GetBakedSplatPositions(gs);

	        //hull generation algorithm
	        var convexHullCalculator = new ConvexHullCalculator();
	        List<Vector3> verts = new List<Vector3>();
	        List<int> tris = new List<int>();
	        List<Vector3> normals = new List<Vector3>();
	        convexHullCalculator.GenerateHull(splatPositions.ToList(), true, ref verts, ref tris, ref normals);

	        //add mesh collider component and set it up
	        m_mesh = new Mesh();
	        m_mesh.SetVertices(verts);
	        m_mesh.SetTriangles(tris, 0);
	        m_mesh.SetNormals(normals);
	        m_mesh.name = $"{gameObject.name} collider mesh";

	        if (!m_meshFilter)
	        {
		        m_meshFilter = gameObject.AddComponent<MeshFilter>();
	        }

	        m_meshFilter.sharedMesh = m_mesh;
        }

        private void AddMeshCollider(GameObject gameObject, Mesh mesh)
        {
	        if (!m_meshCollider)
            {
	            m_meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            m_meshCollider.sharedMesh = mesh;
            m_meshCollider.convex = true;
        }

        private static Vector3[] ConvertToVector3Array(float[] floatArray)
        {
            // Initialize the Vector3 array
            int vectorCount = floatArray.Length / 3;
            Vector3[] vectorArray = new Vector3[vectorCount];

            // Populate the Vector3 array
            for (int i = 0; i < vectorCount; i++)
            {
                int baseIndex = i * 3;
                vectorArray[i] = new Vector3(
                    floatArray[baseIndex], // x
                    floatArray[baseIndex + 1], // y
                    floatArray[baseIndex + 2] // z
                );
            }

            return vectorArray;
        }

        // Method that spawns quads at each position, facing the camera
        public static void CreateQuadsFacingCamera(Vector3[] positions, Camera camera)
        {
            // Create a parent GameObject
            GameObject parentObject = new GameObject("Quads");
            var scale = Vector3.one * 0.01f;
            var skip = 100;

            for (var index = 0; index < positions.Length; index += skip)
            {
                var position = positions[index];
                // Create a quad primitive
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

                // Set the position of the quad
                quad.transform.position = position;

                // Rotate the quad to face the camera
                quad.transform.LookAt(camera.transform);

                // Optional: Adjust the rotation to ensure the quad is aligned properly
                quad.transform.Rotate(0, 90, 0); // Adjust the rotation if needed

                // Set the parent of the quad to be the newly created parent GameObject
                quad.transform.SetParent(parentObject.transform);

                quad.transform.localScale = scale;
            }
        }
        
        static Vector3[] GetBakedSplatPositions(GaussianSplatRenderer gs)
        {
            int kSplatSize = UnsafeUtility.SizeOf<GaussianSplatAssetCreator.InputSplatData>();
            using var gpuData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gs.splatCount, kSplatSize);

            if (!gs.EditExportData(gpuData, false))
                return null;

            GaussianSplatAssetCreator.InputSplatData[] data = new GaussianSplatAssetCreator.InputSplatData[gpuData.count];
            gpuData.GetData(data);

            var gpuDeleted = gs.GpuEditDeleted;
            uint[] deleted = new uint[gpuDeleted.count];
            gpuDeleted.GetData(deleted);

            // count non-deleted splats
            int aliveCount = 0;
            for (int i = 0; i < data.Length; ++i)
            {
                int wordIdx = i >> 5;
                int bitIdx = i & 31;
                bool isDeleted = (deleted[wordIdx] & (1u << bitIdx)) != 0;
                bool isCutout = data[i].nor.sqrMagnitude > 0;
                if (!isDeleted && !isCutout)
                    ++aliveCount;
            }

            Vector3[] bakedSplatPositions = new Vector3[aliveCount];

            int bakedSplatPosIndex = 0;

            for (int i = 0; i < data.Length; ++i)
            {
                int wordIdx = i >> 5;
                int bitIdx = i & 31;
                bool isDeleted = (deleted[wordIdx] & (1u << bitIdx)) != 0;
                bool isCutout = data[i].nor.sqrMagnitude > 0;
                if (!isDeleted && !isCutout)
                {
	                bakedSplatPositions[bakedSplatPosIndex++] = data[i].pos;
                }
            }

            return bakedSplatPositions;
        }

    }
}