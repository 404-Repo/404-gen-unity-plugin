using System.Linq;
using UnityEditor;
using UnityEngine;
#if GS_ENABLE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GaussianSplatting.Editor
{
    [InitializeOnLoad]
    internal static class AutoRenderSetup
    {
        static AutoRenderSetup()
        {
            EditorApplication.delayCall += EnsureRenderSetup;
            EditorApplication.hierarchyChanged += EnsureRenderSetup;
        }

        private static void EnsureRenderSetup()
        {
#if GS_ENABLE_URP
            TryEnsureUrpSetup();
#endif
#if GS_ENABLE_HDRP
            TryEnsureHdrpSetup();
#endif
        }

#if GS_ENABLE_URP
        private static void TryEnsureUrpSetup()
        {
            if (GameObject.Find("GaussianSplatURPPass") != null)
            {
                return;
            }

            var urpPrefab = Resources.Load<GameObject>("GaussianSplatURPPass");
            if (urpPrefab == null)
            {
                SupportPacksUtility.ImportPackageSilent("URP Support pack.unitypackage");
                AssetDatabase.Refresh();
                urpPrefab = Resources.Load<GameObject>("GaussianSplatURPPass");
            }

            if (urpPrefab == null)
            {
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(urpPrefab) as GameObject;
            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Gaussian Splat URP Pass");
                Selection.activeGameObject = instance;
                Debug.Log("[404-GEN] Added URP GaussianSplatURPPass to the scene.");
            }
        }
#endif

#if GS_ENABLE_HDRP
        private static void TryEnsureHdrpSetup()
        {
            // If a GaussianSplatEffect object already exists, do nothing
            if (GameObject.Find("GaussianSplatEffect") != null)
            {
                return;
            }

            // Or if any CustomPassVolume contains GaussianSplatHDRPPass, do nothing
            var volumes = Object.FindObjectsOfType<CustomPassVolume>();
            if (volumes != null)
            {
                foreach (var v in volumes)
                {
                    if (v && v.customPasses != null && v.customPasses.Any(cp => cp != null && cp.GetType().Name == "GaussianSplatHDRPPass"))
                    {
                        return;
                    }
                }
            }

            var hdrpPrefab = Resources.Load<GameObject>("GaussianSplatEffect");
            if (hdrpPrefab == null)
            {
                SupportPacksUtility.ImportPackageSilent("HDRP Support pack.unitypackage");
                AssetDatabase.Refresh();
                hdrpPrefab = Resources.Load<GameObject>("GaussianSplatEffect");
            }

            if (hdrpPrefab == null)
            {
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(hdrpPrefab) as GameObject;
            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Gaussian Splat Effect (HDRP)");
                Selection.activeGameObject = instance;
                Debug.Log("[404-GEN] Added HDRP GaussianSplatEffect to the scene.");
            }
        }
#endif
    }
}


