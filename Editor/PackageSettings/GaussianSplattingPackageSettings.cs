using System.Collections.Generic;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public enum GenerationOption
    {
        GaussianSplat,
        MeshModel
    }

    public enum MeshV2Quality
    {
        Basic,
        Standard,
        Detailed
    }

    public class GaussianSplattingPackageSettings : ScriptableObject
    {
        private static GaussianSplattingPackageSettings _instance;

        public bool LogToConsole;
        
        public string GeneratedModelsPath = "Assets/GeneratedModels";

        public bool DeleteAssociatedFilesWithPrompt = true;

        public bool UsePromptTimeout = true;
        public int PromptTimeoutInSeconds = 300;
        public bool ConfirmDeletes = true;

        public GenerationOption GenerationOption = GenerationOption.GaussianSplat;
        public string GatewayApiUrl = "https://api.dns.404.xyz/";
        public string GatewayApiKey = "6eca4068-3be6-4d30-b828-f63cda3bc35b";
        public string MeshV2ApiUrl = "https://api-eu.404.xyz";
        public string MeshV2ApiKey = "6eca4068-3be6-4d30-b828-f63cda3bc35b";
        public MeshV2Quality MeshV2GeometryQuality = MeshV2Quality.Detailed;
        public MeshV2Quality MeshV2TextureQuality = MeshV2Quality.Detailed;
        public int MeshV2FaceCount = 500000;
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
