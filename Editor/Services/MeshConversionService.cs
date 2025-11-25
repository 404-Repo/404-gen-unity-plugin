using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

using GaussianSplatting.Runtime;


namespace GaussianSplatting.Editor
{
    public static class MeshConversionService
    {
        private static readonly Dictionary<int, string> _statusByRendererId = new Dictionary<int, string>();

        public static void SetConversionStatus(GaussianSplatRenderer renderer, string status)
        {
            if (!renderer) return;
            _statusByRendererId[renderer.GetInstanceID()] = status;
            if (GaussianSplattingPackageSettings.Instance.LogToConsole)
            {
                Debug.Log($"[Mesh Conversion] {renderer.name}: {status}");
            }
        }

        public static string GetConversionStatus(GaussianSplatRenderer renderer)
        {
            if (!renderer) return null;
            return _statusByRendererId.TryGetValue(renderer.GetInstanceID(), out var s) ? s : null;
        }

        private static async Task<byte[]> SendBytesToServerAsync(byte[] inputData, string fileName)
        {
            var form = new WWWForm();
            form.AddBinaryData("file", inputData, fileName, "application/octet-stream");

            var settings = GaussianSplattingPackageSettings.Instance;

            var minDetail = settings.MinDetailSize.ToString(CultureInfo.InvariantCulture);
            form.AddField("min_detail", minDetail);
            var simplify = settings.Simplify.ToString(CultureInfo.InvariantCulture);
            form.AddField("simplify", simplify);
            var angleLimit = (settings.AngleLimit * Mathf.Deg2Rad).ToString(CultureInfo.InvariantCulture);
            form.AddField("angle_limit", angleLimit);
            var textureSize = ((int)settings.TextureSize).ToString();
            form.AddField("texture_size", textureSize);
            if (GaussianSplattingPackageSettings.Instance.LogToConsole)
            {
                Debug.Log(
                    $"Sending mesh conversion params min_detail:{minDetail}, simplify:{simplify}, angle_limit:{angleLimit}, texture_size:{textureSize}");
            }

            using UnityWebRequest www = UnityWebRequest.Post(settings.ConversionServiceUrl, form);
            var operation = www.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                throw new Exception("Upload failed: " + www.error);
            }
            
            return www.downloadHandler.data;
        }

         public static unsafe void ExportPlyFile(GaussianSplatRenderer gs, bool bakeTransform, string path)
        {
            // Ensure directory exists for the target path
            var targetDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            int kSplatSize = UnsafeUtility.SizeOf<GaussianSplatAssetCreator.InputSplatData>();
            using var gpuData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gs.splatCount, kSplatSize);

            if (!gs.EditExportData(gpuData, bakeTransform))
                return;

            GaussianSplatAssetCreator.InputSplatData[] data =
                new GaussianSplatAssetCreator.InputSplatData[gpuData.count];
            gpuData.GetData(data);

            var gpuDeleted = gs.GpuEditDeleted;
            uint[] deleted = new uint[gpuDeleted.count];
            gpuDeleted.GetData(deleted);

            // count non-deleted splats
            int aliveCount = 0;
            for (int i = 0; i < data.Length; ++i)
            {
                int wordIdx = i >> 5;
                int bitIdx = i & 31;
                bool isDeleted = (deleted[wordIdx] & (1u << bitIdx)) != 0;
                bool isCutout = data[i].nor.sqrMagnitude > 0;
                if (!isDeleted && !isCutout)
                    ++aliveCount;
            }

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            // note: this is a long string! but we don't use multiline literal because we want guaranteed LF line ending
            var header =
                $"ply\nformat binary_little_endian 1.0\nelement vertex {aliveCount}\nproperty float x\nproperty float y\nproperty float z\nproperty float nx\nproperty float ny\nproperty float nz\nproperty float f_dc_0\nproperty float f_dc_1\nproperty float f_dc_2\nproperty float f_rest_0\nproperty float f_rest_1\nproperty float f_rest_2\nproperty float f_rest_3\nproperty float f_rest_4\nproperty float f_rest_5\nproperty float f_rest_6\nproperty float f_rest_7\nproperty float f_rest_8\nproperty float f_rest_9\nproperty float f_rest_10\nproperty float f_rest_11\nproperty float f_rest_12\nproperty float f_rest_13\nproperty float f_rest_14\nproperty float f_rest_15\nproperty float f_rest_16\nproperty float f_rest_17\nproperty float f_rest_18\nproperty float f_rest_19\nproperty float f_rest_20\nproperty float f_rest_21\nproperty float f_rest_22\nproperty float f_rest_23\nproperty float f_rest_24\nproperty float f_rest_25\nproperty float f_rest_26\nproperty float f_rest_27\nproperty float f_rest_28\nproperty float f_rest_29\nproperty float f_rest_30\nproperty float f_rest_31\nproperty float f_rest_32\nproperty float f_rest_33\nproperty float f_rest_34\nproperty float f_rest_35\nproperty float f_rest_36\nproperty float f_rest_37\nproperty float f_rest_38\nproperty float f_rest_39\nproperty float f_rest_40\nproperty float f_rest_41\nproperty float f_rest_42\nproperty float f_rest_43\nproperty float f_rest_44\nproperty float opacity\nproperty float scale_0\nproperty float scale_1\nproperty float scale_2\nproperty float rot_0\nproperty float rot_1\nproperty float rot_2\nproperty float rot_3\nend_header\n";
            fs.Write(Encoding.UTF8.GetBytes(header));
            for (int i = 0; i < data.Length; ++i)
            {
                int wordIdx = i >> 5;
                int bitIdx = i & 31;
                bool isDeleted = (deleted[wordIdx] & (1u << bitIdx)) != 0;
                bool isCutout = data[i].nor.sqrMagnitude > 0;
                if (!isDeleted && !isCutout)
                {
                    var splat = data[i];
                    byte* ptr = (byte*)&splat;
                    fs.Write(new ReadOnlySpan<byte>(ptr, kSplatSize));
                }
            }

