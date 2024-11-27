using System.IO;
using UnityEngine;
using UnityEditor;

namespace GaussianSplatting.Editor
{
    public class GaussianSplattingPackageSettingsInitializer : AssetPostprocessor
    {
        private const string PackageSettingsAssetPath = "Assets/Resources/GaussianSplattingPackageSettings.asset";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var settings = GaussianSplattingPackageSettings.Instance;
            if (!AssetDatabase.Contains(settings))
            {
                var directoryPath = Path.GetDirectoryName(PackageSettingsAssetPath);
                if (!Directory.Exists(directoryPath))
                {
                    if (directoryPath != null) Directory.CreateDirectory(directoryPath);
                }

                AssetDatabase.CreateAsset(settings, PackageSettingsAssetPath);
                AssetDatabase.SaveAssets();
                
                Debug.Log("Gaussian splatting Package Settings Asset has been created for you in Resources folder. If you want to modify it, go to Project Settings > 404-GEN 3D Generator");
            }
        }
    }
}