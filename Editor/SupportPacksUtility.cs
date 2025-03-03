using System.IO;
using UnityEditor;

namespace GaussianSplatting.Editor
{
    public static class SupportPacksUtility
    {
        private const string SupportPackagesPath = "Assets/404-gen-unity-plugin/HDRP and URP Support Packs/";
        private const string AlternativeSupportPackagesPath = "Packages/xyz.404.404-gen-unity-plugin/HDRP and URP Support Packs/";

        public static void ImportPackage(string unitypackageName)
        {
            var unitypackagePath = Path.Combine(SupportPackagesPath, unitypackageName);
            var alternativeUnitypackagePath = Path.Combine(AlternativeSupportPackagesPath, unitypackageName);
            if (File.Exists(unitypackagePath))
            {
                AssetDatabase.ImportPackage(unitypackagePath, true);
            } else if (File.Exists(alternativeUnitypackagePath))
            {
                AssetDatabase.ImportPackage(alternativeUnitypackagePath, true);
            }
        }
    }
}