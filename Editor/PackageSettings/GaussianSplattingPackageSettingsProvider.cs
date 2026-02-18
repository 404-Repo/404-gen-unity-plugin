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
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space();
                    EditorGUILayout.SelectableLabel("Prompt Generation", EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Generated models path", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.TextField(settings.GeneratedModelsPath);

                    if (GUILayout.Button("Browse", GUILayout.Width(80)))
                    {
                        // Open folder browser and get selected path
                        string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "Generated assets");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            if (selectedPath.StartsWith(Application.dataPath))
                            {
                                var relativePath = FolderUtility.GetAssetsRelativePath(selectedPath);
                                
                                if (!FolderUtility.FolderExists(relativePath))
                                {
                                    FolderUtility.CreateFolderPath(relativePath);
                                }

                                settings.GeneratedModelsPath = relativePath;


                            }
                            else
                            {
                                Debug.LogError("Output folder must be within project's Assets folder!");
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(8);
                    //setting for logging to Console
                    settings.LogToConsole = EditorGUILayout.ToggleLeft("Send logs to Console window", settings.LogToConsole);

                    settings.DeleteAssociatedFilesWithPrompt = EditorGUILayout.ToggleLeft(
                        "Deleting prompt also deletes associated generated files",
                        settings.DeleteAssociatedFilesWithPrompt);

                    settings.UsePromptTimeout = EditorGUILayout.BeginToggleGroup("Auto-cancel Prompts that Timeout", settings.UsePromptTimeout);
                    EditorGUILayout.BeginHorizontal();
                    settings.PromptTimeoutInSeconds = EditorGUILayout.IntSlider("Timeout threshold", settings.PromptTimeoutInSeconds, 30, 120, GUILayout.MaxWidth(600));
                    EditorGUILayout.LabelField("sec");
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndToggleGroup();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.SelectableLabel("Gateway Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.LabelField("Gateway URL", EditorStyles.boldLabel);
                    settings.GatewayApiUrl = EditorGUILayout.TextField(settings.GatewayApiUrl);
                    
                    EditorGUILayout.LabelField("Gateway API Key", EditorStyles.boldLabel);
                    settings.GatewayApiKey = EditorGUILayout.TextField(settings.GatewayApiKey);
                    
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();

                    

                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                    if (GUI.changed)
                    {
                        EditorUtility.SetDirty(settings);
                    }
                },
                keywords = new[] { "Generation", "Threshold", "Conversion", "Mesh", "Gaussian", "Splats" }
            };

            return provider;
        }
    }
}
