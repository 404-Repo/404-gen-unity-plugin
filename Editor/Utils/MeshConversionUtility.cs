using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace GaussianSplatting.Editor
{
    public static class MeshConversionUtility
    {
        private static bool _showConversionSettings = true;

        public static void DrawMeshConversionOptions()
        {
            _showConversionSettings =
                EditorGUILayout.Foldout(_showConversionSettings, "Gaussian Splat to Mesh Conversion Settings");

            if (_showConversionSettings)
            {
                GUILayout.BeginVertical(GUILayout.MaxWidth(400));

                EditorGUI.indentLevel++;
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                var newMinDetailSizeValue = EditorGUILayout.Slider("Min Detail Size",
                    GaussianSplattingPackageSettings.Instance.MinDetailSize, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(GaussianSplattingPackageSettings.Instance, "Set Min Details size");
                    GaussianSplattingPackageSettings.Instance.MinDetailSize = newMinDetailSizeValue;
                }

                EditorGUI.BeginChangeCheck();
                var newSimplifyValue = EditorGUILayout.Slider("Simplify",
                    GaussianSplattingPackageSettings.Instance.Simplify, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(GaussianSplattingPackageSettings.Instance, "Set Simplify value");
                    GaussianSplattingPackageSettings.Instance.Simplify = newSimplifyValue;
                }

                EditorGUI.BeginChangeCheck();
                var newAngleLimit = EditorGUILayout.IntSlider("Angle Limit",
                    GaussianSplattingPackageSettings.Instance.AngleLimit, 0, 360);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(GaussianSplattingPackageSettings.Instance, "Set Angle Limit");
                    GaussianSplattingPackageSettings.Instance.AngleLimit = newAngleLimit;
                }

                EditorGUI.BeginChangeCheck();
                var newTextureSizeIndex = EditorGUILayout.Popup(
                    "Texture Size",
                    GetTextureSizeIndex(GaussianSplattingPackageSettings.Instance.TextureSize),
                    GetTextureSizeDisplayOptions());
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(GaussianSplattingPackageSettings.Instance, "Set Texture Size");
                    GaussianSplattingPackageSettings.Instance.TextureSize = GetTextureSizeValue(newTextureSizeIndex);
                }

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Set Defaults", GUILayout.Width(80)))
                {
                    Undo.RecordObject(GaussianSplattingPackageSettings.Instance, "Set Default conversion parameters");
                    GaussianSplattingPackageSettings.Instance.SetDefaultConversionParameters();
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(8);

                EditorGUI.indentLevel--;

                GUILayout.EndVertical();
            }
        }

        private static int GetTextureSizeIndex(MeshConversionTextureSize textureSize)
        {
            var enumList = Enum.GetValues(typeof(MeshConversionTextureSize)).Cast<MeshConversionTextureSize>().ToList();
            return enumList.FindIndex(e => (int)e == (int)textureSize);
        }

        private static MeshConversionTextureSize GetTextureSizeValue(int index)
        {
            var enumList = Enum.GetValues(typeof(MeshConversionTextureSize)).Cast<MeshConversionTextureSize>().ToList();
            return enumList[index];
        }

        private static string[] _textureSizeDisplayNames;

        private static string[] GetTextureSizeDisplayOptions()
        {
            if (_textureSizeDisplayNames == null)
            {
                _textureSizeDisplayNames = new[] { "512", "1024", "2K", "4K", "8K" };

                //todo: get exact user friendly labels from GetDescription attribute
                // var enumValues = Enum.GetValues(typeof(MeshConversionTextureSize)).Cast<MeshConversionTextureSize>().ToArray();
                // _textureSizeDisplayNames = new string[enumValues.Length];
                // for (var i = 0; i < enumValues.Length; i++)
                // {
                //     var textureSizeValue = enumValues[i];
                //     var da = (DescriptionAttribute[])(textureSizeValue.GetType().GetField(textureSizeValue.ToString()))
                //         .GetCustomAttributes(typeof(DescriptionAttribute), false);
                //     _textureSizeDisplayNames[i] = da.Length > 0 ? da[0].Description : textureSizeValue.ToString();
                // }
            }

            return _textureSizeDisplayNames;
        }

        public static unsafe void ExportPlyFile(GaussianSplatRenderer gs, bool bakeTransform, string path)
        {
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

        public static IEnumerator SendBytesToServer(byte[] inputData, string fileName,
            Action<byte[]> onResponseReceived, Action<string> onError = null)
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
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke("Upload failed: " + www.error);
            }
            else
            {
                byte[] fbxData = www.downloadHandler.data;
                onResponseReceived.Invoke(fbxData);
            }
        }

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

        public static void DrawInsecureHttpOptions()
        {
            if (PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed)
            {
                GUILayout.Space(20);
                EditorGUILayout.HelpBox(
                    "Non-secure network connections are required to use mesh conversion service",
                    MessageType.Error);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Allow downloads over http:");
                EditorGUILayout.Space();
                if (GUILayout.Button("Development only", GUILayout.Width(120)))
                {
                    PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Always allowed", GUILayout.Width(120)))
                {
                    PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(20);
            }
        }
    }
}