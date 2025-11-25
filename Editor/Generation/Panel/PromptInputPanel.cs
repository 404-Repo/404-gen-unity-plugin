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

        private const float DefaultMinDetailSize = 0.1f;
        private const float DefaultSimplify = 0.0f;
        private const float DefaultAngleLimit = 60f;
        private const int DefaultTextureSizeIndex = 2; // 2048

        // User values
        private float minDetailSize = DefaultMinDetailSize;
        private float simplify = DefaultSimplify;
        private float angleLimit = DefaultAngleLimit;
        private int textureSizeIndex = DefaultTextureSizeIndex;

        // Dropdown options
        private readonly int[] textureSizes = { 512, 1024, 2048, 4096, 8192 };

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
                settings.MinDetailSize = minDetailSize;
                settings.Simplify = simplify;
                settings.AngleLimit = Mathf.RoundToInt(angleLimit);
                int tex = textureSizes[Mathf.Clamp(textureSizeIndex, 0, textureSizes.Length - 1)];
                // Map int value to enum by underlying value
                if (System.Enum.IsDefined(typeof(MeshConversionTextureSize), tex))
                {
                    settings.TextureSize = (MeshConversionTextureSize)tex;
                }
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
            string newText = GUILayout.TextField(textPrompt, GUILayout.MinWidth(150));
            if (EditorGUI.EndChangeCheck())
                onTextChanged?.Invoke(newText);

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
                EditorGUI.indentLevel++;
                minDetailSize = EditorGUILayout.Slider("Min Detail Size", minDetailSize, 0f, 1f);
                simplify = EditorGUILayout.Slider("Simplify", simplify, 0f, 1f);
                angleLimit = EditorGUILayout.Slider("Angle Limit", angleLimit, 0f, 360f);
                textureSizeIndex = EditorGUILayout.Popup(
                    "Texture Size",
                    textureSizeIndex,
                    Array.ConvertAll(textureSizes, s => s + "px")
                );
                GUILayout.Space(4);
                if (GUILayout.Button("Reset to Defaults"))
                {
                    minDetailSize = DefaultMinDetailSize;
                    simplify = DefaultSimplify;
                    angleLimit = DefaultAngleLimit;
                    textureSizeIndex = DefaultTextureSizeIndex;
                }
                EditorGUI.indentLevel--;
            }

            // Generate Button
            GUILayout.Space(8);
            GUI.enabled = textPrompt != "" || selectedImage != null;
            if (GUILayout.Button("Generate"))
                onSubmit?.Invoke();
            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--; // Reset indent
        }
    }
}