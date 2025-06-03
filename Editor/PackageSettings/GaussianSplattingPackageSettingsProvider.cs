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
                    // if (EditorGUILayout.LinkButton("Documentation"))
                    // {
                    //     Application.OpenURL("https://atlas-14.gitbook.io/404/x9lT9mUlXacWtIlJhtMr");
                    // }
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

                                //todo:remove
                                // settings.GeneratedModelsPath = selectedPath
                                //     .Replace(Application.dataPath, "Assets")
                                //     .Replace("\\", "/");
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
                    
                    EditorGUILayout.SelectableLabel("Mesh Conversion ", EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Mesh Conversion service URL", EditorStyles.boldLabel);
                    GaussianSplattingPackageSettings.Instance.ConversionServiceUrl = EditorGUILayout.TextField(GaussianSplattingPackageSettings.Instance.ConversionServiceUrl);
                    EditorGUILayout.Space();
                    MeshConversionUtility.DrawMeshConversionOptions();
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