            if (GaussianSplattingPackageSettings.Instance.LogToConsole)
            {
                Debug.Log($"Exported PLY {path} with {aliveCount:N0} splats");
            }
        }

        public static async Task<string> ConvertPlyToMeshAsync(byte[] plyData, string modelName)
        {
            var folderPath = GaussianSplattingPackageSettings.Instance.ConvertedModelsPath;
            
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var modelFolderPath = Path.Combine(folderPath, modelName);
            if (!Directory.Exists(modelFolderPath))
                Directory.CreateDirectory(modelFolderPath);

            var plyFileName = $"{modelName}.ply";
            var meshFileName = $"{modelName}.fbx";
            var meshPath = Path.Combine(modelFolderPath, meshFileName).Replace("\\", "/");

            byte[] meshData = await SendBytesToServerAsync(plyData, plyFileName);
            
            File.WriteAllBytes(meshPath, meshData);
            AssetDatabase.ImportAsset(meshPath);

            ModelImporter importer = AssetImporter.GetAtPath(meshPath) as ModelImporter;
            if (importer != null)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.External;
                importer.materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
                importer.materialSearch = ModelImporterMaterialSearch.Local;
                importer.bakeAxisConversion = true;
                
                var textureFolderPath = Path.GetDirectoryName(meshPath);
                importer.ExtractTextures(textureFolderPath);
                
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            return meshPath;
        }

        public static async void ConvertGaussianSplatAsync(GaussianSplatRenderer gaussianSplatRenderer, bool exportInWorldSpace)
        {
            SetConversionStatus(gaussianSplatRenderer, "Mesh conversion started");
            var modelName = gaussianSplatRenderer.asset.name;
            var folderPath = GaussianSplattingPackageSettings.Instance.ConvertedModelsPath;
            var modelFolderPath = Path.Combine(folderPath, modelName);

            // Ensure the base folder exists under Assets (create intermediate folders if needed)
            if (!AssetDatabase.IsValidFolder(folderPath))
                GaussianSplatting.FolderUtility.CreateFolderPath(folderPath);
            // Ensure the model subfolder exists
            if (!AssetDatabase.IsValidFolder(modelFolderPath))
                GaussianSplatting.FolderUtility.CreateFolderPath(modelFolderPath);
            
            var plyFileName = $"{modelName}.ply";
            var meshFileName = $"{modelName}.fbx";
            
            //"Assets/Export/skater.ply"
            var plyPath = Path.Combine(folderPath, $"{modelName}/{plyFileName}")
                .Replace("\\", "/");
            
            //"Assets/Export/skater.fbx"
            var meshPath = Path.Combine(folderPath, $"{modelName}/{meshFileName}")
                .Replace("\\", "/");

            // GaussianSplattingPackageSettings.Instance.SetImportedMeshPath(meshPath);
            
            SetConversionStatus(gaussianSplatRenderer, "Exporting PLY");
            ExportPlyFile(gaussianSplatRenderer, exportInWorldSpace, plyPath);
            
            await Task.Yield();
            
            SetConversionStatus(gaussianSplatRenderer, "Importing PLY into project");
            AssetDatabase.ImportAsset(plyPath);
            var ply = AssetDatabase.LoadAssetAtPath<Object>(plyPath);
            var plyData = File.ReadAllBytes(plyPath);
            
            SetConversionStatus(gaussianSplatRenderer, "Uploading to conversion service...");
            
            try 
            {
                byte[] meshData = await SendBytesToServerAsync(plyData, plyFileName);
                
                SetConversionStatus(gaussianSplatRenderer, "Downloading mesh and importing assets");
                File.WriteAllBytes(meshPath, meshData);
                AssetDatabase.ImportAsset(meshPath);

                // Fix: Configure ModelImporter to ensure materials and textures are imported correctly
                // This prevents Unity from remapping to existing materials in the project
                ModelImporter importer = AssetImporter.GetAtPath(meshPath) as ModelImporter;
                if (importer != null)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    importer.materialLocation = ModelImporterMaterialLocation.External;
                    importer.materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
                    importer.materialSearch = ModelImporterMaterialSearch.Local;
                    importer.bakeAxisConversion = true;
                    
                    var textureFolderPath = Path.GetDirectoryName(meshPath);
                    importer.ExtractTextures(textureFolderPath);
                    
                    importer.SaveAndReimport();
                }

                AssetDatabase.Refresh();
                var mesh = AssetDatabase.LoadAssetAtPath<Object>(meshPath);
                SetConversionStatus(gaussianSplatRenderer, "Conversion complete");

                var meshRoot = new GameObject(gaussianSplatRenderer.name);
                var gsTransform = gaussianSplatRenderer.transform;
                meshRoot.transform.position = gsTransform.position;
                meshRoot.transform.rotation = gsTransform.rotation;
                
                var instance = Object.Instantiate(mesh, meshRoot.transform, false) as GameObject;
                if (instance != null)
                {
                    instance.name = "Mesh";
                    instance.transform.Rotate(new Vector3(-180f,0f,0f));
                    //parents Gaussian splat and disables it
                    gaussianSplatRenderer.transform.SetParent(meshRoot.transform);
                    gaussianSplatRenderer.gameObject.SetActive(false);
                    gaussianSplatRenderer.name = "Gaussian splat";
                }
            }
            catch (Exception ex)
            {
                SetConversionStatus(gaussianSplatRenderer, $"Error: {ex.Message}");
                Debug.LogError(ex.Message);
            }
        }
    }
}
