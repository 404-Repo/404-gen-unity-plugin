using System.Collections.Generic;
using UnityEngine;
using static GaussianSplatting.Editor.MeshConversionUtility;

namespace GaussianSplatting.Editor
{
    public class GaussianSplattingPackageSettings : ScriptableObject
    {
        private static GaussianSplattingPackageSettings _instance;

        public bool LogToConsole;
        
        public string GeneratedModelsPath = "Assets/GeneratedModels";

        public bool DeleteAssociatedFilesWithPrompt = true;

        public bool UsePromptTimeout = true;
        public int PromptTimeoutInSeconds = 90;
        public bool ConfirmDeletes = true;

        public GenerationOption GenerationOption = GenerationOption.GaussianSplat;
        //todo: set default service url here
        public string ConversionServiceUrl = "http://34.141.10.161/process";
        public string ConvertedModelsPath = "Assets/GeneratedModels/Mesh";
        public float MinDetailSize = 0.01f;
        public float Simplify = 0f;
        public int AngleLimit = 60;
        public MeshConversionTextureSize TextureSize = MeshConversionTextureSize.Size2048;
        public List<string> ImportedMeshPaths = new List<string>();
        
        //singleton
        public static GaussianSplattingPackageSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GaussianSplattingPackageSettings>("GaussianSplattingPackageSettings");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GaussianSplattingPackageSettings>();
                    }
                }

                return _instance;
            }
        }

        public void SetDefaultConversionParameters()
        {
            MinDetailSize = 0.01f;
            Simplify = 0f;
            AngleLimit = 60;
            TextureSize = MeshConversionTextureSize.Size2048;
        }

        public void SetImportedMeshPath(string meshPath)
        {
            if (!ImportedMeshPaths.Contains(meshPath))
            {
                ImportedMeshPaths.Add(meshPath);
            }
        }

        public bool IsImportedMeshPath(string meshPath)
        {
            return ImportedMeshPaths.Contains(meshPath);
        }

        public void ClearImportedMeshPath(string meshPath)
        {
            if (ImportedMeshPaths.Contains(meshPath))
            {
                ImportedMeshPaths.Remove(meshPath);
            }
        }
    }
}