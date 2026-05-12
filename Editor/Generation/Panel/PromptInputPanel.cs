using UnityEngine;
using UnityEditor;
using System;

namespace GaussianSplatting.Editor
{
    public class PromptInputPanel
    {
        private Action<string> onTextChanged;
        private Action<Texture2D, string> onImageSelected;
        private Action onSubmit;
        private Action<GenerationMode> onModeChanged;

        private bool isExpanded = true;

        private const int DefaultFaceCount = 500000;

        // User values
        private MeshV2Quality geometryQuality = MeshV2Quality.Detailed;
        private MeshV2Quality textureQuality = MeshV2Quality.Detailed;
        private int faceCount = DefaultFaceCount;

        public PromptInputPanel(Action<string> onTextChanged, Action<Texture2D, string> onImageSelected, Action onSubmit, Action<GenerationMode> onModeChanged)
        {
            this.onTextChanged = onTextChanged;
            this.onImageSelected = onImageSelected;
            this.onSubmit = onSubmit;
            this.onModeChanged = onModeChanged;
        }

            public void ApplyMeshSettingsToSettings()
            {
                var settings = GaussianSplattingPackageSettings.Instance;
                settings.MeshV2GeometryQuality = geometryQuality;
                settings.MeshV2TextureQuality = textureQuality;
                settings.MeshV2FaceCount = Mathf.Clamp(faceCount, 20000, 2000000);
            }
        public void Draw(string textPrompt, Texture2D selectedImage, GenerationMode currentMode)
        {
            EditorGUI.indentLevel = 0;
            isExpanded = EditorGUILayout.Foldout(isExpanded, "Prompt", true);

            if (!isExpanded) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(currentMode == GenerationMode.Mesh))
            {
                string newText = GUILayout.TextField(textPrompt, GUILayout.MinWidth(150));
                if (EditorGUI.EndChangeCheck())
                    onTextChanged?.Invoke(newText);
            }

            if (GUILayout.Button("Image", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(fileData);
                    onImageSelected?.Invoke(tex, path);
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (selectedImage != null)
            {
                float maxWidth = EditorGUIUtility.currentViewWidth - 40f; // some padding
                float aspect = (float)selectedImage.height / selectedImage.width;
                float height = maxWidth * aspect;

                // Get rect for the image
                Rect imageRect = GUILayoutUtility.GetRect(maxWidth, height, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(imageRect, selectedImage, null, ScaleMode.ScaleToFit);

                // Overlay "X" button in top-right corner
                float buttonSize = 20f;
                Rect buttonRect = new Rect(
                    imageRect.xMax - buttonSize - 4, // 4px padding from edge
                    imageRect.yMin + 4,
                    buttonSize,
                    buttonSize
                );

                if (GUI.Button(buttonRect, "X", EditorStyles.miniButton))
                {
                    onImageSelected?.Invoke(null, null);
                }
            }

            // --- Mode Selection Row ---
            GUILayout.Space(4);
            GenerationMode newMode = (GenerationMode)EditorGUILayout.EnumPopup("Output", currentMode);
            if (newMode != currentMode)
                onModeChanged?.Invoke(newMode);

            if (newMode == GenerationMode.Mesh)
            {
                if (selectedImage == null)
                {
                    EditorGUILayout.HelpBox("Mesh v2 requires an image input. Text-only mesh generation is not supported.", MessageType.Info);
                }

                EditorGUI.indentLevel++;
                geometryQuality = (MeshV2Quality)EditorGUILayout.EnumPopup("Geometry Quality", geometryQuality);
                textureQuality = (MeshV2Quality)EditorGUILayout.EnumPopup("Texture Quality", textureQuality);
                faceCount = EditorGUILayout.IntSlider("Face Count", faceCount, 20000, 2000000);
                GUILayout.Space(4);
                if (GUILayout.Button("Reset to Defaults"))
                {
                    geometryQuality = MeshV2Quality.Detailed;
                    textureQuality = MeshV2Quality.Detailed;
                    faceCount = DefaultFaceCount;
                }
                EditorGUI.indentLevel--;
            }

            // Generate Button
            GUILayout.Space(8);
            GUI.enabled = currentMode == GenerationMode.Mesh ? selectedImage != null : textPrompt != "" || selectedImage != null;
            if (GUILayout.Button("Generate"))
                onSubmit?.Invoke();
            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--; // Reset indent
        }
    }
}