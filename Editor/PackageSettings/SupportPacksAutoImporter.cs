using UnityEditor;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    [InitializeOnLoad]
    internal static class SupportPacksAutoImporter
    {
        static SupportPacksAutoImporter()
        {
            TryImportSupportPacks();
        }

        private static void TryImportSupportPacks()
        {
#if GS_ENABLE_URP
            // If URP feature prefab not present, import URP support pack silently
            var urpPrefab = Resources.Load<GameObject>("GaussianSplatURPPass");
            if (urpPrefab == null)
            {
                SupportPacksUtility.ImportPackageSilent("URP Support pack.unitypackage");
                AssetDatabase.Refresh();
            }
#endif

#if GS_ENABLE_HDRP
            // If HDRP effect prefab not present, import HDRP support pack silently
            var hdrpPrefab = Resources.Load<GameObject>("GaussianSplatEffect");
            if (hdrpPrefab == null)
            {
                SupportPacksUtility.ImportPackageSilent("HDRP Support pack.unitypackage");
                AssetDatabase.Refresh();
            }
#endif
        }
    }
}


