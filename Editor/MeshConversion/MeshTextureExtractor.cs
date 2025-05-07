using UnityEditor;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public class MeshTextureExtractor : AssetPostprocessor
    {
        private bool IsImportedMeshModel =>
            assetPath.Equals(GaussianSplattingPackageSettings.Instance.ImportedMeshPath);
        private void OnPreprocessModel()
        {
            if (IsImportedMeshModel)
            {
                Debug.Log($"Mesh texture extractor preprocessing {assetPath}");
                
                ModelImporter modelImporter = assetImporter as ModelImporter;
                if (modelImporter != null)
                {
                    modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    modelImporter.materialLocation = ModelImporterMaterialLocation.External;
                }
            }
        }

        void OnPostprocessModel(GameObject model)
        {
            if (IsImportedMeshModel)
            {
                Debug.Log($"Mesh texture extractor postprocessing {assetPath}");
                model.transform.rotation = Quaternion.Euler(90f, 180, 0f);
            }
        }
    }
}