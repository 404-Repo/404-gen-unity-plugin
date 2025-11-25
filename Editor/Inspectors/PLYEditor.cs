using UnityEditor;
using UnityEngine;
using System.IO;
using GaussianSplatting.Runtime;

namespace GaussianSplatting.Editor
{
    [CustomEditor(typeof(DefaultAsset))]
    public class PLYEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Get the path of the selected file
            GUI.enabled = true;
            string path = AssetDatabase.GetAssetPath(target);

            // Check if the selected file is a .ply file
            if (Path.GetExtension(path).ToLower() == ".ply")
            {
                // EditorGUILayout.HelpBox("This is a .ply file. You can load it using GaussianSplatAssetCreator.",
                //     MessageType.Info);

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Import to scene", GUILayout.Width(200), GUILayout.Height(40)))
                {
                    try
                    {
                        var assetName = Path.GetFileNameWithoutExtension(path);
                        var gaussianSplatAssetCreator = new GaussianSplatAssetCreator(true);
                        var asset = gaussianSplatAssetCreator.CreateAsset(path);
                        if (asset != null)
                        {
                            GameObject newObject = new GameObject(assetName);
                            var renderer = newObject.AddComponent<GaussianSplatRenderer>();
                        
                            newObject.SetActive(false);
                            newObject.SetActive(true);
                            renderer.m_Asset = asset;
                            newObject.transform.localScale = new Vector3(1, 1, -1);
                            EditorUtility.SetDirty(asset);
                            Debug.Log("Successfully loaded .ply file: " + path);
                        }
                        else
                        {
                            Debug.LogError("Gaussian splat asset is null");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error loading .ply file: " + e.Message);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                base.OnInspectorGUI();
            }
        }
        
        
    }
}