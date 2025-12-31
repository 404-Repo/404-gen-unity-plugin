using System.IO;
using UnityEditor;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public static class SupportPacksUtility
    {
        private const string SupportPackagesPath = "Assets/404-gen-unity-plugin/HDRP and URP Support Packs/";
        private const string AlternativeSupportPackagesPath = "Packages/xyz.404.404-gen-unity-plugin/HDRP and URP Support Packs/";

        private static string FindUnitypackagePath(string unitypackageName)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
                projectRoot = ".";

            // Known paths first
            var candidates = new[]
            {
                Path.Combine(SupportPackagesPath, unitypackageName),
                Path.Combine(AlternativeSupportPackagesPath, unitypackageName),
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                    return c;
            }

            // Try resolve from this package location
            try
            {
                // Prefer exact package name
                var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/xyz.404.404-gen-unity-plugin");
                if (pkg != null)
                {
                    var p = Path.Combine(pkg.assetPath, "HDRP and URP Support Packs", unitypackageName);
                    if (File.Exists(p)) return p;
                }
            }
            catch {}

            // Fallback: search anywhere in the project (Assets, Packages, etc.)
            try
            {
                var matches = Directory.GetFiles(projectRoot, unitypackageName, SearchOption.AllDirectories);
                foreach (var m in matches)
                {
                    if (m.EndsWith(unitypackageName))
                        return m.Replace("\\", "/");
                }
            }
            catch {}

            return null;
        }

        public static void ImportPackage(string unitypackageName)
        {
            var path = FindUnitypackagePath(unitypackageName);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                AssetDatabase.ImportPackage(path, true);
                return;
            }
            Debug.LogError($"[404-GEN] Could not locate '{unitypackageName}'. Ensure the support pack exists in your project.");
        }

        public static void ImportPackageSilent(string unitypackageName)
        {
            var path = FindUnitypackagePath(unitypackageName);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                AssetDatabase.ImportPackage(path, false);
                return;
            }
            Debug.LogWarning($"[404-GEN] Support pack '{unitypackageName}' not found for silent import.");
        }
    }
}