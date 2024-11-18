using System.IO;
using UnityEditor;

namespace GaussianSplatting.Editor
{
    public static class GaussianSplattingPackageSettingsInitializer
    {
        private const string PackageSettingsAssetPath = "Assets/Resources/GaussianSplattingPackageSettings.asset";

        [InitializeOnLoadMethod]
        private static void Initialize()
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
            }
        }
    }
}