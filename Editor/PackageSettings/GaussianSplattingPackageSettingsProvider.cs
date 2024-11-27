using System.IO;
using UnityEditor;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public static class GaussianSplattingPackageSettingsProvider
    {
        public const string SettingsPath = "Project/404-GEN 3D Generator";
        [SettingsProvider]
        public static SettingsProvider CreateMyPackageSettingsProvider()
        {
            var provider = new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "404-GEN 3D Generator",
                guiHandler = (searchContext) =>
                {
                    var settings = GaussianSplattingPackageSettings.Instance;
                
                    GUILayout.Space(16);
                    GUILayout.Label("Generated models path", EditorStyles.boldLabel);
                    
                    GUILayout.BeginHorizontal();
                    
                    GUILayout.Label(settings.GeneratedModelsPath);
                    GUILayout.FlexibleSpace();
                    
                    // Button to open folder browser
                    if (GUILayout.Button("Browse", GUILayout.Width(80)))
                    {
                        // Open folder browser and get selected path
                        string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "Generated assets");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            if (selectedPath.StartsWith(Application.dataPath))
                            {
                                if (!Directory.Exists(selectedPath))
                                {
                                    Directory.CreateDirectory(selectedPath);
                                }
                                settings.GeneratedModelsPath = selectedPath
                                    .Replace(Application.dataPath, "")
                                    .Replace("\\", "/");
                            }
                            else
                            {
                                Debug.LogError("Output folder must be within project's Assets folder!");
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Space(8);
                    //setting for logging to Console
                    settings.LogToConsole = EditorGUILayout.ToggleLeft("Send logs to Console window", settings.LogToConsole);

                    settings.DeleteAssociatedFilesWithPrompt = EditorGUILayout.ToggleLeft(
                        "Deleting prompt also deletes associated generated files",
                        settings.DeleteAssociatedFilesWithPrompt);

                    settings.UsePromptTimeout = EditorGUILayout.BeginToggleGroup("Auto-cancel Prompts that Timeout", settings.UsePromptTimeout);
                    settings.PromptTimeoutInSeconds = EditorGUILayout.IntSlider("Timeout threshold (sec)", settings.PromptTimeoutInSeconds, 30, 90, GUILayout.MaxWidth(400));
                    EditorGUILayout.EndToggleGroup();
                    
                    if (GUI.changed)
                    {
                        EditorUtility.SetDirty(settings);
                    }
                },
                keywords = new[] { "Generation", "Threshold" }
            };

            return provider;
        }
    }
}