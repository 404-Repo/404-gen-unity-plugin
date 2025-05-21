using System.Collections;
using System.IO;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace GaussianSplatting
{
    public class MeshConversionEditor : EditorWindow
    {
        [MenuItem("Window/404-GEN/Mesh Conversion")]
        public static void ShowWindow()
        {
            MeshConversionEditor window = GetWindow<MeshConversionEditor>("Mesh Conversion");
            window.minSize = new Vector2(500, 360);
            _conversionStatus = null;
            _ply = null;
            _mesh = null;
            _instance = null;
            ValidateConversionFolderPath();
        }

        private static GaussianSplatRenderer _gaussianSplatRenderer;
        private static bool _exportInWorldSpace;

        private void OnFocus()
        {
            ValidateConversionFolderPath();
        }

        public static void ConvertGaussianSplat(GaussianSplatRenderer gaussianSplatRenderer, bool exportInWorldSpace)
        {
            _gaussianSplatRenderer = gaussianSplatRenderer;
            _exportInWorldSpace = exportInWorldSpace;
            ShowWindow();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.MinHeight(350));
            DrawConversionDescription();
            EditorGUILayout.Space();
            MeshConversionUtility.DrawInsecureHttpOptions();
            EditorGUILayout.Space();

            DrawInputField();
            EditorGUILayout.Space();
            
            DrawFolderLocation();
            EditorGUILayout.Space();
            
            MeshConversionUtility.DrawMeshConversionOptions();
            EditorGUILayout.EndVertical();

            DrawConversionButton();
            GUILayout.Space(16);
            DrawOutputFields();
            GUILayout.Space(16);
            DrawConversionStatus();
        }

        private void DrawConversionDescription()
        {
            EditorGUILayout.HelpBox(
                "The conversion will process Gaussian Splat Renderer component, " +
                "generate a .PLY file that will be sent to conversion service" +
                " and retrieve a mesh model",
                MessageType.Info);
        }

        private void DrawInputField()
        {
            EditorGUI.BeginChangeCheck();

            var assignedObject = EditorGUILayout.ObjectField("Gaussian Splat Renderer", _gaussianSplatRenderer, typeof(GaussianSplatRenderer), true);
            if (assignedObject != _gaussianSplatRenderer)
            {
                if (assignedObject is GaussianSplatRenderer renderer)
                {
                    _gaussianSplatRenderer = renderer;
                }
                
                if (assignedObject == null)
                {
                    _gaussianSplatRenderer = null;
                }
            }

            if (_gaussianSplatRenderer == null)
            {
                EditorGUILayout.HelpBox("Assign Gaussian Splat Renderer component", MessageType.Error);
            }
        }

        private static string _covertedFilesPathError = null;

        private void DrawFolderLocation()
        {
            var settings = GaussianSplattingPackageSettings.Instance;
            GUILayout.Label("Converted files location", EditorStyles.boldLabel);
                    
            GUILayout.BeginHorizontal();
                    
            GUILayout.TextField(settings.ConvertedModelsPath);
            // Button to open folder browser
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                // Open folder browser and get selected path
                string selectedPath = EditorUtility.OpenFolderPanel("Select Folder for conversion files", Application.dataPath, "Generated assets");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        if (!Directory.Exists(selectedPath))
                        {
                            Directory.CreateDirectory(selectedPath);
                        }
                        settings.ConvertedModelsPath = "Assets" + selectedPath
                            .Replace(Application.dataPath, "")
                            .Replace("\\", "/");
                    }
                    else
                    {
                        settings.ConvertedModelsPath = null;
                    }

                    ValidateConversionFolderPath(selectedPath);
                }
            }
            GUILayout.EndHorizontal();

            if (_covertedFilesPathError != null)
            {
                EditorGUILayout.HelpBox(_covertedFilesPathError, MessageType.Error);
            }
            
            if (settings.ConvertedModelsPath != null && _folderWithinAssets && !_folderExists)
            {
                if (GUILayout.Button("Create folder", GUILayout.Width(120)))
                {
                    FolderUtility.CreateFolderPath(settings.ConvertedModelsPath);
                    ValidateConversionFolderPath();
                }
            }
        }
        


        private static bool _folderWithinAssets;
        private static bool _folderExists;

        private static void ValidateConversionFolderPath()
        {
            ValidateConversionFolderPath(Path.Join(Application.dataPath.Replace("Assets", ""),
                GaussianSplattingPackageSettings.Instance.ConvertedModelsPath));
        }
        private static void ValidateConversionFolderPath(string selectedPath)
        {
            _folderWithinAssets = selectedPath.StartsWith(Application.dataPath);
            _folderExists = Directory.Exists(selectedPath);
            _covertedFilesPathError = 
                !_folderWithinAssets ? "Conversion files folder must be within project's Assets folder!" : 
                !_folderExists ? "Conversion files folder does not exist" : 
                null;
        }

        private static string _conversionStatus = null;
        private static bool _conversionProcessing = false;

        private static Object _ply, _mesh;
        private static GameObject _instance;
        private void DrawConversionButton()
        {
            
            bool conversionButtonEnabled = _gaussianSplatRenderer != null && _folderExists && _folderWithinAssets &&
                                           !_conversionProcessing && PlayerSettings.insecureHttpOption != InsecureHttpOption.NotAllowed;
            using (new EditorGUI.DisabledScope(!conversionButtonEnabled))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Start conversion to Mesh", GUILayout.Width(240), GUILayout.Height(40)))
                {
                    _conversionProcessing = true;
                    _conversionStatus = "Started";
                    _ply = null;
                    _mesh = null;
                    _instance = null;
                    EditorCoroutineUtility.StartCoroutineOwnerless(MeshConversionCoroutine());
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private void DrawConversionStatus()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(_conversionStatus, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        
        private void DrawOutputFields()
        {
            using (new EditorGUI.DisabledScope(_conversionStatus == null))
            {
                EditorGUILayout.ObjectField("ply file", _ply, typeof(Object), false);
                EditorGUILayout.ObjectField("mesh file", _mesh, typeof(Object), false);    
                EditorGUILayout.ObjectField("Instance", _instance, typeof(GameObject), true);    
            }
        }

        private IEnumerator MeshConversionCoroutine()
        {
            _conversionStatus = "Mesh conversion started.";
            var modelName = _gaussianSplatRenderer.asset.name;
            var folderPath = GaussianSplattingPackageSettings.Instance.ConvertedModelsPath;
            var modelFolderPath = Path.Combine(folderPath, modelName);

            if (!AssetDatabase.IsValidFolder(modelFolderPath))
            {
                Debug.Log($"Creating folder {modelName} in {folderPath}");
                AssetDatabase.CreateFolder(folderPath, modelName);
            }
            else
            {
                Debug.Log($"Folder exists {modelName}");
            }
            
            var plyFileName = $"{modelName}.ply";
            var meshFileName = $"{modelName}.fbx";
            
            //"Assets/Export/skater.ply"
            var plyPath = Path.Combine(folderPath, $"{modelName}/{plyFileName}")
                .Replace("\\", "/");
            
            //"Assets/Export/skater.fbx"
            var meshPath = Path.Combine(folderPath, $"{modelName}/{meshFileName}")
                .Replace("\\", "/");

            GaussianSplattingPackageSettings.Instance.SetImportedMeshPath(meshPath);
            
            MeshConversionUtility.ExportPlyFile(_gaussianSplatRenderer, _exportInWorldSpace, plyPath);
            yield return null;
            if (GaussianSplattingPackageSettings.Instance.LogToConsole)
            {
                Debug.Log($"Importing {plyPath}");
            }
            AssetDatabase.ImportAsset(plyPath);
            _ply = AssetDatabase.LoadAssetAtPath<Object>(plyPath);
            var plyData = File.ReadAllBytes(plyPath);
            
            _conversionStatus = "Waiting for mesh response from conversion service...";
            EditorCoroutineUtility.StartCoroutineOwnerless(
                MeshConversionUtility.SendBytesToServer(plyData, plyFileName,
                    onResponseReceived: (byte[] meshData) =>
                    {
                        File.WriteAllBytes(meshPath, meshData);
                        AssetDatabase.ImportAsset(meshPath);
                        AssetDatabase.Refresh();
                        _mesh = AssetDatabase.LoadAssetAtPath<Object>(meshPath);
                        _conversionStatus = "Conversion complete.";

                        var meshRoot = new GameObject(_gaussianSplatRenderer.name);
                        var gsTransform = _gaussianSplatRenderer.transform;
                        meshRoot.transform.position = gsTransform.position;
                        meshRoot.transform.rotation = gsTransform.rotation;
                        
                        _instance = Instantiate(_mesh, meshRoot.transform, false) as GameObject;
                        if (_instance != null)
                        {
                            _instance.name = "Mesh";
                            _instance.transform.Rotate(new Vector3(-180f,0f,0f));
                            //parents Gaussian splat and disables it
                            _gaussianSplatRenderer.transform.SetParent(meshRoot.transform);
                            _gaussianSplatRenderer.gameObject.SetActive(false);
                            _gaussianSplatRenderer.name = "Gaussian splat";
                        }
                        _conversionProcessing = false;
                    },
                    onError: errorMessage =>
                    {
                        _conversionStatus = errorMessage;
                        _conversionProcessing = false;
                    }));
        }
    }
}