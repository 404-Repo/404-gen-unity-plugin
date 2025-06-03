using System.IO;
using UnityEditor;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public class MeshImportProcessor : AssetPostprocessor
    {
        private bool IsImportedMeshModel =>
            (GaussianSplattingPackageSettings.Instance.IsImportedMeshPath(assetPath));
        private void OnPreprocessModel()
        {
            if (IsImportedMeshModel)
            {
                ModelImporter modelImporter = assetImporter as ModelImporter;
                if (modelImporter != null)
                {
                    if (GaussianSplattingPackageSettings.Instance.LogToConsole)
                    {
                        Debug.Log($"Preprocessing model {assetPath}");
                    }

                    modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    modelImporter.materialLocation = ModelImporterMaterialLocation.External;
                    modelImporter.bakeAxisConversion = true;
                    var folderPath = Path.GetDirectoryName(assetPath);
                    if (GaussianSplattingPackageSettings.Instance.LogToConsole)
                    {
                        Debug.Log($"Extracting model textures to {folderPath}");
                    }
                    
                    if (Directory.Exists(folderPath))
                    {
                        if (modelImporter.ExtractTextures(folderPath))
                        {
                            if (GaussianSplattingPackageSettings.Instance.LogToConsole)
                            {
                                Debug.Log($"Extracted textures to {folderPath}");
                            }
                        }
                        else
                        {
                            if (GaussianSplattingPackageSettings.Instance.LogToConsole)
                            {
                                Debug.LogError($"Failed to textures to {folderPath}");
                            }
                        }
                    }
                }
            }
        }

        private void OnPreprocessTexture()
        {
            if (assetPath.Contains("baked_texture"))
            {
                if (GaussianSplattingPackageSettings.Instance.LogToConsole)
                {
                    Debug.Log($"Preprocessing texture {assetPath}");
                }
                var textureImporter = assetImporter as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.maxTextureSize = 8192;
                }
            }
        }

        void OnPostprocessModel(GameObject model)
        {
            if (IsImportedMeshModel)
            {
                GaussianSplattingPackageSettings.Instance.ClearImportedMeshPath(assetPath);
            }
        }
    }
}