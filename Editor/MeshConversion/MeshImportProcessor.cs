using UnityEditor;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public class MeshImportProcessor : AssetPostprocessor
    {
        private bool IsImportedMeshModel =>
            assetPath.Equals(GaussianSplattingPackageSettings.Instance.ImportedMeshPath);
        private void OnPreprocessModel()
        {
            if (IsImportedMeshModel)
            {
                ModelImporter modelImporter = assetImporter as ModelImporter;
                if (modelImporter != null)
                {
                    modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    modelImporter.materialLocation = ModelImporterMaterialLocation.External;
                    modelImporter.bakeAxisConversion = true;
                }
            }
        }

        void OnPostprocessModel(GameObject model)
        {
            if (IsImportedMeshModel)
            {
                GaussianSplattingPackageSettings.Instance.ImportedMeshPath = null;
            }
        }
    }
}