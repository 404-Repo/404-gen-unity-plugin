using System.IO;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public class GaussianSplattingPackageSettings : ScriptableObject
    {
        private static GaussianSplattingPackageSettings _instance;

        public bool LogToConsole;
        
        public string GeneratedModelsPath = "/GeneratedModels";

        public bool DeleteAssociatedFilesWithPrompt = true;

        public bool UsePromptTimeout = true;
        public int PromptTimeoutInSeconds = 60;
        public bool ConfirmDeletes = true;
        
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
    }
}