using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

namespace GaussianSplatting.Editor
{
    public enum MeshConversionTextureSize
    {
        [Description("0.5K")] Size512 = 512,
        [Description("1K")] Size1024 = 1024,
        [Description("2K")] Size2048 = 2048,
        [Description("4K")] Size4096 = 4096,
        [Description("8K")] Size8192 = 8192
    }

    public enum GenerationOption
    {
        GaussianSplat,
        MeshModel
    }

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
        public string GatewayApiUrl = "https://gateway-us-west.404.xyz/";
        public string GatewayApiKey = "6eca4068-3be6-4d30-b828-f63cda3bc35b";

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
