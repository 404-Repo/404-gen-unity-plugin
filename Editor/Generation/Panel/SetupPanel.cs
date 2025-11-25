using UnityEngine;
using UnityEditor;
using System;

namespace GaussianSplatting.Editor
{
    public class SetupPanel
    {
        private GameObject _gaussianSplatURPPassPrefab;
        private GameObject _gaussianSplatEffectPrefab;

        
        private void AddGaussianSplatEffect()
        {
            if (_gaussianSplatEffectPrefab == null)
            {
                Debug.LogError("GaussianSplatEffect prefab not found in Resources folder. Please ensure the prefab is correctly placed in a 'Resources' folder.");
                return;
            }

            // Instantiate the prefab into the scene
            GameObject instance = PrefabUtility.InstantiatePrefab(_gaussianSplatEffectPrefab) as GameObject;

            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Gaussian Splat Effect");
                Selection.activeGameObject = instance;

                Debug.Log("Gaussian Splat Effect added to the scene.");
            }
            else
            {
                Debug.LogError("Failed to instantiate GaussianSplatEffect prefab.");
            }
        }
        
        private void AddCustomRenderingPassToCamera()
        {
            // Load the prefab from the Resources folder
            if (_gaussianSplatURPPassPrefab == null)
            {
                Debug.LogError("GaussianSplatURPPass prefab not found in Resources folder. Please ensure the prefab is correctly placed in a 'Resources' folder.");
                return;
            }

            // Instantiate the prefab into the scene
            GameObject instance = PrefabUtility.InstantiatePrefab(_gaussianSplatURPPassPrefab) as GameObject;

            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Gaussian Splat URP Pass");
                Selection.activeGameObject = instance;

                Debug.Log("GaussianSplatURPPass added to the scene.");
            }
            else
            {
                Debug.LogError("Failed to instantiate GaussianSplatURPPass prefab.");
            }
        }

        public void Draw()
        {
            // if (!m_showRenderingSetup) return;

            #if GS_ENABLE_URP
            EditorGUILayout.HelpBox("To add Gaussian splats to the URP rendering process, a custom Render pass must be enqueued when camera starts rendering the scene. ",
                MessageType.Info);
            
            
            if (_gaussianSplatURPPassPrefab == null)
            {
                //attempt loading
                _gaussianSplatURPPassPrefab = Resources.Load<GameObject>("GaussianSplatURPPass");
                if (_gaussianSplatURPPassPrefab == null)
                {
                    EditorGUILayout.HelpBox(
                        "Load 'HDRP and URP Support Packs/URP Support pack.unitypackage' to enable custom render pass to be added.",
                        MessageType.Warning);
                    if (GUILayout.Button("Load URP Support pack"))
                    {
                        SupportPacksUtility.ImportPackage("URP Support pack.unitypackage");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Click here to add a gameobject that enqueues custom rendering pass by instantiating a prefab named 'GaussianSplatURPPass' from the Resources folder.",
                    MessageType.Warning);
                // Load the prefab from the Resources folder
                if (GUILayout.Button("Add custom rendering pass"))
                {
                    AddCustomRenderingPassToCamera();
                }
            }
            
            #elif GS_ENABLE_HDRP
            EditorGUILayout.HelpBox(
                    "To add Gaussian splats to the HDRP rendering process, a CustomPassVolume must be present in the scene. ",
                    MessageType.Info);

            if (_gaussianSplatEffectPrefab == null)
            {
                // Load the prefab from the Resources folder
                _gaussianSplatEffectPrefab = Resources.Load<GameObject>("GaussianSplatEffect");
                if (_gaussianSplatEffectPrefab == null)
                {
                    EditorGUILayout.HelpBox(
                        "Load 'HDRP and URP Support Packs/HDRP Support pack.unitypackage' to enable CustomPassVolume to be added.",
                        MessageType.Warning);
                    
                    if (GUILayout.Button("Load HDRP Support pack"))
                    {
                        SupportPacksUtility.ImportPackage("HDRP Support pack.unitypackage");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Click here to add a preconfigured CustomPassVolume by instantiating a prefab named 'GaussianSplatEffect' from the Resources folder.",
                    MessageType.Warning);

                if (GUILayout.Button("Add HDRP custom pass"))
                {
                    AddGaussianSplatEffect();
                }
            }
            #endif
            GUILayout.Space(20);
        }
    }
}